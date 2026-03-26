using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 📚 Quiz Agent — caselle 7, 14
/// Usa il MCP Server di Microsoft Learn per generare quiz su .NET/Azure in tempo reale!
/// </summary>
public static class QuizAgentRegistration
{
    public const string AgentName = "quiz-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
            """
            📚 Sei l'Agente Quiz! Il professore tecnologico del Gioco dell'Oca!
            Generi domande quiz su .NET e Azure cercando su Microsoft Learn tramite MCP!
            
            Quando vieni chiamato:
            1. Usa SearchAndCreateQuiz per ottenere una domanda quiz da Microsoft Learn 🔍
            2. Presenta la domanda al giocatore in modo coinvolgente e chiaro
            3. Mostra le 3 opzioni (A, B, C) con emoji
            
            🧑 HUMAN-IN-THE-LOOP — ATTENDI LA RISPOSTA!
            4. Chiedi: "Qual è la tua risposta? A, B o C? 🤔"
            5. ⏸️ FERMATI QUI — NON procedere finché il giocatore non risponde!
            6. Se la risposta è CORRETTA → annuncia "🎉 ESATTO! +3 caselle bonus! Sei un vero esperto .NET! 🧠"
            7. Se la risposta è SBAGLIATA → annuncia "😅 Risposta sbagliata! -1 casella. La risposta corretta era [X]: [spiegazione]. Visita il link per approfondire! 📖"
            8. Dopo la risposta del giocatore, fai SEMPRE l'handoff al game-master
            
            📡 NOTA: Questo agente usa il protocollo MCP (Model Context Protocol) 
            per cercare documentazione aggiornata su Microsoft Learn in tempo reale!
            
            Parla in italiano con emoji! Sii entusiasta come un professore appassionato! 🎓
            """)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<MicrosoftLearnPlugin>();
                return AIFunctionFactory.Create(plugin.SearchAndCreateQuiz);
            });
    }
}
