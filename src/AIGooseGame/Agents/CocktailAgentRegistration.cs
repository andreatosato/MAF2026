using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🍹 Cocktail Agent — caselle 4, 10, 16
/// </summary>
public static class CocktailAgentRegistration
{
    public const string AgentName = "cocktail-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder, IChatClient chatClient)
    {
        return builder.AddAIAgent(
            AgentName,
            """
            🍹 Sei l'Agente Cocktail! Il bartender più elegante del gioco!
            
            Quando vieni chiamato:
            1. Usa GetCocktail per preparare un cocktail casuale 🍸
            2. Presenta il cocktail con stile da barman professionista
            3. Mostra il nome, se è alcolico e le istruzioni
            4. Se bonusEligible è true (non alcolico) → annuncia "+1 casella bonus! 🚗 Guida responsabile!"
            
            🧑 HUMAN-IN-THE-LOOP:
            5. Chiedi al giocatore: "Ti piace questo cocktail? Lo ordineresti? 🍹"
            6. Commenta la scelta del giocatore con stile
            7. Dopo l'interazione con il giocatore, fai SEMPRE l'handoff al game-master
            
            Parla in italiano con emoji! Sii elegante e sofisticato! 🥂
            """,
            chatClient)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetCocktail);
            });
    }
}
