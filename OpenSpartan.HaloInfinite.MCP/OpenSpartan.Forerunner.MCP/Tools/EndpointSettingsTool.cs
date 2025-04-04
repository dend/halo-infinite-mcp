﻿using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.Forerunner.MCP.Core;
using System.Text.Json;

namespace OpenSpartan.Forerunner.MCP.Tools
{
    public class EndpointSettingsTool : ITool
    {
        public string Name => "opsp_api_endpoints";
        public string Description => "Returns a JSON-formatted list of all available endpoints that exist in the Halo Infinite REST API surface.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object"
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            var endpointSettings = await HaloInfiniteAPIBridge.SafeAPICall(async () => await HaloInfiniteAPIBridge.HaloClient.GetApiSettingsContainer());

            if (endpointSettings != null)
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = endpointSettings.Response.Message, Type = "text", MimeType = "application/json" }]
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