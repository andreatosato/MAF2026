using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace AIGooseGame.Plugins;

/// <summary>
/// Plugin con tutte le chiamate alle API pubbliche (NO auth!) 🌐
/// e il lancio del dado per il Gioco dell'Oca 🎲
/// Include tool per tabellone dinamico, multiplayer e prigione
/// </summary>
public class PublicApiPlugin(HttpClient httpClient, GameState gameState)
{
    private static readonly Random _random = Random.Shared;

    // ─── 🎮 Inizializzazione gioco ─────────────────────────────────────────────

    [Description("Verifica lo stato del flusso di gioco: giocatore corrente e se c'è una sfida pendente. " +
                 "CHIAMA SEMPRE QUESTO TOOL COME PRIMA COSA prima di qualsiasi altra azione! " +
                 "Se shouldTransferToChallenge è TRUE, trasferisci IMMEDIATAMENTE al challenge-agent!")]
    public string GetGameFlowStatus()
    {
        var current = gameState.GetCurrentPlayer();
        var pending = gameState.PendingChallengePlayer;
        var hasPending = pending != null;

        // Auto-clear pending challenge per bot (non-umani) per prevenire loop infiniti.
        // I bot devono completare le prove in un solo passaggio (FASE UNICA).
        if (hasPending && current != null
            && string.Equals(pending, current.Name, StringComparison.OrdinalIgnoreCase)
            && !current.IsHuman)
        {
            gameState.ClearPendingChallenge();
            pending = null;
            hasPending = false;
        }

        var shouldTransfer = hasPending
            && current != null
            && string.Equals(pending, current.Name, StringComparison.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(new
        {
            currentPlayer = current?.Name,
            currentPlayerIsHuman = current?.IsHuman ?? false,
            pendingChallengePlayer = pending,
            hasPendingChallenge = hasPending,
            shouldTransferToChallenge = shouldTransfer
        });
    }

    [Description("Segna che c'è una sfida pendente per un giocatore UMANO (FASE 1 completata). " +
                 "NON usare per bot AI! I bot completano la prova in un unico passaggio.")]
    public string SetPendingChallenge(
        [Description("Nome del giocatore UMANO con sfida pendente")] string playerName)
    {
        var player = gameState.GetPlayer(playerName);
        if (player is null)
            return JsonSerializer.Serialize(new { error = "Giocatore non trovato", playerName, set = false });
        if (!player.IsHuman)
            return JsonSerializer.Serialize(new { error = "Solo per giocatori UMANI! I bot completano la prova in un solo passaggio.", playerName, set = false });
        gameState.SetPendingChallenge(playerName);
        return JsonSerializer.Serialize(new { pendingChallenge = playerName, set = true });
    }

    [Description("Inizializza il gioco con un tabellone dinamico. Crea caselle casuali con prove, prigione e bonus. " +
                 "Chiamalo all'inizio della partita PRIMA di registrare i giocatori.")]
    public string InitializeGame(
        [Description("Dimensione del tabellone (default 20, min 10, max 40)")] int boardSize = 20)
    {
        boardSize = Math.Clamp(boardSize, 10, 40);
        gameState.InitializeBoard(boardSize);
        return JsonSerializer.Serialize(new
        {
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
    }

    // ─── 🗺️ Info tabellone ─────────────────────────────────────────────────────

    [Description("Restituisce le informazioni sul tabellone corrente con tutte le caselle.")]
    public string GetBoardInfo()
    {
        if (!gameState.IsGameInitialized)
            return JsonSerializer.Serialize(new { error = "Il gioco non è stato inizializzato. Chiama InitializeGame prima." });

        return JsonSerializer.Serialize(new
        {
            boardSize = gameState.BoardSize,
            board = gameState.Board.Select(s => new
            {
                position = s.Position,
                type = s.Type.ToString().ToLowerInvariant(),
                emoji = s.Emoji
            }),
            boardString = gameState.GetBoardString()
        });
    }

    // ─── 👥 Giocatore corrente ──────────────────────────────────────────────────

    [Description("Restituisce il giocatore di turno e la lista di tutti i giocatori registrati.")]
    public string GetCurrentPlayerInfo()
    {
        var current = gameState.GetCurrentPlayer();
        return JsonSerializer.Serialize(new
        {
            currentPlayer = current is not null ? new
            {
                name = current.Name,
                position = current.Position,
                turnsToSkip = current.TurnsToSkip
            } : null,
            allPlayers = gameState.GetPlayerOrder()
        });
    }

    // ─── ⏭️ Turno successivo ────────────────────────────────────────────────────

    [Description("Passa al giocatore successivo. Salta automaticamente chi è in prigione. " +
                 "Restituisce il prossimo giocatore e chi è stato saltato.")]
    public string AdvanceToNextPlayer()
    {
        var (next, skipped) = gameState.AdvanceToNextPlayer();
        return JsonSerializer.Serialize(new
        {
            nextPlayer = next is not null ? new
            {
                name = next.Name,
                position = next.Position,
                turnsPlayed = next.TurnsPlayed,
                turnsToSkip = next.TurnsToSkip,
                isHuman = next.IsHuman
            } : null,
            skippedPlayers = skipped
        });
    }

    // ─── 🔒 Prigione ────────────────────────────────────────────────────────────

    [Description("Applica la prigione al giocatore: il giocatore perderà N turni. " +
                 "Usa turnsToSkip tra 1 e 2.")]
    public string ApplyPrisonToPlayer(
        [Description("Nome del giocatore")] string playerName,
        [Description("Numero di turni da saltare (1-2)")] int turnsToSkip)
    {
        var player = gameState.GetPlayer(playerName);
        if (player is null)
            return JsonSerializer.Serialize(new { error = "Giocatore non trovato", playerName });

        turnsToSkip = Math.Clamp(turnsToSkip, 1, 2);
        var updated = gameState.ApplyPrison(playerName, turnsToSkip);
        return JsonSerializer.Serialize(new
        {
            playerName = updated.Name,
            turnsToSkip = updated.TurnsToSkip,
            position = updated.Position
        });
    }

    // ─── 🎮 Registrazione giocatore ────────────────────────────────────────────

    [Description("Registra un giocatore nel Gioco dell'Oca. Restituisce JSON con il nome e la posizione iniziale. " +
                 "Chiamalo quando il giocatore si presenta per la prima volta o se un altro tool restituisce 'Giocatore non trovato'.")]
    public string JoinGame(
        [Description("Nome del giocatore da registrare")] string playerName)
    {
        var player = gameState.JoinGame(playerName, isHuman: true);
        return JsonSerializer.Serialize(new
        {
            playerName = player.Name,
            position = player.Position,
            registered = true
        });
    }

    // ─── 🎲 Dado ───────────────────────────────────────────────────────────────

    [Description("Lancia il dado per un giocatore, muove la pedina sul tabellone e restituisce il risultato JSON con: diceValue, newPosition, squareType, finished, playerName.")]
    public string RollDice([Description("Nome del giocatore che lancia il dado")] string playerName)
    {
        var dice = _random.Next(1, 7);

        var player = gameState.GetPlayer(playerName);
        if (player is null)
        {
            gameState.JoinGame(playerName, isHuman: true);
        }

        var (updated, finished) = gameState.MovePlayer(playerName, dice);
        var squareType = gameState.GetSquareTypeString(updated.Position);

        return JsonSerializer.Serialize(new
        {
            diceValue = dice,
            newPosition = updated.Position,
            squareType,
            finished,
            playerName = updated.Name
        });
    }

    // ─── 📊 Stato giocatore ────────────────────────────────────────────────────

    [Description("Ottieni lo stato attuale del giocatore: posizione corrente, tipo di casella, turni giocati, prigione e se ha finito il gioco.")]
    public string GetPlayerStatus([Description("Nome del giocatore")] string playerName)
    {
        var player = gameState.GetPlayer(playerName);
        if (player is null)
        {
            return JsonSerializer.Serialize(new { error = "Giocatore non trovato", playerName });
        }

        return JsonSerializer.Serialize(new
        {
            playerName = player.Name,
            position = player.Position,
            squareType = gameState.GetSquareTypeString(player.Position),
            turnsPlayed = player.TurnsPlayed,
            hasFinished = player.HasFinished,
            turnsToSkip = player.TurnsToSkip
        });
    }

    // ─── 🎁 Bonus/Malus ────────────────────────────────────────────────────────

    [Description("Applica un bonus o malus alla posizione del giocatore sul tabellone. " +
                 "Bonus positivo = avanza, negativo = arretra. " +
                 "Restituisce JSON con posizione precedente, bonus applicato, nuova posizione e tipo casella. " +
                 "DEVI chiamare questo tool OGNI volta che un giocatore guadagna o perde caselle!")]
    public string ApplyBonusToPlayer(
        [Description("Nome del giocatore")] string playerName,
        [Description("Valore del bonus/malus (es: 2, -1, 3, -2)")] int bonus)
    {
        var player = gameState.GetPlayer(playerName);
        if (player is null)
        {
            gameState.JoinGame(playerName, isHuman: true);
            player = gameState.GetPlayer(playerName)!;
        }

        var previousPosition = player.Position;
        var updated = gameState.ApplyBonus(playerName, bonus);
        var squareType = gameState.GetSquareTypeString(updated.Position);

        return JsonSerializer.Serialize(new
        {
            playerName = updated.Name,
            previousPosition,
            bonus,
            newPosition = updated.Position,
            squareType,
            finished = updated.HasFinished
        });
    }

    // ─── 🐶 Dog CEO API ────────────────────────────────────────────────────────

    [Description("Ottieni un'immagine di un cane casuale. Restituisce JSON con breed e imageUrl. Se la razza contiene 'labrador' o 'retriever' il giocatore ottiene +2 caselle bonus!")]
    public async Task<string> GetRandomDog()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://dog.ceo/api/breeds/image/random");
        var imageUrl = response.GetProperty("message").GetString() ?? "";
        var parts = imageUrl.Split('/');
        var breed = parts.Length >= 6 ? parts[^2].Replace("-", " ") : "unknown";

        return JsonSerializer.Serialize(new
        {
            breed,
            imageUrl,
            bonusEligible = breed.Contains("labrador", StringComparison.OrdinalIgnoreCase)
                         || breed.Contains("retriever", StringComparison.OrdinalIgnoreCase)
        });
    }

    // ─── 😂 Official Joke API ──────────────────────────────────────────────────

    [Description("Ottieni una barzelletta casuale. Restituisce JSON con setup e punchline. Se l'utente ride, +1 casella bonus!")]
    public async Task<string> GetJoke()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://official-joke-api.appspot.com/random_joke");
        var setup = response.GetProperty("setup").GetString() ?? "";
        var punchline = response.GetProperty("punchline").GetString() ?? "";

        return JsonSerializer.Serialize(new { setup, punchline });
    }

    // ─── 🐱 Cat Facts API ──────────────────────────────────────────────────────

    [Description("Ottieni un fatto sui gatti. Se il fatto menziona 'sleep' o 'hours', il giocatore ottiene un turno extra!")]
    public async Task<string> GetCatFact()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://catfact.ninja/fact");
        var fact = response.GetProperty("fact").GetString() ?? "";

        return JsonSerializer.Serialize(new
        {
            fact,
            extraTurnEligible = fact.Contains("sleep", StringComparison.OrdinalIgnoreCase)
                             || fact.Contains("hours", StringComparison.OrdinalIgnoreCase)
        });
    }

