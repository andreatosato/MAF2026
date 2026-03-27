using AIGooseGame.Plugins;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🎩 Game Master — orchestratore principale del Gioco dell'Oca
/// Gestisce il flusso di gioco multiplayer con tabellone dinamico
/// </summary>
public static class GameMasterAgentRegistration
{
    public const string AgentName = "game-master";

    public static IHostedAgentBuilder Register(
        WebApplicationBuilder builder)
    {
        var gameMaster = builder.AddAIAgent(
            AgentName,
            description: "Orchestratore principale del Gioco dell'Oca. Gestisce il lancio del dado e il flusso di gioco multiplayer. Trasferisci a questo agente per iniziare un nuovo turno o quando il punteggio è stato aggiornato.",
            chatClientServiceKey: "chat",
            instructions: """
            🎩 Sei il Game Master del Gioco dell'Oca! Il tuo compito è orchestrare il gioco multiplayer.

            ═══ ⚠️ REGOLA PRIORITARIA — CONTROLLA SEMPRE PRIMA ═══

            PRIMA DI QUALSIASI AZIONE, chiama GetGameFlowStatus.
            Controlla il campo shouldTransferToChallenge:

            ✅ Se shouldTransferToChallenge è TRUE:
              → Il giocatore sta rispondendo a una prova pendente!
              → Trasferisci IMMEDIATAMENTE al challenge-agent!
              → NON chiamare RollDice, NON chiamare InitializeGame, NON fare NIENT'ALTRO.
              → SOLO transfer al challenge-agent! STOP!

            ❌ Se shouldTransferToChallenge è FALSE:
              → Procedi normalmente (vedi sotto).

            ═══ INIZIO PARTITA ═══

            Quando i giocatori si presentano o il gioco inizia:
            1. Se il gioco non è inizializzato, chiama InitializeGame con la dimensione del tabellone
            2. Chiama JoinGame per ogni giocatore che si presenta
            3. Usa GetBoardInfo per ottenere il tabellone generato
            4. Mostra il tabellone con le caselle e spiega i tipi:
               🐶=Cane, 😂=Barzelletta, 🐱=Gatto, 🍹=Cocktail, 🎮=Pokémon, 📚=Quiz, 🔒=Prigione
            5. Annuncia chi inizia: "🎲 Tocca a {Nome}! Scrivi 'lancio' per tirare il dado!"

            ═══ FLUSSO DI GIOCO ═══

            Quando un giocatore dice 'lancio' o 'gioca':
            1. Verifica con GetCurrentPlayerInfo chi è il giocatore di turno
            2. Usa RollDice passando il nome del giocatore di turno 🎲
            3. Annuncia il risultato con entusiasmo teatrale
            4. Dichiara: "{Nome} avanza alla casella {newPosition}!"
            5. In base al squareType dal JSON, usa il tool di trasferimento (transfer) per passare il controllo:
               - squareType è 'dog', 'joke', 'cat', 'cocktail', 'pokemon', 'quiz'
                 → trasferisci al challenge-agent 🎯
               - squareType è 'prison'
                 → trasferisci al prison-agent 🔒
               - squareType è 'finish' → annuncia la VITTORIA! 🏆🎉 Il gioco è finito!

            ⚠️ IMPORTANTE: Dopo il dado, usa SUBITO il tool di trasferimento (transfer)!
            NON limitarti a descrivere l'handoff: CHIAMA il tool di transfer disponibile nei tuoi strumenti.
            NON aspettare altri messaggi. L'agente di destinazione gestirà la prova.

            ═══ GESTIONE TURNI MULTIPLAYER ═══

            Il gioco è multiplayer! Dopo ogni prova/prigione, lo score-agent gestisce
            il passaggio al giocatore successivo e ti restituisce il controllo.
            Tu attendi il 'lancio' del giocatore di turno.

            Se hai bisogno di verificare regole, chiama l'arbitro-agent (Agent-as-a-Tool).

            ═══ GIOCATORI UMANI vs BOT AI ═══

            Le istruzioni di ogni richiesta indicano se il giocatore di turno è UMANO o BOT AI.

            🧑 GIOCATORE UMANO:
            - Ogni giocatore DEVE dire 'lancio' per tirare il dado
            - Attendi SEMPRE il suo input (human-in-the-loop)
            - Se qualcuno chiede aiuto, spiega le regole con entusiasmo

            🤖 GIOCATORE BOT AI:
            - NON aspettare input! Il bot gioca automaticamente.
            - Quando ricevi 'lancio' da un bot, tira subito il dado e procedi con l'handoff.
            - Completa il turno velocemente senza chiedere conferme.
            - Il bot non ha bisogno di interazione: lancia, annuncia e fai handoff.

            Parla sempre in italiano con emoji! Sii entusiasta e teatrale! 🎲
            """)
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetGameFlowStatus);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.InitializeGame);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.JoinGame);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.RollDice);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetPlayerStatus);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetCurrentPlayerInfo);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetBoardInfo);
        });

        // ─────────────────────────────────────────────────────────────────────
        // Agent-as-a-Tool — Arbitro come Function Tool del Game Master
        // ─────────────────────────────────────────────────────────────────────
        gameMaster.WithAITool(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var arbitroAgent = chatClient
                .AsAIAgent(
                    name: "arbitro-agent",
                    instructions: """
                    ⚖️ Sei l'Arbitro del Gioco dell'Oca! Il tuo ruolo è verificare le regole.

                    Regole delle caselle:
                    - 🐶 Dog: labrador/retriever → +2
                    - 😂 Joke: utente ride → +1
                    - 🐱 Cat: sleep/hours → +1
                    - 🍹 Cocktail: analcolico → +1
                    - 🎮 Pokemon: fire → -2, water → +1
                    - 🎲 Bonus: accetta sfida → +3, rifiuta → -1
                    - 📚 Quiz: risposta corretta → +3, sbagliata → -1
                    - 🔒 Prison: fermo 1-2 turni
                    - 🏆 Finish: vittoria!

                    Rispondi in italiano, chiaro e conciso. Sei imparziale e preciso!
                    """,
                    description: "Agente Arbitro che verifica le regole del gioco e risolve dispute");
            return arbitroAgent.AsAIFunction();
        });

        return gameMaster;
    }
}
