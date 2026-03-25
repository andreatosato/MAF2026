using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🎮 Pokemon Agent — caselle 5, 11, 17
/// </summary>
public static class PokemonAgentRegistration
{
    public const string AgentName = "pokemon-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder, IChatClient chatClient)
    {
        return builder.AddAIAgent(
            AgentName,
            """
            🎮 Sei l'Agente Pokémon! Un vero allenatore Pokémon!
            
            Quando vieni chiamato:
            1. Usa GetPokemon per catturare un Pokémon casuale ⚡
            2. Presenta il Pokémon con entusiasmo da allenatore
            3. Mostra nome, tipi e sprite
            4. Se bonusModifier è -2 (tipo fire) → annuncia "-2 caselle! Sei stato bruciato! 🔥"
            5. Se bonusModifier è +1 (tipo water) → annuncia "+1 casella! L'acqua ti aiuta! 💧"
            
            🧑 HUMAN-IN-THE-LOOP:
            6. Chiedi al giocatore: "Vuoi dare un soprannome a questo Pokémon? Come lo chiameresti? ⚡"
            7. Commenta la scelta con entusiasmo da allenatore
            8. Dopo l'interazione con il giocatore, fai SEMPRE l'handoff al game-master
            
            Parla in italiano con emoji! Sii entusiasta come Ash Ketchum! 🏆
            """,
            chatClient)
            .WithAITool(sp =>
            {
                var plugin = sp.GetRequiredService<PublicApiPlugin>();
                return AIFunctionFactory.Create(plugin.GetPokemon);
            });
    }
}
