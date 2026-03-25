using AIGooseGame;
using AIGooseGame.Agents;
using AIGooseGame.Plugins;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// ─────────────────────────────────────────────────────────────────────────────
// Builder & Configuration
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Azure OpenAI Chat Client ────────────────────────────────────────────────────
var endpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"];
if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("https://YOUR-"))
    throw new InvalidOperationException(
        "Configura 'AZURE_OPENAI_ENDPOINT' in appsettings.json o come variabile d'ambiente. " +
        "Esempio: https://YOUR-RESOURCE.openai.azure.com/");

var deploymentName = builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "gpt-4o-mini";

AzureOpenAIClient azureClient = new(new Uri(endpoint), new DefaultAzureCredential());
IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

// ─────────────────────────────────────────────────────────────────────────────
// 3️⃣ ASPIRE / OpenTelemetry — AI Logging & Tracing
// ─────────────────────────────────────────────────────────────────────────────
// Configurazione OpenTelemetry compatibile con .NET Aspire Dashboard.
// Avvia la dashboard con: docker run --rm -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard
// Poi apri http://localhost:18888 per visualizzare log, trace e metriche degli agenti.

var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("AIGooseGame"))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint));
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint));
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AIGooseGame"));
    options.AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint));
});

// Servizi di base ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddSingleton<GameState>();
builder.Services.AddSingleton<PublicApiPlugin>(sp =>
    new PublicApiPlugin(sp.GetRequiredService<IHttpClientFactory>().CreateClient()));

// ─────────────────────────────────────────────────────────────────────────────
// Agenti 🎲 Gioco dell'Oca + Workflow Handoff
// ─────────────────────────────────────────────────────────────────────────────

builder.AddGooseGameAgents(chatClient, azureClient, deploymentName);

// ─────────────────────────────────────────────────────────────────────────────
// OpenAI Responses + Conversations + DevUI
// ─────────────────────────────────────────────────────────────────────────────

builder.AddOpenAIResponses();
builder.AddOpenAIConversations();
builder.AddDevUI();

// ─────────────────────────────────────────────────────────────────────────────
// 5️⃣ Salvataggio Conversazione su Cosmos DB
// ─────────────────────────────────────────────────────────────────────────────
// Configurazione opzionale: se COSMOS_CONNECTION_STRING è presente, abilita
// il salvataggio automatico della cronologia chat su Azure Cosmos DB.
// La classe CosmosChatHistoryProvider salva ogni messaggio in un container Cosmos.

var cosmosConnectionString = builder.Configuration["COSMOS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    var cosmosDatabase = builder.Configuration["COSMOS_DATABASE_NAME"] ?? "GooseGameDB";
    var cosmosContainer = builder.Configuration["COSMOS_CONTAINER_NAME"] ?? "ChatHistory";

    // Registra CosmosChatHistoryProvider come servizio singleton.
    // Il provider salva automaticamente la cronologia della conversazione su Cosmos DB.
    builder.Services.AddSingleton(sp =>
    {
        var options = new ChatClientAgentOptions();
        options.WithCosmosDBChatHistoryProvider(cosmosConnectionString, cosmosDatabase, cosmosContainer);
        return options.ChatHistoryProvider!;
    });
}

// ─────────────────────────────────────────────────────────────────────────────
// App & Endpoints
// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

// ─── Game UI — serve game.html come pagina principale ────────────────────────
app.MapGet("/", () =>
{
    // Cerca in ordine: output dir → project dir (dev con dotnet run)
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "Pages", "game.html"),
        Path.Combine(Directory.GetCurrentDirectory(), "Pages", "game.html"),
    };
    var htmlPath = candidates.FirstOrDefault(File.Exists);
    if (htmlPath is null)
        return Results.NotFound("game.html non trovato. Assicurati che Pages/game.html esista.");

    var html = File.ReadAllText(htmlPath);
    return Results.Content(html, "text/html");
})
.WithName("GameUI")
.WithSummary("UI interattiva del Gioco dell'Oca")
.ExcludeFromDescription();

// ─── REST Endpoints Gioco dell'Oca ────────────────────────────────────────────

