using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;


#pragma warning disable SKEXP0110
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

            // Retrieve settings from configuration, not used in this code, just for demonstration
            string? modelid = config["OpenAI:ModelId"];
            string? apikey = config["OpenAI:ApiKey"];

            // Create a service collection and register HttpClientFactory
            var services = new ServiceCollection();
            services.AddHttpClient();

            var kernelBuilder = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(modelid!, apikey!);

            // Add the services to the kernel builder
            kernelBuilder.Services.AddHttpClient();
            kernelBuilder.Services.AddSingleton<IConfiguration>(config);

            Kernel kernel = kernelBuilder.Build();

            ChatCompletionAgent agent = new()
            {
                Instructions = "Answer weather-related questions",
                Name = "WeatherBot",
                Kernel = kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()

                }),
            };

            // Add plugins with service provider for dependency injection
            agent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<MyPlugins.GetGeoCoordinates>(serviceProvider: kernel.Services));
            agent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<MyPlugins.GetWeather>(serviceProvider: kernel.Services));

            ChatHistory chat = [];

            await InvokeAgentAsync(agent, chat, "What is the weather like in Orlando, FL?");
            await InvokeAgentAsync(agent, chat, "What is the weather like in San Diego, CA?");
        }

        private static async Task InvokeAgentAsync(ChatCompletionAgent agent, ChatHistory chat, string input)
        {
            ChatMessageContent message = new(AuthorRole.User, input);
            chat.Add(message);
            Console.WriteLine(message);
            await foreach (var response in agent.InvokeAsync(chat))
            {
                chat.Add(response);
                Console.Write(response.Message);
            }
        }
    }
}