    // ─── 🍹 CocktailDB API ─────────────────────────────────────────────────────

    [Description("Ottieni un cocktail casuale. Restituisce JSON con nome, se è alcolico, istruzioni e immagine. Se non alcolico → +1 casella bonus (guida responsabile!)")]
    public async Task<string> GetCocktail()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://www.thecocktaildb.com/api/json/v1/1/random.php");
        var drink = response.GetProperty("drinks")[0];

        var name = drink.GetProperty("strDrink").GetString() ?? "";
        var alcoholicStr = drink.GetProperty("strAlcoholic").GetString() ?? "";
        var isAlcoholic = !alcoholicStr.Equals("Non alcoholic", StringComparison.OrdinalIgnoreCase);
        var instructions = drink.GetProperty("strInstructions").GetString() ?? "";
        var image = drink.GetProperty("strDrinkThumb").GetString() ?? "";

        return JsonSerializer.Serialize(new
        {
            name,
            isAlcoholic,
            instructions = instructions.Length > 200 ? instructions[..200] + "..." : instructions,
            imageUrl = image,
            bonusEligible = !isAlcoholic
        });
    }

    // ─── 🎮 PokéAPI ────────────────────────────────────────────────────────────

    [Description("Cattura un Pokémon casuale (ID 1-151). Restituisce JSON con nome, tipi e sprite. Se tipo 'fire' → -2 caselle (bruciato! 🔥). Se tipo 'water' → +1 casella 💧")]
    public async Task<string> GetPokemon()
    {
        var id = _random.Next(1, 152);
        var response = await httpClient.GetFromJsonAsync<JsonElement>($"https://pokeapi.co/api/v2/pokemon/{id}");

        var name = response.GetProperty("name").GetString() ?? "";
        var types = response.GetProperty("types")
            .EnumerateArray()
            .Select(t => t.GetProperty("type").GetProperty("name").GetString() ?? "")
            .ToArray();
        var sprite = response.GetProperty("sprites").GetProperty("front_default").GetString() ?? "";

        var bonusModifier = 0;
        if (types.Contains("fire")) bonusModifier = -2;
        else if (types.Contains("water")) bonusModifier = 1;

        return JsonSerializer.Serialize(new
        {
            name,
            types,
            spriteUrl = sprite,
            bonusModifier
        });
    }

    }
