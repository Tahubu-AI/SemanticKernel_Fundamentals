using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var deploymentName = "gpt-4o";
var endpoint = "replace";
var apiKey = "replace";

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();

var ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

while (true)
{
    Console.Write("User: ");
    var prompt = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(prompt)) break;
    var response = await ChatCompletionService.GetChatMessageContentAsync(prompt);
    Console.WriteLine($"Bot: {response}");
}