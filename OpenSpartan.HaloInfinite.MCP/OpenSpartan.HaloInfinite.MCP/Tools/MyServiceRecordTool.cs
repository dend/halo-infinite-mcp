﻿using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.HaloInfinite.MCP.Core;
using System.Text.Json;

namespace OpenSpartan.HaloInfinite.MCP.Tools
{
    public class MyServiceRecordTool : ITool
    {
        public string Name => "opsp_halo_infinite_my_service_record";
        public string Description => "Returns the complete Halo Infinite player service record for matchmade games for the currently authenticated player.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object"
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            //if (!arguments.TryGetValue("message", out var message))
            //{
            //    throw new McpServerException("Missing required argument 'message'");
            //}

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
