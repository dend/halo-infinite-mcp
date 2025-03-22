using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace OpenSpartan.HaloInfinite.MCP.Core
{
    public class ResourceManager
    {
        private readonly IEnumerable<IResourceProvider> _resourceProviders;
        private readonly object _subscribedResourcesLock = new object();
        private readonly HashSet<string> _subscribedResources = new HashSet<string>();

        public ResourceManager(IEnumerable<IResourceProvider> resourceProviders)
        {
            _resourceProviders = resourceProviders ?? throw new ArgumentNullException(nameof(resourceProviders));
        }

        public IEnumerable<Resource> GetAllResources()
        {
            foreach (var provider in _resourceProviders)
            {
                foreach (var resource in provider.GetResourceDefinitions())
                {
                    yield return resource;
                }
            }
        }

        public IEnumerable<ResourceTemplate> GetAllResourceTemplates()
        {
            foreach (var provider in _resourceProviders)
            {
                foreach (var template in provider.GetResourceTemplates())
                {
                    yield return template;
                }
            }
        }

        public async Task<ResourceContents> GetResourceContentsAsync(string uri, CancellationToken cancellationToken)
        {
            var provider = _resourceProviders.FirstOrDefault(p => p.CanHandleUri(uri));

            if (provider == null)
            {
                throw new McpServerException($"No provider found for URI: {uri}");
            }

            return await provider.GetResourceContentsAsync(uri, cancellationToken);
        }

        public bool IsValidResourceUri(string uri)
        {
            return _resourceProviders.Any(p => p.CanHandleUri(uri));
        }

        public void SubscribeToResource(string uri)
        {
            if (!IsValidResourceUri(uri))
            {
                throw new McpServerException("Invalid resource URI");
            }

            lock (_subscribedResourcesLock)
            {
                _subscribedResources.Add(uri);
            }
        }

        public void UnsubscribeFromResource(string uri)
        {
            if (!IsValidResourceUri(uri))
            {
                throw new McpServerException("Invalid resource URI");
            }

            lock (_subscribedResourcesLock)
            {
                _subscribedResources.Remove(uri);
            }
        }

        public IEnumerable<string> GetSubscribedResources()
        {
            lock (_subscribedResourcesLock)
            {
                return _subscribedResources.ToList();
            }
        }
    }
}
