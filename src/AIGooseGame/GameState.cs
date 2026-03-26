using System.Collections.Concurrent;

namespace AIGooseGame;

/// <summary>
/// Stato di un singolo giocatore nel Gioco dell'Oca 🎲
/// </summary>
public record PlayerState(
    string Name,
    int Position = 0,
    int TurnsPlayed = 0,
    bool HasFinished = false,
    bool IsHuman = false
);

/// <summary>
/// Gestione multi-giocatore con ConcurrentDictionary per la sicurezza dei thread 🏆
/// </summary>
public class GameState
{
    public const int BoardSize = 20;

    /// <summary>
    /// Mappa le caselle del tabellone: indice = posizione (1-based), valore = tipo di casella
    /// </summary>
    public static readonly string[] BoardSquares =
    [
        "start",     // 0 - START
        "dog",       // 1 🐶
        "joke",      // 2 😂
        "cat",       // 3 🐱
        "cocktail",  // 4 🍹
        "pokemon",   // 5 🎮
        "bonus",     // 6 🎲
        "quiz",      // 7 📚 MCP Microsoft Learn
        "joke",      // 8 😂
        "cat",       // 9 🐱
        "cocktail",  // 10 🍹
        "pokemon",   // 11 🎮
        "bonus",     // 12 🎲
        "dog",       // 13 🐶
        "quiz",      // 14 📚 MCP Microsoft Learn
        "cat",       // 15 🐱
        "cocktail",  // 16 🍹
        "pokemon",   // 17 🎮
        "bonus",     // 18 🎲
        "dog",       // 19 🐶
        "finish",    // 20 🏆 FINE
    ];

    private readonly ConcurrentDictionary<string, PlayerState> _players = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Aggiunge un giocatore alla partita o lo resetta se già presente
    /// </summary>
    public PlayerState JoinGame(string name, bool isHuman = false)
    {
        var player = new PlayerState(name, IsHuman: isHuman);
        _players[name] = player;
        return player;
    }

    /// <summary>
    /// Ottiene lo stato corrente di un giocatore
    /// </summary>
    public PlayerState? GetPlayer(string name) =>
        _players.TryGetValue(name, out var player) ? player : null;

    /// <summary>
    /// Muove il giocatore in base al lancio del dado e restituisce la nuova posizione
    /// </summary>
    public (PlayerState Player, bool Finished) MovePlayer(string name, int diceRoll)
    {
        if (!_players.TryGetValue(name, out var player))
            throw new InvalidOperationException($"Giocatore '{name}' non trovato. Usa /game/join/{name} prima.");

        var newPosition = Math.Min(player.Position + diceRoll, BoardSize);
        var finished = newPosition >= BoardSize;

        var updated = player with
        {
            Position = newPosition,
            TurnsPlayed = player.TurnsPlayed + 1,
            HasFinished = finished
        };

        _players[name] = updated;
        return (updated, finished);
    }

    /// <summary>
    /// Applica un bonus/malus alla posizione del giocatore
    /// </summary>
    public PlayerState ApplyBonus(string name, int bonus)
    {
        if (!_players.TryGetValue(name, out var player))
            throw new InvalidOperationException($"Giocatore '{name}' non trovato.");

        var newPosition = Math.Clamp(player.Position + bonus, 0, BoardSize);
        var finished = newPosition >= BoardSize;

        var updated = player with
        {
            Position = newPosition,
            HasFinished = finished
        };

        _players[name] = updated;
        return updated;
    }

    /// <summary>
    /// Restituisce il tipo di casella per una posizione data
    /// </summary>
    public string GetSquareType(int position)
    {
        if (position < 0 || position >= BoardSquares.Length)
            return "finish";
        return BoardSquares[position];
    }

    /// <summary>
    /// Restituisce la classifica corrente di tutti i giocatori
    /// </summary>
    public IReadOnlyList<PlayerState> GetScoreboard() =>
        _players.Values
            .OrderByDescending(p => p.HasFinished)
            .ThenByDescending(p => p.Position)
            .ThenBy(p => p.TurnsPlayed)
            .ToList();
}
