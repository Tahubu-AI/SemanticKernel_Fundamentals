using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using System.Security;
using ModelContextProtocol.Client;

namespace SK_Demos
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Retrieve settings from configuration

            string? modelid = config["OpenAI:ModelId"];
            string? apikey = config["OpenAI:ApiKey"];
            string? GitHub_PAT = config["GitHub_Pat"];

  
            var builder = Kernel.CreateBuilder();
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddOpenAIChatCompletion(modelid!, apikey!);
            
            //var kernel = builder.Build();
            var kernelBuilder = builder.Services.AddKernel();

            //Add MCP Servers

            await AddFileSystemMcpServer(kernelBuilder);
            await AddGithubMcpServer(kernelBuilder, GitHub_PAT!);


            //Step 3: chat History
            var history = new ChatHistory(systemMessage: "You are a helpful assistant that helps people find information.");

            var kernel = kernelBuilder.Services.BuildServiceProvider().GetRequiredService<Kernel>();

            var ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            FunctionChoiceBehaviorOptions options = new() { AllowConcurrentInvocation = true };
            OpenAIPromptExecutionSettings settings = new()
            {
                MaxTokens = 1000,
                Temperature = 0.7,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: options),
            };

            //var reducer = new ChatHistoryTruncationReducer(targetCount: 2);
            var reducer = new ChatHistorySummarizationReducer(ChatCompletionService, 2, 2);

            foreach (var attrib in ChatCompletionService.Attributes)
            {
                Console.WriteLine($"{attrib.Key} \t\t {attrib.Value}");
            }

            while (true)
            {

                Console.Write("User: ");
                var prompt = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(prompt)) break;

                history.AddUserMessage(prompt);

                //var response = await ChatCompletionService.GetChatMessageContentAsync(history, settings);
                string fullMessage = "";
                OpenAI.Chat.ChatTokenUsage? usage = null;
                await foreach (StreamingChatMessageContent responseChunk in ChatCompletionService.GetStreamingChatMessageContentsAsync(history, settings, kernel))
                {
                    Console.Write(responseChunk.Content);
                    fullMessage += responseChunk.Content;
                    var streamingUpdate = responseChunk.InnerContent as OpenAI.Chat.StreamingChatCompletionUpdate;
                    if (streamingUpdate?.Usage != null)
                    {
                        usage = streamingUpdate.Usage;
                    }
                }

                //Step3: Chat History
                //history.Add(response);  //System is pretty intelligent to uderstand that the add is is for AddAsssistantMessage
                history.AddAssistantMessage(fullMessage);
               
                if (usage != null)
                {
                    Console.WriteLine($"\nInput Tokens: {usage.InputTokenCount}");
                    Console.WriteLine($"Output Tokens: {usage.OutputTokenCount}");
                    Console.WriteLine($"Total tokens:  {usage.TotalTokenCount}");
                }

                var reduceMessages = await reducer.ReduceAsync(history);
                if (reduceMessages is not null)
                {
                    history = new(reduceMessages);
                }
            }
        }

        private static async Task AddFileSystemMcpServer(IKernelBuilder kernelBuilder)
        {
            var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new()
            {
                Name = "Github",
                Command = "npx",
                Arguments = ["-y", "@modelcontextprotocol/server-filesystem","D:\\SemanticKernel_Fundamentals\\15-MCP\\MCPData"]
            }));

            var tools = await mcpClient.ListToolsAsync();

            kernelBuilder.Plugins.AddFromFunctions("FS", tools.Select(skf =>skf.AsKernelFunction()));   
        }

        private static async Task AddGithubMcpServer(IKernelBuilder kernelBuilder, string GitHub_PAT)
        {
            var mcpClient = await McpClientFactory.CreateAsync(new SseClientTransport(new()
            {
                Name = "Github",
                Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {GitHub_PAT}" 
                }   
            }));
            
            var tools = await mcpClient.ListToolsAsync();

            kernelBuilder.Plugins.AddFromFunctions("GH", tools.Select(skf =>skf.AsKernelFunction()));
        }
    }
}