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

## Running

Some questions you can ask:

1. What are my latest stats for matchmade games?
1. What were the outcomes for my last 10 matches?
1. What is my current armor configuration?
1. What’s currently available on sale through The Exchange?
1. What’s my current career rank?

![GIF showing querying the Forerunner MCP for career rank data from Claude Desktop](media/claude-desktop-career-rank.gif)
