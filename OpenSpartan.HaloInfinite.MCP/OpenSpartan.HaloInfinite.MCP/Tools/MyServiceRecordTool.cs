using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.Forerunner.MCP.Core;
using System.Text.Json;

namespace OpenSpartan.Forerunner.MCP.Tools
{
    public class MyServiceRecordTool : ITool
    {
        public string Name => "opsp_my_service_record";
        public string Description => "Returns the complete Halo Infinite player service record for matchmade games for the currently authenticated player. This tool does not have the career rank.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object"
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            var playerStats = await HaloInfiniteAPIBridge.SafeAPICall(async () => await HaloInfiniteAPIBridge.HaloClient.StatsGetPlayerServiceRecord($"xuid({HaloInfiniteAPIBridge.HaloClient.Xuid})", Den.Dev.Grunt.Models.HaloInfinite.LifecycleMode.Matchmade));

            if (playerStats != null)
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = playerStats.Response.Message, Type = "text", MimeType = "application/json" }]
                };
            }
            else
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = "No API endpoints could be obtained.", Type = "text" }]
                };
            }
        }
    }
}
