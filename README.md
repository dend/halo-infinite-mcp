<div align="center">
	<img src="media/forerunner-logo.webp" width="200" height="200">
	<h1>OpenSpartan Forerunner</h1>
	<p>
		<b>MCP server connecting you to Halo Infinite data.</b>
	</p>
	<br>
	<br>
	<br>
</div>

Forerunner is a custom-built **local MCP server** that allows you to connect to your Halo Infinite data through whatever MCP client you're using.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Get started

1. Download the [latest release](https://github.com/dend/halo-infinite-mcp/releases).
1. Extract the package locally.
1. Update your MCP client configuration to point to the `OpenSpartan.Forerunner.MCP.dll` with the `dotnet` command as the bootstrap tool.

### Example configurations

#### Visual Studio Code

>[!IMPORTANT]
>You will need to install [Visual Studio Code Insiders](https://code.visualstudio.com/insiders/) for MCP support.

```json
"mcp": {
    "inputs": [],
    "servers": {
        "mcp-halo-infinite": {
            "command": "dotnet",
            "args": [
                "PATH_TO_YOUR_OpenSpartan.Forerunner.MCP.dll",
            ],
            "env": {}
        }
    }
}
```

#### Claude Desktop

```json
"mcpServers": {
    "mcp-halo-infinite": {
        "command": "dotnet",
        "args": [
            "PATH_TO_YOUR_OpenSpartan.Forerunner.MCP.dll"
        ],
        "env": {}
    }
}
```

## Supported tools

>[!IMPORTANT]
>I am constantly adding new tools. This table will be updated as new ones are introduced.

| Tool Name | Description |
|-----------|-------------|
| `opsp_api_endpoints` | Returns a JSON-formatted list of all available endpoints that exist in the Halo Infinite REST API surface. |
| `opsp_my_service_record` | Returns the complete Halo Infinite player service record for matchmade games for the currently authenticated player. This tool does not have the career rank. |
| `opsp_my_latest_matches` | Returns the stats for a player's latest Halo Infinite matches. This includes all match types, such as matchmade games, custom games, and LAN games. All match dates returned in UTC. |
| `opsp_exchange_list` | Lists all of the items that are currently available on the Halo Infinite exchange. |
| `opsp_my_gear_configuration` | Returns Halo Infinite customizations with their images for the authenticated user. |
| `opsp_my_career_rank` | Returns the player's current Halo Infinite career rank (or level) and progress to the top level (Hero). The player earns experience with every match and might want to know how long until Hero rank. |

## Running

Some questions you can ask:

1. What are my latest stats for matchmade games?
1. What were the outcomes for my last 10 matches?
1. What is my current armor configuration?
1. What’s currently available on sale through The Exchange?
1. What’s my current career rank?

![GIF showing querying the Forerunner MCP for career rank data from Claude Desktop](media/claude-desktop-career-rank.gif)
