using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

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
        this WebApplicationBuilder builder)
    {
        // Registrazione agenti
        var gameMaster = GameMasterAgentRegistration.Register(builder);
        var dogAgent = DogAgentRegistration.Register(builder);
        var jokeAgent = JokeAgentRegistration.Register(builder);
        var catAgent = CatAgentRegistration.Register(builder);
        var cocktailAgent = CocktailAgentRegistration.Register(builder);
        var pokemonAgent = PokemonAgentRegistration.Register(builder);
        var bonusAgent = BonusAgentRegistration.Register(builder);
        var quizAgent = QuizAgentRegistration.Register(builder);

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
            var quiz = sp.GetRequiredKeyedService<AIAgent>(quizAgent.Name);

            var workflow = AgentWorkflowBuilder
                .CreateHandoffBuilderWith(gm)
                .WithHandoffs(gm, [dog, joke, cat, cocktail, pokemon, bonus, quiz])
                .WithHandoffs([dog, joke, cat, cocktail, pokemon, bonus, quiz], gm)
                .Build();

            // HandoffsWorkflowBuilder.Build() non imposta il nome sul Workflow,
            // ma AddWorkflow richiede che workflow.Name corrisponda alla chiave.
            // Il setter è internal → reflection necessaria.
            typeof(Workflow).GetProperty(nameof(Workflow.Name))!.SetValue(workflow, key);

            return workflow;
        }).AddAsAIAgent();
    }
}
