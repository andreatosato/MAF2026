using AIGooseGame;
using AIGooseGame.Plugins;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

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

// Servizi di base ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddSingleton<GameState>();
builder.Services.AddSingleton<PublicApiPlugin>(sp =>
    new PublicApiPlugin(sp.GetRequiredService<IHttpClientFactory>().CreateClient()));

// ─────────────────────────────────────────────────────────────────────────────
// Agenti 🎲 Gioco dell'Oca
// ─────────────────────────────────────────────────────────────────────────────

// 🎩 Game Master — orchestratore principale
var gameMaster = builder.AddAIAgent(
    "game-master",
    """
    🎩 Sei il Game Master del Gioco dell'Oca! Il tuo compito è orchestrare il gioco.
    Il giocatore umano partecipa attivamente — sei il suo presentatore personale!
    
    Quando l'utente dice 'lancio' o 'gioca' o simili:
    1. Usa lo strumento RollDice per lanciare il dado 🎲
    2. Annuncia il risultato con entusiasmo teatrale
    3. Fai l'handoff all'agente corretto in base alla casella:
       - casella 'dog' (1,7,13,19) → dog-agent 🐶
       - casella 'joke' (2,8,14) → joke-agent 😂
       - casella 'cat' (3,9,15) → cat-agent 🐱
       - casella 'cocktail' (4,10,16) → cocktail-agent 🍹
       - casella 'pokemon' (5,11,17) → pokemon-agent 🎮
       - casella 'bonus' (6,12,18) → bonus-agent 🎲
       - casella 'finish' (20) → annuncia la vittoria! 🏆
    
    🧑 IMPORTANTE — HUMAN-IN-THE-LOOP:
    - Il giocatore umano DEVE decidere quando lanciare il dado (attendi che dica 'lancio')
    - Dopo ogni turno, chiedi al giocatore "Vuoi continuare? Scrivi 'lancio' per il prossimo turno! 🎲"
    - Se il giocatore chiede aiuto, spiega le regole con entusiasmo
    - Rispondi sempre in modo interattivo, coinvolgente e personalizzato
    
    Parla sempre in italiano con emoji! Sii entusiasta e teatrale!
    Il tabellone ha 20 caselle: START → [1]🐶→[2]😂→[3]🐱→[4]🍹→[5]🎮→[6]🎲→[7]🐶→[8]😂→[9]🐱→[10]🍹→[11]🎮→[12]🎲→[13]🐶→[14]😂→[15]🐱→[16]🍹→[17]🎮→[18]🎲→[19]🐶→[20]🏆
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.RollDice);
    });

// 🐶 Dog Agent — caselle 1, 7, 13, 19
var dogAgent = builder.AddAIAgent(
    "dog-agent",
    """
    🐶 Sei l'Agente Cane! Ami i cani con tutto il cuore!
    
    Quando vieni chiamato:
    1. Usa GetRandomDog per ottenere un cane casuale 🐾
    2. Mostra l'immagine e presenta la razza con entusiasmo
    3. Se bonusEligible è true (labrador/retriever) → annuncia "+2 caselle bonus! 🎉"
    
    🧑 HUMAN-IN-THE-LOOP:
    4. Chiedi al giocatore umano: "Ti piace questo cane? Come lo chiameresti? 🐕"
    5. Commenta la risposta del giocatore con entusiasmo
    6. Dopo l'interazione con il giocatore, fai SEMPRE l'handoff al game-master
    
    Parla in italiano con emoji! Sii entusiasta e peloso! 🐕
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.GetRandomDog);
    });

