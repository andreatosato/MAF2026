using System.ComponentModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Responses;

namespace AIGooseGame.Plugins;

#pragma warning disable OPENAI001

/// <summary>
/// Plugin che usa le OpenAI Responses API con MCP Tool per cercare su Microsoft Learn 📚
/// Questo è un esempio di integrazione MCP (Model Context Protocol) con il server pubblico
/// di Microsoft Learn: https://learn.microsoft.com/api/mcp
/// </summary>
public class MicrosoftLearnPlugin
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly string _deploymentName;

    private static readonly string[] DotNetTopics =
    [
        ".NET Aspire distributed applications",
        "Blazor interactive web UI",
        "Entity Framework Core database",
        "MAUI cross-platform apps",
        "Minimal APIs in ASP.NET Core",
        "SignalR real-time communication",
        "C# pattern matching features",
        "Microsoft Agent Framework AI agents",
        "Azure Functions serverless",
        "Azure Container Apps deployment",
        "OpenTelemetry in .NET",
        "Dependency Injection in .NET",
        "gRPC services in ASP.NET Core",
        "Azure Cosmos DB with .NET",
        ".NET 10 new features"
    ];

    public MicrosoftLearnPlugin(AzureOpenAIClient azureClient, string deploymentName)
    {
        _azureClient = azureClient;
        _deploymentName = deploymentName;
    }

    [Description("Cerca su Microsoft Learn tramite MCP Server e genera una domanda quiz su .NET/Azure. " +
                 "Usa il protocollo MCP (Model Context Protocol) per accedere alla documentazione Microsoft Learn in tempo reale. " +
                 "Restituisce una domanda con 3 opzioni (A, B, C), la risposta corretta e una spiegazione.")]
    public async Task<string> SearchAndCreateQuiz()
    {
        var topic = DotNetTopics[Random.Shared.Next(DotNetTopics.Length)];

        try
        {
            var responsesClient = _azureClient.GetResponsesClient();

            var prompt =
                $"Cerca su Microsoft Learn informazioni riguardo: \"{topic}\".\n" +
                "Basandoti su quello che trovi, genera UNA domanda quiz in italiano a risposta multipla.\n\n" +
                "Rispondi SOLO in formato JSON valido (senza markdown code blocks):\n" +
                "{\n" +
                "  \"topic\": \"nome argomento\",\n" +
                "  \"question\": \"domanda in italiano\",\n" +
                "  \"options\": {\n" +
                "    \"A\": \"prima opzione\",\n" +
                "    \"B\": \"seconda opzione\",\n" +
                "    \"C\": \"terza opzione\"\n" +
                "  },\n" +
                "  \"correct\": \"A\",\n" +
                "  \"explanation\": \"spiegazione breve in italiano\",\n" +
                "  \"learnUrl\": \"URL della pagina Microsoft Learn trovata\"\n" +
                "}";

            var mcpTool = ResponseTool.CreateMcpTool(
                serverLabel: "microsoft_learn",
                serverUri: new Uri("https://learn.microsoft.com/api/mcp"));

            var options = new CreateResponseOptions
            {
                Model = _deploymentName,
                InputItems = { ResponseItem.CreateUserMessageItem(prompt) },
                Tools = { mcpTool }
            };

            var result = await responsesClient.CreateResponseAsync(options);
            return result.Value.GetOutputText();
        }
        catch (Exception ex)
        {
            // Fallback: genera una domanda statica se il MCP server non è raggiungibile
            return JsonSerializer.Serialize(new
            {
                topic,
                question = $"Quale di queste affermazioni su {topic} è corretta?",
                options = new Dictionary<string, string>
                {
                    ["A"] = "È un servizio esclusivamente cloud",
                    ["B"] = "È una tecnologia open-source di Microsoft",
                    ["C"] = "È disponibile solo su Windows"
                },
                correct = "B",
                explanation = $"La maggior parte delle tecnologie .NET moderne sono open-source! Visita learn.microsoft.com per scoprire di più su {topic}.",
                learnUrl = "https://learn.microsoft.com/dotnet/",
                mcpFallback = true,
                mcpError = ex.Message
            });
        }
    }
}
