using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace OpenSpartan.HaloInfinite.MCP.Core
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        JsonElement InputSchema { get; }
        Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken);
    }
}
