namespace SiteKit.Core.Importing;

/// <summary>Downloads bounded JSON import payloads from explicitly allowlisted HTTPS hosts.</summary>
public sealed class SiteKitRestImportSource(HttpClient client, IEnumerable<string> allowedHosts)
{
    private readonly HashSet<string> _allowedHosts = allowedHosts
        .Where(host => !string.IsNullOrWhiteSpace(host))
        .Select(host => host.Trim().TrimEnd('.'))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<ReadOnlyMemory<byte>> FetchAsync(SiteKitImportMapping mapping, CancellationToken cancellationToken = default)
    {
        var errors = JsonSiteKitImportMappingRegistry.Validate(mapping);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        if (string.IsNullOrWhiteSpace(mapping.SourceUrl) || !Uri.TryCreate(mapping.SourceUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("This mapping does not define a REST source URL.");
        if (!_allowedHosts.Contains(uri.IdnHost.TrimEnd('.')))
            throw new ArgumentException($"REST source host '{uri.IdnHost}' is not in SiteKit:Import:AllowedHosts.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if ((int)response.StatusCode is >= 300 and < 400)
            throw new InvalidOperationException("REST import sources cannot redirect. Configure the final HTTPS endpoint directly.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"REST import source returned HTTP {(int)response.StatusCode}.");
        if (response.Content.Headers.ContentLength is > SiteKitDataImportService.MaximumBytes)
            throw new InvalidOperationException("REST import response exceeds the 10 MiB limit.");

        await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
        await using var output = new MemoryStream();
        var buffer = new byte[32 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer, timeout.Token);
            if (read == 0) break;
            if (output.Length + read > SiteKitDataImportService.MaximumBytes)
                throw new InvalidOperationException("REST import response exceeds the 10 MiB limit.");
            await output.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
        }
        if (output.Length == 0) throw new InvalidOperationException("REST import source returned an empty response.");
        return output.ToArray();
    }
}
