using ModelContextProtocol.Protocol.Types;

namespace OpenSpartan.HaloInfinite.MCP.Core
{
    public interface IResourceProvider
    {
        // Get the static resource definitions this provider offers
        IEnumerable<Resource> GetResourceDefinitions();

        // Get available resource templates for dynamic resources
        IEnumerable<ResourceTemplate> GetResourceTemplates();

        // Check if this provider can handle a specific URI
        bool CanHandleUri(string uri);

        // Read a resource by URI
        Task<ResourceContents> GetResourceContentsAsync(string uri, CancellationToken cancellationToken);
    }
}
