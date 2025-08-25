using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;


#pragma warning disable SKEXP0110 
#pragma warning disable SKEXP0001

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

            // Retrieve settings from configuration

            string? modelid = config["OpenAI:ModelId"];
            string? apikey = config["OpenAI:ApiKey"];

            Kernel kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(modelid!, apikey!)
                .Build();

            string analystPersona = """
            You are a business analyst. Your role is to gather and understand user requirements, creating detailed documentation outlining the scope and expectations.
            """;

            string developerPersona = """
            You are a frontend developer. Your task is to build a responsive web application using HTML and JavaScript, following the analyst's requirements.
            """;

            string qaPersona = """
            You are a QA engineer. Your responsibility is to thoroughly test the application to ensure it meets all requirements and is free of defects. Approve the product by responding with \"approve\" if it passes all tests.
            """;

            ChatCompletionAgent analystAgent = new()
            {
                Instructions = analystPersona,
                Name = "AnalystAgent",
                Kernel = kernel
            };

            ChatCompletionAgent developerAgent = new()
            {
                Instructions = developerPersona,
                Name = "DeveloperAgent",
                Kernel = kernel
            };

            ChatCompletionAgent qaAgent = new()
            {
                Instructions = qaPersona,
                Name = "QAAgent",
                Kernel = kernel
            };

            AgentGroupChat groupChat = new(analystAgent, developerAgent, qaAgent)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new ApprovalTerminationStrategy()
                    {
                        Agents = [qaAgent],
                        MaximumIterations = 6
                    }
                }
            };

            string userRequest = """
            I need a simple to-do list web app. It should allow adding and removing tasks. Please ensure it's ready for release.
            """;

            groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userRequest));
            Console.WriteLine($"# {AuthorRole.User}: '{userRequest}'");

            await foreach (var message in groupChat.InvokeAsync())
            {
                Console.WriteLine($"# {message.Role} - {message.AuthorName ?? "*"}: '{message.Content}'");
            }
        }
    }

    public sealed class ApprovalTerminationStrategy : TerminationStrategy
    {
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[^1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}