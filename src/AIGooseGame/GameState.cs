using System.Collections.Concurrent;

namespace AIGooseGame;

/// <summary>
/// Tipo di casella del tabellone 🎲
/// </summary>
public enum SquareType
{
    Start,
    Dog,
    Joke,
    Cat,
    Cocktail,
    Pokemon,
    Quiz,
    Prison,
    Finish
}

/// <summary>
/// Definizione di una casella del tabellone
/// </summary>
public record BoardSquareDefinition(int Position, SquareType Type, string Emoji, string Label);

/// <summary>
/// Stato di un singolo giocatore nel Gioco dell'Oca 🎲
/// </summary>
public record PlayerState(
    string Name,
    int Position = 0,
    int TurnsPlayed = 0,
    bool HasFinished = false,
    bool IsHuman = false,
    int TurnsToSkip = 0
);

/// <summary>
/// Gestione multi-giocatore con tabellone dinamico e prigione 🏆
/// </summary>
public class GameState
{
    private static readonly Random _random = Random.Shared;
    private readonly ConcurrentDictionary<string, PlayerState> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _playerOrder = [];
    private int _currentPlayerIndex;
    private List<BoardSquareDefinition> _board = [];
    private bool _gameInitialized;

    public int BoardSize => _board.Count > 0 ? _board.Count - 1 : 20;
    public bool IsGameInitialized => _gameInitialized;
    public IReadOnlyList<BoardSquareDefinition> Board => _board;

    /// <summary>
    /// Giocatore con sfida pendente (FASE 1 completata, attende risposta umana)
    /// </summary>
    public string? PendingChallengePlayer { get; private set; }

    public void SetPendingChallenge(string playerName) => PendingChallengePlayer = playerName;
    public void ClearPendingChallenge() => PendingChallengePlayer = null;

    private static readonly Dictionary<SquareType, string> SquareEmojis = new()
    {
        [SquareType.Start] = "🏁",
        [SquareType.Dog] = "🐶",
        [SquareType.Joke] = "😂",
        [SquareType.Cat] = "🐱",
        [SquareType.Cocktail] = "🍹",
        [SquareType.Pokemon] = "🎮",
        [SquareType.Quiz] = "📚",
        [SquareType.Prison] = "🔒",
        [SquareType.Finish] = "🏆",
    };

    /// <summary>
    /// Inizializza il tabellone dinamico e resetta lo stato del gioco
    /// </summary>
    public void InitializeBoard(int size = 20)
    {
        _board = GenerateBoard(size);
        _gameInitialized = true;
        _currentPlayerIndex = 0;
        _players.Clear();
        _playerOrder.Clear();
    }

    private static List<BoardSquareDefinition> GenerateBoard(int size)
    {
        var board = new List<BoardSquareDefinition>
        {
            new(0, SquareType.Start, "🏁", "START")
        };

        var challengeTypes = new[]
        {
            SquareType.Dog,
            SquareType.Joke,
            SquareType.Cat,
            SquareType.Cocktail,
            SquareType.Pokemon,
            SquareType.Quiz,
        };

        for (int i = 1; i < size; i++)
        {
            SquareType type;
            if (_random.Next(100) < 18)
            {
                type = SquareType.Prison;
            }
            else
            {
                type = challengeTypes[_random.Next(challengeTypes.Length)];
            }
            var emoji = SquareEmojis[type];
            board.Add(new(i, type, emoji, i.ToString()));
        }

        board.Add(new(size, SquareType.Finish, "🏆", "FINE"));
        return board;
    }

    /// <summary>
    /// Aggiunge un giocatore alla partita o lo resetta se già presente
    /// </summary>
    public PlayerState JoinGame(string name, bool isHuman = false)
    {
        var player = new PlayerState(name, IsHuman: isHuman);
        _players[name] = player;
        if (!_playerOrder.Contains(name, StringComparer.OrdinalIgnoreCase))
            _playerOrder.Add(name);
        return player;
    }

    /// <summary>
    /// Ottiene il giocatore corrente (di turno)
    /// </summary>
    public PlayerState? GetCurrentPlayer()
    {
        if (_playerOrder.Count == 0) return null;
        var name = _playerOrder[_currentPlayerIndex % _playerOrder.Count];
        return GetPlayer(name);
    }

    /// <summary>
    /// Ottiene il nome del giocatore corrente
    /// </summary>
    public string? GetCurrentPlayerName()
    {
        if (_playerOrder.Count == 0) return null;
        return _playerOrder[_currentPlayerIndex % _playerOrder.Count];
    }

    /// <summary>
    /// Passa al giocatore successivo, saltando chi è in prigione
    /// </summary>
    public (PlayerState? NextPlayer, List<string> SkippedPlayers) AdvanceToNextPlayer()
    {
        if (_playerOrder.Count == 0) return (null, []);

        // Auto-clear sfida pendente quando il turno avanza
        PendingChallengePlayer = null;

        var skipped = new List<string>();

        for (int i = 0; i < _playerOrder.Count; i++)
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _playerOrder.Count;
            var player = GetCurrentPlayer()!;

            if (player.TurnsToSkip > 0)
            {
                skipped.Add(player.Name);
                _players[player.Name] = player with { TurnsToSkip = player.TurnsToSkip - 1 };
                continue;
            }

            return (player, skipped);
        }

        // Tutti in prigione — tocca comunque al prossimo
        return (GetCurrentPlayer(), skipped);
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
    /// Applica la prigione: il giocatore perderà N turni
    /// </summary>
    public PlayerState ApplyPrison(string name, int turnsToSkip)
    {
        if (!_players.TryGetValue(name, out var player))
            throw new InvalidOperationException($"Giocatore '{name}' non trovato.");

        var updated = player with { TurnsToSkip = turnsToSkip };
        _players[name] = updated;
        return updated;
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
    /// Restituisce il tipo di casella per una posizione data (enum)
    /// </summary>
    public SquareType GetSquareType(int position)
    {
        if (position < 0 || position >= _board.Count)
            return SquareType.Finish;
        return _board[position].Type;
    }

    /// <summary>
    /// Restituisce il tipo di casella come stringa
    /// </summary>
    public string GetSquareTypeString(int position) =>
        GetSquareType(position).ToString().ToLowerInvariant();

    /// <summary>
    /// Restituisce la classifica corrente di tutti i giocatori
    /// </summary>
    public IReadOnlyList<PlayerState> GetScoreboard() =>
        _players.Values
            .OrderByDescending(p => p.HasFinished)
            .ThenByDescending(p => p.Position)
            .ThenBy(p => p.TurnsPlayed)
            .ToList();

    /// <summary>
    /// Restituisce l'elenco dei giocatori nell'ordine di turno
    /// </summary>
    public IReadOnlyList<string> GetPlayerOrder() => _playerOrder;

    /// <summary>
    /// Restituisce il tabellone come stringa leggibile
    /// </summary>
    public string GetBoardString() =>
        string.Join("→", _board.Select(s => $"[{s.Position}]{s.Emoji}"));
}