app.MapPost("/game/join/{playerName}", (string playerName, GameState gameState, bool? isHuman) =>
{
    var player = gameState.JoinGame(playerName, isHuman ?? false);
    var type = player.IsHuman ? "🧑 Giocatore Umano" : "🤖 Giocatore AI";
    return Results.Ok(new
    {
        message = $"🎉 Benvenuto {player.Name}! ({type}) Sei pronto a giocare al Gioco dell'Oca!",
        player,
        board = $"Tabellone: START → [1]🐶→[2]😂→[3]🐱→[4]🍹→[5]🎮→[6]🎲→...→[20]🏆 FINE"
    });
})
.WithName("JoinGame")
.WithSummary("Entra nella partita del Gioco dell'Oca");

app.MapGet("/game/scoreboard", (GameState gameState) =>
{
    var scoreboard = gameState.GetScoreboard();
    return Results.Ok(new
    {
        title = "🏆 Classifica Gioco dell'Oca",
        players = scoreboard.Select((p, i) => new
        {
            rank = i + 1,
            name = p.Name,
            position = p.Position,
            turnsPlayed = p.TurnsPlayed,
            hasFinished = p.HasFinished,
            squareType = GameState.BoardSquares[Math.Min(p.Position, GameState.BoardSize)],
            isHuman = p.IsHuman
        })
    });
})
.WithName("GetScoreboard")
.WithSummary("Ottieni la classifica corrente");

// ─────────────────────────────────────────────────────────────────────────────
// 6️⃣ GPT Realtime — Endpoint per interazione vocale
// ─────────────────────────────────────────────────────────────────────────────
// Endpoint REST che fornisce le informazioni di connessione per il Realtime API.
// Il client usa queste info per aprire una sessione WebSocket audio/testo
// direttamente verso Azure OpenAI (gpt-4o-realtime-preview).
// Per una sessione completa, il client si connette via WebSocket all'URL fornito.

app.UseWebSockets();

var realtimeDeployment = app.Configuration["AZURE_OPENAI_REALTIME_DEPLOYMENT"] ?? "gpt-4o-realtime-preview";

app.MapGet("/realtime/session", () =>
{
    try
    {
        var host = new Uri(endpoint!).Host;
        return Results.Ok(new
        {
            deployment = realtimeDeployment,
            status = "ready",
            instructions = "GPT Realtime è configurato. " +
                          "Connettiti via WebSocket per sessioni audio/testo in tempo reale. " +
                          "Usa GetRealtimeClient().StartConversationSessionAsync() per avviare una sessione.",
            websocketUrl = $"wss://{host}/openai/realtime?api-version=2025-04-01-preview&deployment={realtimeDeployment}",
            gameInstructions = "🎩 Sei il Game Master del Gioco dell'Oca! Parla in italiano con entusiasmo.",
            codeExample = """
            #pragma warning disable OPENAI002
            var realtimeClient = azureClient.GetRealtimeClient();
            var session = await realtimeClient.StartConversationSessionAsync("gpt-4o-realtime-preview");
            """
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"GPT Realtime non configurato. Assicurati di avere un deployment '{realtimeDeployment}'. Errore: {ex.Message}",
            statusCode: 503);
    }
})
.WithName("GetRealtimeSession")
.WithSummary("Verifica e ottieni le info per la connessione GPT Realtime WebSocket");

// ─── Startup Info ─────────────────────────────────────────────────────────────

app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    Console.WriteLine("\n🎲 ═══════════════════════════════════════════════════════════");
    Console.WriteLine("🎩  AI GIOCO DELL'OCA - Microsoft Agent Framework Demo");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    foreach (var addr in addresses)
    {
        Console.WriteLine($"🎮  GIOCA:       {addr}");
        Console.WriteLine($"🛠️   DevUI:       {addr}/devui");
        Console.WriteLine($"🤖  Responses:   {addr}/openai/v1/responses");
        Console.WriteLine($"💬  Chats:       {addr}/openai/v1/conversations");
        Console.WriteLine($"🏆  Scoreboard:  {addr}/game/scoreboard");
        Console.WriteLine($"🎤  Realtime:    {addr}/realtime/session");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("📊  Aspire Dashboard: http://localhost:18888  (se attiva)");
    Console.WriteLine("═══════════════════════════════════════════════════════════\n");
});

app.Run();
