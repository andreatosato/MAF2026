using AIGooseGame;
using AIGooseGame.Agents;
using AIGooseGame.Components;
using AIGooseGame.Plugins;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

// ─────────────────────────────────────────────────────────────────────────────
// Builder & Configuration
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// ASPIRE Service Defaults — OpenTelemetry, Health Checks, Resilience
// ⚠️ Va registrato PRIMA della registrazione AI client
// ─────────────────────────────────────────────────────────────────────────────
builder.AddServiceDefaults();

// Microsoft Foundry Chat Client — configurato da Aspire tramite Azure AI Inference
// Registra IChatClient direttamente da DI (pattern Aspire.Azure.AI.Inference)
builder.AddAzureChatCompletionsClient("chat")
    .AddChatClient("chat");

// AzureOpenAIClient — necessario per MicrosoftLearnPlugin (Responses API + MCP)
// Registrato manualmente dall'endpoint Foundry (il raw SDK Azure.AI.OpenAI è già nel progetto)
builder.Services.AddSingleton(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("chat") ?? "";
    var endpoint = connectionString.Split(';')
        .Select(p => p.Split('=', 2))
        .Where(p => p.Length == 2 && p[0].Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
        .Select(p => p[1].TrimEnd('/'))
        .FirstOrDefault() ?? throw new InvalidOperationException("Missing 'Endpoint' in 'chat' connection string.");
    return new AzureOpenAIClient(new Uri(endpoint), new Azure.Identity.DefaultAzureCredential());
});

builder.Services.AddAntiforgery();

// Registra GameState e i plugin come servizi DI
builder.Services.AddSingleton<GameState>();
builder.Services.AddHttpClient<PublicApiPlugin>();
builder.Services.AddSingleton<MicrosoftLearnPlugin>(sp =>
{
    var azureClient = sp.GetRequiredService<AzureOpenAIClient>();
    return new MicrosoftLearnPlugin(azureClient, "chat");
});

// L'estensione ora risolve tutto da DI internamente
builder.AddGooseGameAgents();

// Razor Components (Blazor Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ─────────────────────────────────────────────────────────────────────────────
// OpenAI Responses + Conversations + DevUI
// ─────────────────────────────────────────────────────────────────────────────

builder.AddOpenAIResponses();
builder.AddOpenAIConversations();
builder.AddDevUI();

// ─────────────────────────────────────────────────────────────────────────────
// 5️⃣ Salvataggio Conversazione su Cosmos DB
// ─────────────────────────────────────────────────────────────────────────────
// Cosmos DB è gestito da Aspire: emulatore in locale, Azure in produzione.
// La connection string "GooseGameDB" viene iniettata tramite WithReference nell'AppHost.

var cosmosConnectionString = builder.Configuration.GetConnectionString("GooseGameDB");
if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
{
    var cosmosDatabase = "GooseGameDB";
    var cosmosContainer = builder.Configuration["COSMOS_CONTAINER_NAME"] ?? "ChatHistory";

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

app.MapDefaultEndpoints();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

// ─── REST Endpoints Gioco dell'Oca ────────────────────────────────────────────

app.MapPost("/game/join/{playerName}", (string playerName, [FromServices] GameState gameState, bool? isHuman) =>
{
    var player = gameState.JoinGame(playerName, isHuman ?? false);
    var type = player.IsHuman ? "🧑 Giocatore Umano" : "🤖 Giocatore AI";
    return Results.Ok(new
    {
        message = $"🎉 Benvenuto {player.Name}! ({type}) Sei pronto a giocare al Gioco dell'Oca!",
        player,
        board = $"Tabellone: START → [1]🐶→[2]😂→[3]🐱→[4]🍹→[5]🎮→[6]🎲→[7]📚→[8]😂→...→[14]📚→...→[20]🏆 FINE"
    });
})
.WithName("JoinGame")
.WithSummary("Entra nella partita del Gioco dell'Oca");

app.MapGet("/game/scoreboard", ([FromServices] GameState gameState) =>
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

app.MapGet("/game/player/{playerName}", (string playerName, [FromServices] GameState gameState) =>
{
    var player = gameState.GetPlayer(playerName);
    return player is null
        ? Results.NotFound(new { error = $"Giocatore '{playerName}' non trovato" })
        : Results.Ok(new { player.Name, player.Position, player.TurnsPlayed, player.HasFinished });
})
.WithName("GetPlayer")
.WithSummary("Ottieni lo stato di un giocatore");

// ─── Startup Info ─────────────────────────────────────────────────────────────

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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
        Console.WriteLine($"🤖  Responses:   {addr}/v1/responses");
        Console.WriteLine($"📡  Demo API:    {addr}/responses-demo (Blazor)");
        Console.WriteLine($"💬  Chats:       {addr}/v1/conversations");
        Console.WriteLine($"🏆  Scoreboard:  {addr}/game/scoreboard");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("📊  Aspire Dashboard: avvia AIGooseGame.AppHost per orchestrazione completa");
    Console.WriteLine("═══════════════════════════════════════════════════════════\n");
});

app.Run();