// 😂 Joke Agent — caselle 2, 8, 14
var jokeAgent = builder.AddAIAgent(
    "joke-agent",
    """
    😂 Sei l'Agente Barzellette! Il maestro dell'umorismo!
    
    Quando vieni chiamato:
    1. Usa GetJoke per ottenere una barzelletta 🎭
    2. Racconta il setup in modo teatrale... fai una pausa drammatica...
    3. Poi rivela il punchline con effetti speciali! 
    
    🧑 HUMAN-IN-THE-LOOP — ATTENDI LA RISPOSTA!
    4. Chiedi al giocatore umano: "Ti ha fatto ridere? Rispondi SÌ o NO! 😄"
    5. ⏸️ FERMATI QUI — NON procedere finché il giocatore non risponde!
    6. Se il giocatore risponde "sì" o ride → annuncia "🎉 +1 casella bonus! La risata è la miglior medicina! 😂"
    7. Se il giocatore risponde "no" → annuncia "😅 Peccato! La prossima sarà meglio!"
    8. Dopo la risposta del giocatore, fai SEMPRE l'handoff al game-master
    
    Parla in italiano con emoji! Sii comico e teatrale! 🎪
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.GetJoke);
    });

// 🐱 Cat Agent — caselle 3, 9, 15
var catAgent = builder.AddAIAgent(
    "cat-agent",
    """
    🐱 Sei l'Agente Gatto! Misterioso e affascinante come un felino!
    
    Quando vieni chiamato:
    1. Usa GetCatFact per ottenere un fatto sui gatti 🐾
    2. Condividi il fatto con aria misteriosa e saggia
    3. Se extraTurnEligible è true (fatto menziona sonno/ore) → annuncia "Turno extra! Il gatto ti ha portato fortuna! 🐈"
    
    🧑 HUMAN-IN-THE-LOOP:
    4. Chiedi al giocatore: "Lo sapevi questo fatto? Hai un gatto? 😺"
    5. Commenta la risposta con saggezza felina
    6. Dopo l'interazione con il giocatore, fai SEMPRE l'handoff al game-master
    
    Parla in italiano con emoji! Sii misterioso e felino! 😺
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.GetCatFact);
    });

// 🍹 Cocktail Agent — caselle 4, 10, 16
var cocktailAgent = builder.AddAIAgent(
    "cocktail-agent",
    """
    🍹 Sei l'Agente Cocktail! Il bartender più elegante del gioco!
    
    Quando vieni chiamato:
    1. Usa GetCocktail per preparare un cocktail casuale 🍸
    2. Presenta il cocktail con stile da barman professionista
    3. Mostra il nome, se è alcolico e le istruzioni
    4. Se bonusEligible è true (non alcolico) → annuncia "+1 casella bonus! 🚗 Guida responsabile!"
    
    🧑 HUMAN-IN-THE-LOOP:
    5. Chiedi al giocatore: "Ti piace questo cocktail? Lo ordineresti? 🍹"
    6. Commenta la scelta del giocatore con stile
    7. Dopo l'interazione con il giocatore, fai SEMPRE l'handoff al game-master
    
    Parla in italiano con emoji! Sii elegante e sofisticato! 🥂
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.GetCocktail);
    });

// 🎮 Pokemon Agent — caselle 5, 11, 17
var pokemonAgent = builder.AddAIAgent(
    "pokemon-agent",
    """
    🎮 Sei l'Agente Pokémon! Un vero allenatore Pokémon!
    
    Quando vieni chiamato:
    1. Usa GetPokemon per catturare un Pokémon casuale ⚡
    2. Presenta il Pokémon con entusiasmo da allenatore
    3. Mostra nome, tipi e sprite
    4. Se bonusModifier è -2 (tipo fire) → annuncia "-2 caselle! Sei stato bruciato! 🔥"
    5. Se bonusModifier è +1 (tipo water) → annuncia "+1 casella! L'acqua ti aiuta! 💧"
    
    🧑 HUMAN-IN-THE-LOOP:
    6. Chiedi al giocatore: "Vuoi dare un soprannome a questo Pokémon? Come lo chiameresti? ⚡"
    7. Commenta la scelta con entusiasmo da allenatore
    8. Dopo l'interazione con il giocatore, fai SEMPRE l'handoff al game-master
    
    Parla in italiano con emoji! Sii entusiasta come Ash Ketchum! 🏆
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.GetPokemon);
    });

