using AIGooseGame;
using AIGooseGame.Agents;
using AIGooseGame.Components;
using AIGooseGame.Plugins;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

// ─────────────────────────────────────────────────────────────────────────────
// Builder & Configuration
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// ASPIRE Service Defaults — OpenTelemetry, Health Checks, Resilience
// ⚠️ Va registrato PRIMA di AddAzureOpenAIClient e BuildServiceProvider
//    affinché il client Azure OpenAI risolto dal tempSp partecipi alle tracce.
// ─────────────────────────────────────────────────────────────────────────────
builder.AddServiceDefaults();

// Azure OpenAI Chat Client — configurato da Aspire tramite connection string "chat"
builder.AddAzureOpenAIClient("chat");

// Registra IChatClient come servizio derivato
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var azureClient = sp.GetRequiredService<AzureOpenAIClient>();
    return azureClient.GetChatClient("chat").AsIChatClient();
});

// L'estensione ora risolve da DI internamente
builder.AddGooseGameAgents("chat");

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

app.MapPost("/game/join/{playerName}", (string playerName, GameState gameState, bool? isHuman) =>
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
