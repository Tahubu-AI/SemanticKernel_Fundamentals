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

            string BahiaConcierge = """
            You are an informative hotel concierge. Your role is to gather and 
            understand user requirements and reply to guests with professionalism 
            regarding the hotel and room Remember that the WIFI password is "Bahia@1!".
            """;

            string DockSide1953 = """
            You are the manager of the DockerSide 1953 restaurant at the Bahia Resort 
            in San Diego. Your task is to answer questions about the restaurant and 
            recommend it to hotel guests.
            """;

            string BahiaCruises = """
            You are the Bahia resort Cruise cordinator at the Bahia Resort in San Diego. 
            Your responsibility is to answer questions about the cruises offered by the 
            resort and to recommend them to hotel guests.
            """;

            ChatCompletionAgent BahiaConciergeAgent = new()
            {
                Instructions = BahiaConcierge,
                Name = "BahiaConciergeAgent",
                Kernel = kernel
            };

            ChatCompletionAgent DockSide1953Agent = new()
            {
                Instructions = DockSide1953,
                Name = "DockSide1953Agent",
                Kernel = kernel
            };

            ChatCompletionAgent BahiaCruisesAgent = new()
            {
                Instructions = BahiaCruises,
                Name = "BahiaCruisesAgent",
                Kernel = kernel
            };

            AgentGroupChat groupChat = new(BahiaConciergeAgent, DockSide1953Agent, BahiaCruisesAgent)
            {
                ExecutionSettings = new()
                {
                    TerminationStrategy = new ApprovalTerminationStrategy()
                    {
                        MaximumIterations = 6
                    }
                }
            };

            string userRequest = """
            I am in my room but need the Wifi password. Also, can you recommend a good place for dinner tonight?
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
            => Task.FromResult(history[^1].Content?.Contains("done", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}