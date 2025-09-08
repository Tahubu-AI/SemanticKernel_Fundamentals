using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var deploymentName = "gpt-4o";
var endpoint = "replace";
var apiKey = "replace";

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();

//Step 2: Prompting
OpenAIPromptExecutionSettings settings = new()
{
    MaxTokens = 1000,
    Temperature = 0.7,
    ChatSystemPrompt = "You are a helpful assistant that helps people find information.",  
};

//Step 3: chat History
var history = new ChatHistory();

var ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

//Step 4: Reducers
//var reducer = new ChatHistoryTruncationReducer(targetCount: 2);
var reducer = new ChatHistorySummarizationReducer(ChatCompletionService, 2, 2);

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

    //Goes with Step3: Chat History
    //history.Add(response);  //System is pretty intelligent to uderstand that the add is is for AddAsssistantMessage
    history.AddAssistantMessage(fullMessage);
    //Step 2: Tokens
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n\nBot: {fullMessage}");
    Console.ResetColor();
    Console.WriteLine($"Input Tokens: {usage!.InputTokenCount}");
    Console.WriteLine($"Output Tokens: {usage.OutputTokenCount}");
    Console.WriteLine($"Total tokens:  {usage.TotalTokenCount}");
    

    var reduceMessages = await reducer.ReduceAsync(history);
    if (reduceMessages is not null)
    {
        history = new(reduceMessages);
    }

}