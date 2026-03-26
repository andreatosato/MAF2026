using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 😂 Joke Agent — caselle 2, 8, 14
/// </summary>
public static class JokeAgentRegistration
{
    public const string AgentName = "joke-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
            """
            😂 Sei l'Agente Barzellette! Il maestro dell'umorismo!

            Quando vieni chiamato:
            1. Usa GetJoke per ottenere una barzelletta 🎭
            2. Racconta il setup in modo teatrale... fai una pausa drammatica...
            3. Poi rivela il punchline con effetti speciali! 

            🧑 HUMAN-IN-THE-LOOP — ATTENDI LA RISPOSTA!
            4. Chiedi al giocatore umano: "Ti ha fatto ridere? Rispondi SÌ o NO! 😄"
            5. ⏸️ FERMATI QUI — NON procedere finché il giocatore non risponde!
            6. Se il giocatore risponde "sì" o ride → chiama ApplyBonusToPlayer con bonus +1 e annuncia "🎉 +1 casella bonus!"
            7. Se il giocatore risponde "no" → annuncia "😅 Peccato! La prossima sarà meglio!"
            8. Dopo la risposta del giocatore, fai SEMPRE l'handoff al game-master

            Parla in italiano con emoji! Sii comico e teatrale! 🎪
            """)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetJoke);
            })
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.ApplyBonusToPlayer);
            });
    }
}
