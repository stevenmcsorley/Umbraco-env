using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SiteKit.AgentHost;

/// <summary>Public-client Sign in with ChatGPT flow for a user-controlled loopback Agent Host.</summary>
public sealed class SiwcClient(HttpClient httpClient, CredentialStore credentialStore)
{
    private const string Issuer = "https://auth.openai.com";
    private const string Resource = "https://api.openai.com/v1";
    private const int CallbackPort = 1455;
    private const string CallbackPath = "/auth/callback";
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);

    public async Task<ChatGptAccount> SignInAsync(bool authorizePlanUsage, CancellationToken cancellationToken = default)
    {
        var existing = await credentialStore.LoadAsync(cancellationToken);
        var hostId = await credentialStore.GetOrCreateHostIdAsync(cancellationToken);
        var redirectUri = $"http://127.0.0.1:{CallbackPort}{CallbackPath}";
        var metadata = await DiscoverAsync(cancellationToken);
        var state = RandomToken(32);
        var nonce = RandomToken(32);
        var verifier = RandomToken(48);
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{CallbackPort}/");
        listener.Start();

        var clientId = existing?.ClientId ?? "dynamic_agent_client";
        var scopes = authorizePlanUsage
            ? "openid profile email offline_access resource.invoke chatgpt.tokens.use.direct"
            : "openid profile email";
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = scopes,
            ["resource"] = Resource,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge_method"] = "S256",
            ["code_challenge"] = challenge,
            ["ext_agent_host_id"] = hostId
        };
        if (existing is null) query["agent_name_hint"] = "SiteKit";
        else
        {
            query["id_token_hint"] = existing.IdToken;
            if (!string.IsNullOrWhiteSpace(existing.Email)) query["login_hint"] = existing.Email;
        }
        var authorizationUri = metadata.AuthorizationEndpoint + "?" + string.Join('&', query.Select(item =>
            Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value)));
        Console.WriteLine("Opening the system browser. Complete Sign in with ChatGPT to return to SiteKit Agent Host.");
        try { Process.Start(new ProcessStartInfo(authorizationUri) { UseShellExecute = true }); }
        catch (Exception exception) { Console.WriteLine($"Could not open a browser automatically: {exception.Message}\nOpen this address manually:\n{authorizationUri}"); }

        var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(5), cancellationToken);
        var callback = context.Request;
        var response = context.Response;
        response.ContentType = "text/html; charset=utf-8";
        var callbackState = callback.QueryString["state"];
        if (callback.Url?.AbsolutePath != CallbackPath || callbackState is null || !FixedTimeEquals(state, callbackState))
        {
            await RespondAsync(response, "<h1>Sign in could not be verified</h1><p>Return to SiteKit Agent Host and start again.</p>");
            throw new InvalidOperationException("OAuth callback path or state did not match the pending sign-in.");
        }
        var oauthError = callback.QueryString["error"];
        if (!string.IsNullOrEmpty(oauthError))
        {
            await RespondAsync(response, "<h1>Sign in was not completed</h1><p>You can close this tab and return to SiteKit Agent Host.</p>");
            throw new InvalidOperationException("ChatGPT sign-in was not completed: " + oauthError);
        }
        var code = callback.QueryString["code"];
        var returnedClientId = callback.QueryString["client_id"];
        if (string.IsNullOrWhiteSpace(code))
        {
            await RespondAsync(response, "<h1>No authorization code was returned</h1>");
            throw new InvalidOperationException("No authorization code was returned.");
        }
        if (existing is not null && returnedClientId is not null && returnedClientId != existing.ClientId)
        {
            await RespondAsync(response, "<h1>Account registration did not match</h1>");
            throw new InvalidOperationException("The callback client_id did not match the selected saved registration.");
        }
        var issuedClientId = existing?.ClientId ?? returnedClientId;
        if (string.IsNullOrWhiteSpace(issuedClientId) || issuedClientId == "dynamic_agent_client")
        {
            await RespondAsync(response, "<h1>ChatGPT did not return a registered client</h1>");
            throw new InvalidOperationException("The dynamic registration did not return its issued client_id.");
        }

        try
        {
            using var tokenResponse = await httpClient.PostAsync(metadata.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code", ["client_id"] = issuedClientId, ["code"] = code,
                ["code_verifier"] = verifier, ["redirect_uri"] = redirectUri, ["resource"] = Resource
            }), cancellationToken);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode) throw new InvalidOperationException($"OAuth token exchange failed ({(int)tokenResponse.StatusCode}): {ReadOAuthError(tokenJson)}");
            using var document = JsonDocument.Parse(tokenJson);
            var root = document.RootElement;
            var idToken = GetString(root, "id_token") ?? throw new InvalidOperationException("The OAuth token response did not include an ID token.");
            var identity = await ValidateIdTokenAsync(idToken, issuedClientId, nonce, metadata, cancellationToken);
            if (existing is not null && identity.Subject != existing.Subject)
                throw new InvalidOperationException("The newly authenticated ChatGPT account does not match the selected saved account.");
            // Only the token response establishes granted plan-usage scopes.
            // Callback scopes describe the authorization request and are not proof
            // that the token endpoint granted them.
            var grantedScopes = ChatGptAccount.ParseGrantedScopes(GetString(root, "scope"));
            var accessToken = GetString(root, "access_token") ?? throw new InvalidOperationException("The token response did not include an access token.");
            var refreshToken = GetString(root, "refresh_token") ?? throw new InvalidOperationException("The token response did not include a refresh token.");
            var expiresIn = root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds) ? seconds : 3600;
            var account = new ChatGptAccount
            {
                Issuer = Issuer, Subject = identity.Subject, Email = identity.Email, ClientId = issuedClientId, HostId = hostId,
                IdToken = idToken, AccessToken = accessToken, RefreshToken = refreshToken, Scopes = grantedScopes,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn)), SavedAt = DateTimeOffset.UtcNow
            };
            await credentialStore.SaveAsync(account, cancellationToken);
            await RespondAsync(response, "<h1>SiteKit is connected</h1><p>You can close this tab and return to SiteKit Agent Host.</p>");
            return account;
        }
        catch
        {
            if (response.OutputStream.CanWrite)
            {
                try { await RespondAsync(response, "<h1>Sign in could not be completed</h1><p>Return to SiteKit Agent Host for details.</p>"); }
                catch (ObjectDisposedException) { }
            }
            throw;
        }
    }

    public async Task<ChatGptAccount> GetValidAccountAsync(bool requirePlanUsage, CancellationToken cancellationToken = default)
    {
        var account = await credentialStore.LoadAsync(cancellationToken) ?? throw new InvalidOperationException("No ChatGPT account is connected. Run `sitekit-agent login` first.");
        if (requirePlanUsage && !account.IsEligibleForPlanUsage)
            throw new InvalidOperationException("ChatGPT plan usage is not authorized for this account. Reconnect and separately authorize plan usage.");
        if (account.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2)) return account;
        await RefreshGate.WaitAsync(cancellationToken);
        try
        {
            account = await credentialStore.LoadAsync(cancellationToken) ?? throw new InvalidOperationException("No ChatGPT account is connected.");
            if (account.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2)) return account;
            var metadata = await DiscoverAsync(cancellationToken);
            using var response = await httpClient.PostAsync(metadata.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token", ["client_id"] = account.ClientId,
                ["refresh_token"] = account.RefreshToken, ["resource"] = Resource
            }), cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"ChatGPT token refresh failed ({(int)response.StatusCode}): {ReadOAuthError(json)}");
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var seconds = root.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var value) ? value : 3600;
            account = account with
            {
                AccessToken = GetString(root, "access_token") ?? throw new InvalidOperationException("The refresh response did not include an access token."),
                RefreshToken = GetString(root, "refresh_token") ?? account.RefreshToken,
                Scopes = (GetString(root, "scope") ?? string.Join(' ', account.Scopes)).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, seconds)), SavedAt = DateTimeOffset.UtcNow
            };
            await credentialStore.SaveAsync(account, cancellationToken);
            return account;
        }
        finally { RefreshGate.Release(); }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var account = await credentialStore.LoadAsync(cancellationToken);
        if (account is null) return;
        try
        {
            var metadata = await DiscoverAsync(cancellationToken);
            if (metadata.RevocationEndpoint is not null)
            {
                using var response = await httpClient.PostAsync(metadata.RevocationEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["token"] = account.RefreshToken, ["token_type_hint"] = "refresh_token", ["client_id"] = account.ClientId
                }), cancellationToken);
                if (!response.IsSuccessStatusCode) Console.Error.WriteLine("ChatGPT remote token revocation was not confirmed; local credentials will still be cleared.");
            }
        }
        catch (HttpRequestException) { Console.Error.WriteLine("ChatGPT remote token revocation could not be reached; local credentials will still be cleared."); }
        finally { await credentialStore.DeleteAsync(cancellationToken); }
    }

    private async Task<OidcMetadata> DiscoverAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(Issuer + "/.well-known/openid-configuration", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        return new OidcMetadata(
            Required(root, "authorization_endpoint"), Required(root, "token_endpoint"), Required(root, "jwks_uri"),
            root.TryGetProperty("revocation_endpoint", out var revocation) ? revocation.GetString() : null,
            Required(root, "issuer"));
    }

    private async Task<IdentityClaims> ValidateIdTokenAsync(string token, string clientId, string expectedNonce, OidcMetadata metadata, CancellationToken cancellationToken)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw new InvalidOperationException("OpenAI returned a malformed ID token.");
        using var header = JsonDocument.Parse(FromBase64Url(parts[0]));
        using var payload = JsonDocument.Parse(FromBase64Url(parts[1]));
        var headerRoot = header.RootElement;
        var claims = payload.RootElement;
        if (GetString(headerRoot, "alg") != "RS256") throw new InvalidOperationException("The ID token uses an unsupported signing algorithm.");
        var keyId = GetString(headerRoot, "kid") ?? throw new InvalidOperationException("The ID token has no signing key id.");
        using var jwksResponse = await httpClient.GetAsync(metadata.JwksUri, cancellationToken);
        jwksResponse.EnsureSuccessStatusCode();
        using var jwks = await JsonDocument.ParseAsync(await jwksResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var key = jwks.RootElement.GetProperty("keys").EnumerateArray().FirstOrDefault(x => GetString(x, "kid") == keyId);
        if (key.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException("The ID token signing key is unknown.");
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Modulus = FromBase64Url(Required(key, "n")), Exponent = FromBase64Url(Required(key, "e")) });
        var signingInput = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
        if (!rsa.VerifyData(signingInput, FromBase64Url(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new InvalidOperationException("The ID token signature is invalid.");

        if (GetString(claims, "iss") != metadata.Issuer) throw new InvalidOperationException("The ID token issuer is invalid.");
        var audienceValid = claims.TryGetProperty("aud", out var audience) && (audience.ValueKind == JsonValueKind.String
            ? audience.GetString() == clientId
            : audience.ValueKind == JsonValueKind.Array && audience.EnumerateArray().Any(x => x.GetString() == clientId));
        if (!audienceValid) throw new InvalidOperationException("The ID token audience is invalid.");
        if (!claims.TryGetProperty("exp", out var expiration) || !expiration.TryGetInt64(out var exp) || DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("The ID token has expired.");
        if (GetString(claims, "nonce") is not { } nonce || !FixedTimeEquals(expectedNonce, nonce)) throw new InvalidOperationException("The ID token nonce is invalid.");
        var subject = Required(claims, "sub");
        return new IdentityClaims(subject, GetString(claims, "email"));
    }

    private static async Task RespondAsync(HttpListenerResponse response, string html)
    {
        var bytes = Encoding.UTF8.GetBytes("<!doctype html><meta charset=utf-8><title>SiteKit</title><main style='font:18px system-ui;max-width:40rem;margin:12vh auto'>" + html + "</main>");
        response.StatusCode = 200;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var left = Encoding.UTF8.GetBytes(expected);
        var right = Encoding.UTF8.GetBytes(actual);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string RandomToken(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
    private static string Required(JsonElement element, string name) => GetString(element, name) ?? throw new InvalidOperationException($"OpenID metadata or token did not contain `{name}`.");
    private static string? GetString(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string ReadOAuthError(string body)
    {
        try { using var doc = JsonDocument.Parse(body); return GetString(doc.RootElement, "error_description") ?? GetString(doc.RootElement, "error") ?? "authorization failed"; }
        catch (JsonException) { return "authorization failed"; }
    }

    private sealed record OidcMetadata(string AuthorizationEndpoint, string TokenEndpoint, string JwksUri, string? RevocationEndpoint, string Issuer);
    private sealed record IdentityClaims(string Subject, string? Email);
}
