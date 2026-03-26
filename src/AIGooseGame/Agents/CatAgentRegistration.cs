using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🐱 Cat Agent — caselle 3, 9, 15
/// </summary>
public static class CatAgentRegistration
{
    public const string AgentName = "cat-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
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
            """)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetCatFact);
            });
    }
}
