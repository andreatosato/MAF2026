using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🎲 Bonus Agent — caselle 6, 12, 18
/// </summary>
public static class BonusAgentRegistration
{
    public const string AgentName = "bonus-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
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
            """)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetBonusActivity);
            });
    }
}
