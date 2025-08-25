using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.AzureAIInference;

namespace SK_Demos
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var kernel = Kernel.CreateBuilder()
                .AddAzureAIInferenceChatCompletion(config["DeepSeek:ModelId"]!, config["DeepSeek:ApiKey"]!, new Uri(config["DeepSeek:Endpoint"]!))
                .Build();

            //Step 3: chat History
            var history = new ChatHistory(systemMessage: "You are a helpful assistant that helps people find information.");

            var ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            //Step 2: Prompting
            AzureAIInferencePromptExecutionSettings settings = new()
            {
                MaxTokens = 1000,
                Temperature = 0.7f,
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
                await foreach (StreamingChatMessageContent responseChunk in ChatCompletionService.GetStreamingChatMessageContentsAsync(history, settings))
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

                Console.WriteLine($"Bot: {fullMessage}");
                if (usage != null)
                {
                    Console.WriteLine($"Input Tokens: {usage.InputTokenCount}");
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
    }
}