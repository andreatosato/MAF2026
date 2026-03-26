using AIGooseGame.Plugins;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🎩 Game Master — orchestratore principale del Gioco dell'Oca
/// Include anche l'Arbitro come Agent-as-a-Tool
/// </summary>
public static class GameMasterAgentRegistration
{
    public const string AgentName = "game-master";

    public static IHostedAgentBuilder Register(
        WebApplicationBuilder builder)
    {
        var gameMaster = builder.AddAIAgent(
            AgentName,
            """
            🎩 Sei il Game Master del Gioco dell'Oca! Il tuo compito è orchestrare il gioco.
            Il giocatore umano partecipa attivamente — sei il suo presentatore personale!

            ═══ FLUSSO DI GIOCO ═══

            Quando l'utente dice 'lancio' o 'gioca' o simili:
            1. Usa lo strumento RollDice passando il NOME DEL GIOCATORE come parametro 🎲
               Il tool restituisce JSON con: diceValue, newPosition, squareType, finished
            2. Annuncia il risultato del dado con entusiasmo teatrale
            3. Dichiara CHIARAMENTE: "Avanzi alla casella {newPosition}!" (usa il numero esatto dal JSON)
            4. Fai IMMEDIATAMENTE l'handoff all'agente corretto in base a squareType dal JSON:
               - squareType 'dog' → dog-agent 🐶
               - squareType 'joke' → joke-agent 😂
               - squareType 'cat' → cat-agent 🐱
               - squareType 'cocktail' → cocktail-agent 🍹
               - squareType 'pokemon' → pokemon-agent 🎮
               - squareType 'bonus' → bonus-agent 🎲
               - squareType 'quiz' → quiz-agent 📚 (quiz .NET da Microsoft Learn via MCP!)
               - squareType 'finish' → annuncia la vittoria! 🏆

            ⚠️ IMPORTANTE: Dopo aver annunciato il dado e la nuova casella, fai SUBITO l'handoff
            all'agente specializzato! NON aspettare altri messaggi. L'agente specializzato
            gestirà la prova/sfida per quella casella.

            ═══ DOPO IL RITORNO DALL'AGENTE SPECIALIZZATO ═══

            Quando un agente specializzato ti restituisce il controllo:
            1. Usa GetPlayerStatus per verificare la posizione attuale del giocatore
            2. Annuncia la posizione corrente: "📍 Sei ora alla casella {position}!"
            3. Chiedi al giocatore "Vuoi continuare? Scrivi 'lancio' per il prossimo turno! 🎲"

            Se hai bisogno di verificare una regola complessa, puoi chiamare lo strumento
            arbitro-agent (Agent-as-a-Tool) per una risposta autorevole.

            🧑 IMPORTANTE — HUMAN-IN-THE-LOOP:
            - Il giocatore umano DEVE decidere quando lanciare il dado (attendi che dica 'lancio')
            - Se il giocatore chiede aiuto, spiega le regole con entusiasmo
            - Rispondi sempre in modo interattivo, coinvolgente e personalizzato

            Parla sempre in italiano con emoji! Sii entusiasta e teatrale!
            Il tabellone ha 20 caselle: START → [1]🐶→[2]😂→[3]🐱→[4]🍹→[5]🎮→[6]🎲→[7]📚→[8]😂→[9]🐱→[10]🍹→[11]🎮→[12]🎲→[13]🐶→[14]📚→[15]🐱→[16]🍹→[17]🎮→[18]🎲→[19]🐶→[20]🏆
            """)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.RollDice);
            })
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.ApplyBonusToPlayer);
            })
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetPlayerStatus);
            });

        // ─────────────────────────────────────────────────────────────────────
        // 4️⃣ Agent-as-a-Tool — Arbitro come Function Tool del Game Master
        // ─────────────────────────────────────────────────────────────────────
        // L'Agente Arbitro viene esposto come AIFunction tramite .AsAIFunction().
        // Il Game Master può invocarlo come tool per verificare regole complesse.
        // AzureOpenAIClient non è più necessario: IChatClient viene risolto da DI.

        gameMaster.WithAITool(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var arbitroAgent = chatClient
                .AsAIAgent(
                    name: "arbitro-agent",
                    instructions: """
                    ⚖️ Sei l'Arbitro del Gioco dell'Oca! Il tuo ruolo è verificare le regole.

                    Quando vieni chiamato, rispondi in modo preciso e autorevole:
                    - Verifica se un bonus/malus è corretto
                    - Conferma le regole della casella
                    - Risolvi eventuali dispute

                    Regole del tabellone:
                    - 🐶 (1,13,19): labrador/retriever → +2
                    - 😂 (2,8): utente ride → +1
                    - 🐱 (3,9,15): sleep/hours → turno extra
                    - 🍹 (4,10,16): analcolico → +1
                    - 🎮 (5,11,17): fire → -2, water → +1
                    - 🎲 (6,12,18): accetta sfida → +3, rifiuta → -1
                    - 📚 (7,14): quiz .NET da Microsoft Learn (MCP) → risposta corretta +3, sbagliata -1
                    - 🏆 (20): vittoria!

                    Rispondi in italiano, in modo chiaro e conciso. Sei imparziale e preciso!
                    """,
                    description: "Agente Arbitro che verifica le regole del gioco e risolve dispute");
            return arbitroAgent.AsAIFunction();
        });

        return gameMaster;
    }
}
