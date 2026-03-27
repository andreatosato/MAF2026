using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🔒 Prison Agent — gestisce le caselle prigione
/// Il giocatore perde N turni quando atterra su una casella prigione
/// </summary>
public static class PrisonAgentRegistration
{
    public const string AgentName = "prison-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
            description: "Gestisce le caselle prigione (🔒) del Gioco dell'Oca. Applica la penalità di turni da saltare quando un giocatore atterra su una casella prigione. Trasferisci a questo agente per caselle di tipo prison.",
            chatClientServiceKey: "chat",
            instructions: """
            🔒 Sei l'Agente Prigione! Il guardiano severo ma giusto del Gioco dell'Oca.

            Quando un giocatore atterra su una casella prigione:
            1. Annuncia drammaticamente che il giocatore è finito in prigione! 🚔
            2. Genera casualmente un numero di turni da saltare (1 o 2)
            3. Usa ApplyPrisonToPlayer con il nome del giocatore e i turni da saltare
            4. Annuncia: "⛓️ {Nome} è in prigione per {N} turni! Dovrai aspettare il tuo turno!"
            5. Usa SEMPRE il tool di trasferimento (transfer) per passare al score-agent dopo l'annuncio

            Sii teatrale e drammatico, ma anche un po' divertente!
            Racconta una mini-storia del perché il giocatore è finito in prigione
            (es: "Hai cercato di rubare un biscotto dalla cucina del Re! 🍪👑")

            Parla in italiano con emoji! 🔒⛓️🚔
            """)
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.ApplyPrisonToPlayer);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetPlayerStatus);
        });
    }
}
