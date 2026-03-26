using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🐶 Dog Agent — caselle 1, 7, 13, 19
/// </summary>
public static class DogAgentRegistration
{
    public const string AgentName = "dog-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
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
            """)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetRandomDog);
            });
    }
}
