using AIGooseGame.Plugins;
using Azure.AI.OpenAI;
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
        WebApplicationBuilder builder,
        IChatClient chatClient,
        AzureOpenAIClient azureClient,
        string deploymentName)
    {
        var gameMaster = builder.AddAIAgent(
            AgentName,
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
            
            Se hai bisogno di verificare una regola complessa, puoi chiamare lo strumento
            arbitro-agent (Agent-as-a-Tool) per una risposta autorevole.
            
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

        // ─────────────────────────────────────────────────────────────────────
        // 4️⃣ Agent-as-a-Tool — Arbitro come Function Tool del Game Master
        // ─────────────────────────────────────────────────────────────────────
        // L'Agente Arbitro viene esposto come AIFunction tramite .AsAIFunction().
        // Il Game Master può invocarlo come tool per verificare regole complesse.

        var arbitroAgent = azureClient.GetChatClient(deploymentName).AsIChatClient()
            .AsAIAgent(
                name: "arbitro-agent",
                instructions: """
                ⚖️ Sei l'Arbitro del Gioco dell'Oca! Il tuo ruolo è verificare le regole.
                
                Quando vieni chiamato, rispondi in modo preciso e autorevole:
                - Verifica se un bonus/malus è corretto
                - Conferma le regole della casella
                - Risolvi eventuali dispute
                
                Regole del tabellone:
                - 🐶 (1,7,13,19): labrador/retriever → +2
                - 😂 (2,8,14): utente ride → +1
                - 🐱 (3,9,15): sleep/hours → turno extra
                - 🍹 (4,10,16): analcolico → +1
                - 🎮 (5,11,17): fire → -2, water → +1
                - 🎲 (6,12,18): accetta sfida → +3, rifiuta → -1
                - 🏆 (20): vittoria!
                
                Rispondi in italiano, in modo chiaro e conciso. Sei imparziale e preciso!
                """,
                description: "Agente Arbitro che verifica le regole del gioco e risolve dispute");

        // Il Game Master usa l'Arbitro come tool (Agent-as-a-Tool pattern!)
        gameMaster.WithAITool(sp => arbitroAgent.AsAIFunction());

        return gameMaster;
    }
}
