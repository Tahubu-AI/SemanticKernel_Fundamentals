using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plugins;
using Azure.Search.Documents.Indexes;
using Azure;

#pragma warning disable SKEXP0010

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


            // Retrieve settings from configuration, not used in this code, just for demonstration
            string? endpoint = config["ENDPOINT"];
            string? apikey = config["API_KEY"];
            string? deploymentName = config["DEPLOYMENT_NAME"];
            string? embeddingDeploymentName = config["EMBEDDING_DEPLOYNAME"];
            string? aiSearchEndpoint = config["AI_SEARCH_ENDPOINT"];
            string? aiSearchKey = config["AI_SEARCH_KEY"];



            var builder = Kernel.CreateBuilder();


            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddAzureOpenAIChatCompletion(deploymentName!, endpoint!, apikey!);

            //RAG: Add Text Embedding 
            builder.Services.AddAzureOpenAIEmbeddingGenerator(embeddingDeploymentName!, endpoint!, apikey!);

             //RAG: Add AI Search
            builder.Services.AddSingleton(sp => new SearchIndexClient(new Uri(aiSearchEndpoint!), new AzureKeyCredential(aiSearchKey!)));
            builder.Services.AddAzureAISearchVectorStore();

            // disable concurrent invocation of functions
            FunctionChoiceBehaviorOptions options = new() { AllowConcurrentInvocation = false };

            builder.Services.AddTransient<PromptExecutionSettings>( _ => new OpenAIPromptExecutionSettings {
                Temperature = 0.75,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: options)
            });

            var kernelBuilder = builder.Services.AddKernel();

            kernelBuilder.Plugins.AddFromType<FloridaDMVRAG>();

            var kernel = kernelBuilder.Services.BuildServiceProvider().GetRequiredService<Kernel>();

            var ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            
            var history = new ChatHistory(systemMessage: "You are a helpful assistant that helps people find information about Driving in Florida, only reply with responses based on information available in the context being passed to you.");

            while (true)
            {

                Console.Write("User: ");
                var prompt = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(prompt)) break;

                // Add user message to history
                history.AddUserMessage(prompt);

                // Get execution settings
                var settings = kernel.Services.GetRequiredService<PromptExecutionSettings>();

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

                // Add assistant response to history
                history.AddAssistantMessage(fullMessage);

                Console.WriteLine();
                if (usage != null)
                {
                    Console.WriteLine($"Input Tokens: {usage.InputTokenCount}");
                    Console.WriteLine($"Output Tokens: {usage.OutputTokenCount}");
                    Console.WriteLine($"Total tokens:  {usage.TotalTokenCount}");
                }
                Console.WriteLine();
            }
        }
    }
}