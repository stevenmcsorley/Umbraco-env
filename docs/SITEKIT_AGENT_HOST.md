# SiteKit Agent Host

The Agent Host is a local .NET process. It stores ChatGPT credentials and
conversation history on the machine where it runs, talks directly to the public
OpenAI authentication and Responses endpoints, and performs a small allowlist
of validated SiteKit operations. It does not request an OpenAI API key.

## Start

From the repository root, with the .NET 10 SDK installed:

```powershell
dotnet run --project SiteKit.AgentHost -- login
```

The command opens the system browser using the official Sign in with ChatGPT
flow and receives the callback on `127.0.0.1:1455/auth/callback`. The console
asks separately whether to request ChatGPT plan usage. Answer `y` only if you
want to grant that separate permission. A connected identity without the plan
scope can inspect local connection status but cannot make model requests.

```powershell
dotnet run --project SiteKit.AgentHost -- status
dotnet run --project SiteKit.AgentHost -- models
dotnet run --project SiteKit.AgentHost -- chat --project my-site
dotnet run --project SiteKit.AgentHost -- logout
```

The plan-sharing integration is available only when OpenAI's current account,
workspace and program eligibility rules allow it. It is not unrestricted API
access. Usage limits are surfaced as a recoverable error. Requests use
`store:false`, `stream:true`, and local conversation history; unsupported
hosted tools and APIs are not used.

## Local storage and configuration

The default directory is `%LOCALAPPDATA%/SiteKit/AgentHost` on Windows and the
equivalent user-local application data directory on Unix. On Windows the token
record is encrypted with DPAPI for the current user. On Unix it is written with
owner-only file permissions. Conversation input and tool outputs are stored in
the same local directory under `history`; users can remove this history without
disconnecting their ChatGPT account.

Configure repository and state roots with:

```powershell
$env:SITEKIT_ROOT = (Get-Location).Path
$env:SITEKIT_DATA_ROOT = "F:\my-sitekit-data"
$env:SITEKIT_AGENTHOST_DIRECTORY = "$env:LOCALAPPDATA\SiteKit\AgentHost"
$env:SITEKIT_DOTNET_EXECUTABLE = "C:\Program Files\dotnet\dotnet.exe"
```

`SITEKIT_DOTNET_EXECUTABLE` optionally points the fixed build tool at a specific
trusted .NET CLI executable when `dotnet` is not on the host process PATH.

The component, project and placement registries normally use the Umbraco
application's `umbraco/Data/SiteKit` directory. Keep that directory on local
user-controlled storage when using plan sharing. Do not mount it into a shared
host that forwards multiple users' plan tokens.

## Local MCP tools

Launch the stdio MCP server as a subprocess configured in an MCP-compatible
client:

```json
{
  "mcpServers": {
    "sitekit": {
      "command": "dotnet",
      "args": ["run", "--project", "F:/umbraco/SiteKit.AgentHost", "--", "mcp"],
      "env": { "SITEKIT_ROOT": "F:/umbraco" }
    }
  }
}
```

The server exposes `sitekit.list_projects`, `sitekit.inspect_project`,
`sitekit.create_project`, `sitekit.list_components`, `sitekit.create_component`,
`sitekit.create_placement`, `sitekit.preview_project`, `sitekit.create_entity`,
`sitekit.list_entities`, `sitekit.generate_project`, `sitekit.validate_project`,
`sitekit.list_modules`, `sitekit.install_module`, and `sitekit.build_project`.
There is no delete, deploy, arbitrary shell, or
external-service tool. Build is fixed to either the generated project for the
requested id or `dotnet build UmbraccoEnv.sln` under the configured root. Calls are recorded in the local
`umbraco/Data/SiteKit/audit/agent-tools.jsonl` file.

The current SIWC preview flow supports locally executed function tools, but not
hosted MCP connections. `sitekit-agent chat` executes those functions inside
the local Agent Host; the separate stdio server is for local Codex or other
compatible MCP clients. It should not be presented as a hosted ChatGPT MCP
connection.

## Verification status

- The Agent Host and full solution compile on .NET 10.
- MCP initialization, tool discovery and a local `sitekit.list_components`
  request were exercised over stdio.
- No OpenAI account was connected in this environment, so OAuth, token refresh,
  model discovery and streaming inference remain unverified against a live
  account.
- The Docker and SQL Server stack now starts locally, and the real Umbraco
  login route returns HTTP 200. No administrator session was provisioned for
  this verification, so authenticated Studio pages and database-backed AI tool
  calls remain unverified in the live CMS UI.


## Administrator bootstrap

The legacy customer login is not sufficient to establish the first admin account when registration is public. Addresses listed in `SiteKit:AdminEmails` therefore cannot register without a separate bootstrap code. In Docker, set `SITEKIT_ADMIN_EMAILS` and temporarily set `SITEKIT_ADMIN_REGISTRATION_CODE` in the private `.env` file. Register that exact address at `/register` with the code (or send `adminRegistrationCode` to `/api/auth/register`), then remove the bootstrap code and restart the app. Subsequent admin sign-ins use the saved account and no longer need the bootstrap code. Keep the code secret and do not commit `.env`.
