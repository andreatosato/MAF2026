using AIGooseGame;
using AIGooseGame.Agents;
using AIGooseGame.Components;
using AIGooseGame.Plugins;
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

// Bridge: Aspire registra IChatClient come servizio default (non-keyed).
// Gli agenti MAF con chatClientServiceKey: "chat" lo cercano come keyed service.
builder.Services.AddKeyedSingleton<IChatClient>("chat", (sp, _) => sp.GetRequiredService<IChatClient>());

builder.Services.AddAntiforgery();

// Registra GameState e i plugin come servizi DI
builder.Services.AddSingleton<GameState>();
builder.Services.AddHttpClient<PublicApiPlugin>();
builder.Services.AddHttpClient<MicrosoftLearnPlugin>();

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

app.MapPost("/game/initialize", ([FromServices] GameState gameState, int? size) =>
{
    gameState.InitializeBoard(size ?? 20);
    return Results.Ok(new
    {
        message = "🎲 Tabellone generato!",
        boardSize = gameState.BoardSize,
        board = gameState.Board.Select(s => new
        {
            position = s.Position,
            type = s.Type.ToString().ToLowerInvariant(),
            emoji = s.Emoji,
            label = s.Label
        }),
        boardString = gameState.GetBoardString()
    });
})
.WithName("InitializeGame")
.WithSummary("Inizializza il tabellone dinamico");

app.MapPost("/game/join/{playerName}", (string playerName, [FromServices] GameState gameState, bool? isHuman) =>
{
    var player = gameState.JoinGame(playerName, isHuman ?? false);
    var type = player.IsHuman ? "🧑 Giocatore Umano" : "🤖 Giocatore AI";
    return Results.Ok(new
    {
        message = $"🎉 Benvenuto {player.Name}! ({type}) Sei pronto a giocare al Gioco dell'Oca!",
        player,
        board = gameState.IsGameInitialized ? gameState.GetBoardString() : "Tabellone non ancora generato"
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
            squareType = gameState.GetSquareTypeString(p.Position),
            turnsToSkip = p.TurnsToSkip,
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
        : Results.Ok(new { player.Name, player.Position, player.TurnsPlayed, player.HasFinished, player.TurnsToSkip });
})
.WithName("GetPlayer")
.WithSummary("Ottieni lo stato di un giocatore");

app.MapGet("/game/board", ([FromServices] GameState gameState) =>
{
    if (!gameState.IsGameInitialized)
        return Results.Ok(new { initialized = false, board = Array.Empty<object>() });

    return Results.Ok(new
    {
        initialized = true,
        boardSize = gameState.BoardSize,
        board = gameState.Board.Select(s => new
        {
            position = s.Position,
            type = s.Type.ToString().ToLowerInvariant(),
            emoji = s.Emoji,
            label = s.Label
        })
    });
})
.WithName("GetBoard")
.WithSummary("Ottieni il tabellone corrente");

app.MapGet("/game/current-player", ([FromServices] GameState gameState) =>
{
    var current = gameState.GetCurrentPlayer();
    return Results.Ok(new
    {
        currentPlayer = current?.Name,
        allPlayers = gameState.GetPlayerOrder()
    });
})
.WithName("GetCurrentPlayer")
.WithSummary("Ottieni il giocatore di turno");

app.MapPost("/game/apply-bonus", ([FromServices] GameState gameState, [FromQuery] string playerName, [FromQuery] int bonus) =>
{
    try
    {
        var updated = gameState.ApplyBonus(playerName, bonus);
        return Results.Ok(new
        {
            playerName = updated.Name,
            bonus,
            newPosition = updated.Position,
            hasFinished = updated.HasFinished
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ApplyBonus")
.WithSummary("Applica bonus/malus direttamente al giocatore");

app.MapPost("/game/advance-turn", ([FromServices] GameState gameState) =>
{
    var (next, skipped) = gameState.AdvanceToNextPlayer();
    return Results.Ok(new
    {
        nextPlayer = next?.Name,
        nextPosition = next?.Position ?? 0,
        nextIsHuman = next?.IsHuman ?? false,
        skippedPlayers = skipped
    });
})
.WithName("AdvanceTurn")
.WithSummary("Avanza al giocatore successivo");

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
