using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// Registrazione di tutti gli agenti e del workflow Handoff 🔄
/// </summary>
public static class WorkflowRegistration
{
    /// <summary>
    /// Registra tutti gli agenti del Gioco dell'Oca e configura il workflow handoff
    /// </summary>
    public static void AddGooseGameAgents(
        this WebApplicationBuilder builder,
        IChatClient chatClient,
        AzureOpenAIClient azureClient,
        string deploymentName)
    {
        // Registrazione agenti
        var gameMaster = GameMasterAgentRegistration.Register(builder, chatClient, azureClient, deploymentName);
        var dogAgent = DogAgentRegistration.Register(builder, chatClient);
        var jokeAgent = JokeAgentRegistration.Register(builder, chatClient);
        var catAgent = CatAgentRegistration.Register(builder, chatClient);
        var cocktailAgent = CocktailAgentRegistration.Register(builder, chatClient);
        var pokemonAgent = PokemonAgentRegistration.Register(builder, chatClient);
        var bonusAgent = BonusAgentRegistration.Register(builder, chatClient);

        // ─────────────────────────────────────────────────────────────────────
        // 1️⃣ Workflow Handoff 🔄
        // ─────────────────────────────────────────────────────────────────────

        builder.AddWorkflow("goose-game", (sp, key) =>
        {
            var gm = sp.GetRequiredKeyedService<AIAgent>(gameMaster.Name);
            var dog = sp.GetRequiredKeyedService<AIAgent>(dogAgent.Name);
            var joke = sp.GetRequiredKeyedService<AIAgent>(jokeAgent.Name);
            var cat = sp.GetRequiredKeyedService<AIAgent>(catAgent.Name);
            var cocktail = sp.GetRequiredKeyedService<AIAgent>(cocktailAgent.Name);
            var pokemon = sp.GetRequiredKeyedService<AIAgent>(pokemonAgent.Name);
            var bonus = sp.GetRequiredKeyedService<AIAgent>(bonusAgent.Name);

            return AgentWorkflowBuilder
                .CreateHandoffBuilderWith(gm)
                .WithHandoffs(gm, [dog, joke, cat, cocktail, pokemon, bonus])
                .WithHandoffs([dog, joke, cat, cocktail, pokemon, bonus], gm)
                .Build();
        }).AddAsAIAgent();
    }
}
