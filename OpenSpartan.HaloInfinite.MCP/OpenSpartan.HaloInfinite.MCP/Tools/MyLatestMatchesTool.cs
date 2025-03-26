using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.HaloInfinite.MCP.Core;
using Serilog;
using System.Text.Json;

namespace OpenSpartan.HaloInfinite.MCP.Tools
{
    public class MyLatestMatchesTool : ITool
    {
        public string Name => "opsp_my_latest_matches";
        public string Description => "Returns the stats for a player's latest Halo Infinite matches. This includes all match types, such as matchmade games, custom games, and LAN games.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
              "start": {
                "type": "integer",
                "description": "Starting index of matches to lookup."
              },
              "count": {
                "type": "integer",
                "description": "Number of matches to return. Maximum is 25."
              }
            }
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            Log.Logger.Information($"Arguments: {JsonSerializer.Serialize(arguments)}");

            arguments.TryGetValue("start", out var start);
            arguments.TryGetValue("count", out var count);

            int startValue = start != null && int.TryParse(start.ToString(), out var s) ? s : 0;
            int countValue = count != null && int.TryParse(count.ToString(), out var c) ? c : 25;

            Log.Logger.Information($"Start value is {startValue} and count value is {countValue}");

            var matches = await HaloInfiniteAPIBridge.SafeAPICall(async () => await HaloInfiniteAPIBridge.HaloClient.StatsGetMatchHistory($"xuid({HaloInfiniteAPIBridge.HaloClient.Xuid})", startValue, countValue, Den.Dev.Grunt.Models.HaloInfinite.MatchType.All));

            if (matches != null)
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = matches.Response.Message, Type = "text", MimeType = "application/json" }]
                };
            }
            else
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = "No matches could be obtained.", Type = "text" }]
                };
            }
        }
    }
}
