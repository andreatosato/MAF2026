using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

namespace AIGooseGame.Agents;

/// <summary>
/// Registrazione di tutti gli agenti e del workflow Handoff 🔄
/// Nuovo flusso: Game Master → Challenge/Prison → Score → Game Master
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
        var challengeAgent = ChallengeAgentRegistration.Register(builder);
        var prisonAgent = PrisonAgentRegistration.Register(builder);
        var scoreAgent = ScoreAgentRegistration.Register(builder);

        // ─────────────────────────────────────────────────────────────────────
        // Workflow Handoff 🔄
        // Game Master → Challenge Agent (prove) o Prison Agent (prigione)
        // Challenge Agent → Score Agent (aggiorna punteggio)
        // Prison Agent → Score Agent (aggiorna punteggio)
        // Score Agent → Game Master (prossimo turno)
        // ─────────────────────────────────────────────────────────────────────

        builder.AddWorkflow("goose-game", (sp, key) =>
        {
            var gm = sp.GetRequiredKeyedService<AIAgent>(gameMaster.Name);
            var challenge = sp.GetRequiredKeyedService<AIAgent>(challengeAgent.Name);
            var prison = sp.GetRequiredKeyedService<AIAgent>(prisonAgent.Name);
            var score = sp.GetRequiredKeyedService<AIAgent>(scoreAgent.Name);

            var workflow = AgentWorkflowBuilder
                .CreateHandoffBuilderWith(gm)
                // Game Master → Challenge Agent, Prison Agent
                .WithHandoffs(gm, [challenge, prison])
                // Challenge Agent → Score Agent
                .WithHandoffs(challenge, [score])
                // Prison Agent → Score Agent
                .WithHandoffs(prison, [score])
                // Score Agent → Game Master
                .WithHandoffs(score, [gm])
                .Build();

            typeof(Workflow).GetProperty(nameof(Workflow.Name))!.SetValue(workflow, key);

            return workflow;
        }).AddAsAIAgent();
    }
}
