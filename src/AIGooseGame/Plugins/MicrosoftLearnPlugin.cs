using System.ComponentModel;
using System.Text.Json;

namespace AIGooseGame.Plugins;

/// <summary>
/// Plugin che recupera documentazione da Microsoft Learn tramite la Search API pubblica.
/// Il tool è un puro client HTTP: nessuna chiamata LLM interna.
/// La generazione del quiz è responsabilità dell'agente (IChatClient via MAF).
/// </summary>
public class MicrosoftLearnPlugin(HttpClient httpClient)
{
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

    [Description("Cerca su Microsoft Learn documentazione per un argomento .NET/Azure casuale. " +
                 "Restituisce titolo, descrizione e URL della pagina trovata. " +
                 "Usa questo contenuto per formulare TU STESSO una domanda quiz in italiano con 3 opzioni (A, B, C).")]
    public async Task<string> SearchMicrosoftLearn()
    {
        var topic = DotNetTopics[Random.Shared.Next(DotNetTopics.Length)];

        try
        {
            var encoded = Uri.EscapeDataString(topic);
            var json = await httpClient.GetFromJsonAsync<JsonElement>(
                $"https://learn.microsoft.com/api/search?search={encoded}&locale=en-US&$top=2&facet=category");

            var items = new List<object>();
            if (json.TryGetProperty("results", out var results))
            {
                foreach (var item in results.EnumerateArray().Take(2))
                {
                    items.Add(new
                    {
                        title       = item.TryGetProperty("title",       out var t) ? t.GetString() : topic,
                        description = item.TryGetProperty("description", out var d) ? d.GetString() : "",
                        url         = item.TryGetProperty("url",         out var u) ? u.GetString() : "https://learn.microsoft.com"
                    });
                }
            }

            return JsonSerializer.Serialize(new { topic, results = items, source = "learn.microsoft.com" });
        }
        catch
        {
            // Restituisce il solo argomento: l'agente può comunque generare una domanda
            // basandosi sulla sua conoscenza del topic
            return JsonSerializer.Serialize(new
            {
                topic,
                results = new[] { new { title = topic, description = (string?)null, url = "https://learn.microsoft.com/dotnet/" } },
                source  = "fallback"
            });
        }
    }
}
