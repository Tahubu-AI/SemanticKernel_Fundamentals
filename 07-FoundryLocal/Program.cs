using Microsoft.AI.Foundry.Local;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Agents;


var modelAlias = "Phi-4-mini";

FoundryLocalManager manager = await FoundryLocalManager.StartModelAsync(modelAlias);
ModelInfo? modelInfo = await manager.GetModelInfoAsync(modelAlias);

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelInfo!.ModelId, manager.Endpoint, "")
    .Build();

ChatCompletionAgent agent = new ChatCompletionAgent
{
    Kernel = kernel,
    Instructions = "You are a powerful and helpful local agent"
};

List<ChatMessageContent> history = [];
while (true)
{
    Console.Write("Q:  ");
    var inputFromUser = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(inputFromUser))
    {
        history.Add(new ChatMessageContent(AuthorRole.User, inputFromUser));
        await foreach (AgentResponseItem<StreamingChatMessageContent> response in agent.InvokeStreamingAsync(history))
        {
            history.Add(new ChatMessageContent(AuthorRole.Assistant, response.Message.Content));
            Console.Write(response.Message);
        }
    }

    Console.WriteLine();
}