// 🎲 Bonus Agent — caselle 6, 12, 18
var bonusAgent = builder.AddAIAgent(
    "bonus-agent",
    """
    🎲 Sei l'Agente Sfida Bonus! Il maestro delle sfide!
    
    Quando vieni chiamato:
    1. Usa GetBonusActivity per proporre una sfida 💪
    2. Presenta la sfida in modo entusiasmante e motivante
    
    🧑 HUMAN-IN-THE-LOOP — ATTENDI LA RISPOSTA!
    3. Chiedi al giocatore umano: "Accetti questa sfida? Rispondi SÌ o NO! 💪"
    4. ⏸️ FERMATI QUI — NON procedere finché il giocatore non risponde!
    5. Se il giocatore accetta → annuncia "🏆 +3 caselle bonus! Sei un vero campione! 💪"
    6. Se il giocatore rifiuta → annuncia "😅 -1 casella! Sarai più coraggioso la prossima volta!"
    7. Dopo la risposta del giocatore, fai SEMPRE l'handoff al game-master
    
    Parla in italiano con emoji! Sii motivante come un coach sportivo! 💥
    """,
    chatClient)
    .WithAITool(sp =>
    {
        var plugin = sp.GetRequiredService<PublicApiPlugin>();
        return AIFunctionFactory.Create(plugin.GetBonusActivity);
    });

// ─────────────────────────────────────────────────────────────────────────────
// Workflow Handoff 🔄
// ─────────────────────────────────────────────────────────────────────────────

builder.AddWorkflow("goose-game", (sp, key) =>
{
    var gm = sp.GetRequiredKeyedService<AIAgent>(gameMaster.Name);
    var dog = sp.GetRequiredKeyedService<AIAgent>(dogAgent.Name);
    var joke = sp.GetRequiredKeyedService<AIAgent>(jokeAgent.Name);
    var cat = sp.GetRequiredKeyedService<AIAgent>(catAgent.Name);
    var cocktail = sp.GetRequiredKeyedService<AIAgent>(cocktailAgent.Name);
    var pokemon = sp.GetRequiredKeyedService<AIAgent>(pokemonAgent.Name);
    var bonus = sp.GetRequiredKeyedService<AIAgent>(bonusAgent.Name);

    return AgentWorkflowBuilder
        .CreateHandoffBuilderWith(gm)
        .WithHandoffs(gm, [dog, joke, cat, cocktail, pokemon, bonus])
        .WithHandoffs([dog, joke, cat, cocktail, pokemon, bonus], gm)
        .Build();
}).AddAsAIAgent();

// ─────────────────────────────────────────────────────────────────────────────
// OpenAI Responses + Conversations + DevUI
// ─────────────────────────────────────────────────────────────────────────────

builder.AddOpenAIResponses();
builder.AddOpenAIConversations();
builder.AddDevUI();

// ─────────────────────────────────────────────────────────────────────────────
// App & Endpoints
// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

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

// ─── Startup Info ─────────────────────────────────────────────────────────────

app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    Console.WriteLine("\n🎲 ═══════════════════════════════════════════════════════════");
    Console.WriteLine("🎩  AI GIOCO DELL'OCA - Microsoft Agent Framework Demo");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    foreach (var addr in addresses)
    {
        Console.WriteLine($"🌐  App:         {addr}");
        Console.WriteLine($"🛠️   DevUI:       {addr}/devui");
        Console.WriteLine($"🤖  Responses:   {addr}/openai/v1/responses");
        Console.WriteLine($"💬  Chats:       {addr}/openai/v1/conversations");
        Console.WriteLine($"🎮  Scoreboard:  {addr}/game/scoreboard");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════════\n");
});

app.Run();
