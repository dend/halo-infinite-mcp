using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.HaloInfinite.MCP.Core;
using OpenSpartan.HaloInfinite.MCP.Resources;
using OpenSpartan.HaloInfinite.MCP.Tools;
using System.Text;

namespace OpenSpartan.HaloInfinite.MCP
{
    internal class Program
    {
        private static readonly HashSet<string> _subscribedResources = new();
        private static readonly object _subscribedResourcesLock = new();

        static async Task Main(string[] args)
        {
            var authResult = await AuthHandler.InitializePublicClientApplication();

            if (authResult != null)
            {
                bool isHaloClientReady = await HaloInfiniteAPIBridge.InitializeHaloClient(authResult);

                if (isHaloClientReady)
                {
                    McpServerOptions options = new()
                    {
                        ServerInfo = new Implementation() { Name = "OpenSpartan Halo Infinite MCP Server", Version = "1.0.0" },
                        Capabilities = new ServerCapabilities()
                        {
                            Tools = ConfigureTools(),
                            Resources = ConfigureResources(),
                            //Prompts = ConfigurePrompts(),
                            //Logging = ConfigureLogging()
                        },
                        ProtocolVersion = "2024-11-05",
                        ServerInstructions = "A MCP server designed to help access data from the Halo Infinite API inside an LLM.",
                        GetCompletionHandler = ConfigureCompletion(),
                    };

                    await using IMcpServer server = McpServerFactory.Create(new StdioServerTransport("OpenSpartan Halo Infinite MCP Server"), options);

                    await server.StartAsync();

                    await Task.Delay(Timeout.Infinite);
                }
                else
                {
                    Console.WriteLine("Halo Infinite client could not be initialized.");
                }
            }
            else
            {
                Console.WriteLine("Could not authenticate the user. This MCP server requires user credentials to work.");
            }
        }

        private static ResourcesCapability ConfigureResources()
        {
            // Create resource providers
            var resourceProviders = new IResourceProvider[]
            {
                new EndpointSettingsResource(),
                new MyServiceRecordResource()
            };

            // Create resource manager
            var resourceManager = new ResourceManager(resourceProviders);

            const int pageSize = 10;

            return new()
            {
                ListResourceTemplatesHandler = (request, cancellationToken) =>
                {
                    var templates = resourceManager.GetAllResourceTemplates().ToList();

                    return Task.FromResult(new ListResourceTemplatesResult()
                    {
                        ResourceTemplates = templates
                    });
                },

                ListResourcesHandler = (request, cancellationToken) =>
                {
                    var allResources = resourceManager.GetAllResources().ToList();

                    int startIndex = 0;
                    if (request.Params?.Cursor is not null)
                    {
                        try
                        {
                            var startIndexAsString = Encoding.UTF8.GetString(Convert.FromBase64String(request.Params.Cursor));
                            startIndex = Convert.ToInt32(startIndexAsString);
                        }
                        catch
                        {
                            throw new McpServerException("Invalid cursor");
                        }
                    }

                    int endIndex = Math.Min(startIndex + pageSize, allResources.Count);
                    string? nextCursor = null;

                    if (endIndex < allResources.Count)
                    {
                        nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(endIndex.ToString()));
                    }

                    return Task.FromResult(new ListResourcesResult()
                    {
                        NextCursor = nextCursor,
                        Resources = allResources.GetRange(startIndex, endIndex - startIndex)
                    });
                },

                ReadResourceHandler = async (request, cancellationToken) =>
                {
                    if (request.Params?.Uri is null)
                    {
                        throw new McpServerException("Missing required argument 'uri'");
                    }

                    var contents = await resourceManager.GetResourceContentsAsync(request.Params.Uri, cancellationToken);

                    return new ReadResourceResult()
                    {
                        Contents = [contents]
                    };
                },

                SubscribeToResourcesHandler = (request, cancellationToken) =>
                {
                    if (request?.Params?.Uri is null)
                    {
                        throw new McpServerException("Missing required argument 'uri'");
                    }

                    resourceManager.SubscribeToResource(request.Params.Uri);

                    return Task.FromResult(new EmptyResult());
                },

                UnsubscribeFromResourcesHandler = (request, cancellationToken) =>
                {
                    if (request?.Params?.Uri is null)
                    {
                        throw new McpServerException("Missing required argument 'uri'");
                    }

                    resourceManager.UnsubscribeFromResource(request.Params.Uri);

                    return Task.FromResult(new EmptyResult());
                },

                Subscribe = true
            };
        }

        public static ToolsCapability ConfigureTools()
        {
            var tools = new List<ITool>
            {
                new EndpointSettingsTool(),
                new MyServiceRecordTool(),
            };

            return new ToolsCapability
            {
                ListToolsHandler = (request, cancellationToken) =>
                {
                    return Task.FromResult(new ListToolsResult()
                    {
                        Tools = [.. tools.Select(t => new Tool
                        {
                            Name = t.Name,
                            Description = t.Description,
                            InputSchema = t.InputSchema
                        })]
                    });
                },

                CallToolHandler = async (request, cancellationToken) =>
                {
                    var toolName = request.Params?.Name;
                    var tool = tools.FirstOrDefault(t => t.Name == toolName);

                    if (tool == null)
                    {
                        throw new McpServerException($"Unknown tool: {toolName}");
                    }

                    return await tool.ExecuteAsync(
                        request.Params?.Arguments ?? default,
                        request.Server,
                        cancellationToken);
                }
            };
        }

        private static Func<RequestContext<CompleteRequestParams>, CancellationToken, Task<CompleteResult>> ConfigureCompletion()
        {
            List<string> sampleResourceIds = ["1", "2", "3", "4", "5"];
            Dictionary<string, List<string>> exampleCompletions = new()
            {
                {"style", ["casual", "formal", "technical", "friendly"]},
                {"temperature", ["0", "0.5", "0.7", "1.0"]},
            };

            return (request, cancellationToken) =>
            {
                if (request.Params?.Ref?.Type == "ref/resource")
                {
                    var resourceId = request.Params?.Ref?.Uri?.Split('/').LastOrDefault();
                    if (string.IsNullOrEmpty(resourceId))
                        return Task.FromResult(new CompleteResult() { Completion = new() { Values = [] } });

                    // Filter resource IDs that start with the input value
                    var values = sampleResourceIds.Where(id => id.StartsWith(request.Params!.Argument.Value)).ToArray();
                    return Task.FromResult(new CompleteResult() { Completion = new() { Values = values, HasMore = false, Total = values.Length } });

                }

                if (request.Params?.Ref?.Type == "ref/prompt")
                {
                    // Handle completion for prompt arguments
                    if (!exampleCompletions.TryGetValue(request.Params.Argument.Name, out var completions))
                        return Task.FromResult(new CompleteResult() { Completion = new() { Values = [] } });

                    var values = completions.Where(value => value.StartsWith(request.Params.Argument.Value)).ToArray();
                    return Task.FromResult(new CompleteResult() { Completion = new() { Values = values, HasMore = false, Total = values.Length } });
                }

                throw new McpServerException($"Unknown reference type: {request.Params?.Ref.Type}");
            };
        }
    }
}
