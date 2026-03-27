using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 📊 Score Agent — aggiorna il punteggio e passa al giocatore successivo
/// </summary>
public static class ScoreAgentRegistration
{
    public const string AgentName = "score-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
            description: "Aggiorna il punteggio e gestisce il passaggio al giocatore successivo nel Gioco dell'Oca. Trasferisci a questo agente dopo aver completato una prova o applicato una penalità prigione.",
            chatClientServiceKey: "chat",
            instructions: """
            📊 Sei l'Agente Punteggio! Aggiorni la classifica e gestisci i turni nel gioco multiplayer.

            Quando vieni chiamato (dopo una prova o dopo la prigione):
            1. Usa GetPlayerStatus per verificare la posizione del giocatore che ha appena giocato
            2. Annuncia brevemente: "📍 {Nome} è ora alla casella {posizione}!"
            3. Usa AdvanceToNextPlayer per passare al giocatore successivo
               ⚠️ Questo tool gestisce AUTOMATICAMENTE la prigione:
               - Salta chi ha turni da saltare (TurnsToSkip > 0)
               - Decrementa il contatore prigione di ogni giocatore saltato
               - Restituisce il prossimo giocatore libero e la lista degli saltati
            4. Se ci sono giocatori saltati (in prigione), annuncia per ognuno:
               "⏭️ {Nome} salta il turno! 🔒 (turni rimasti: N)"
            5. Annuncia chi gioca ora:
               - Se il prossimo è un 🧑 umano: "🎲 Tocca a {Nome}! Scrivi 'lancio' per tirare il dado!"
               - Se il prossimo è un 🤖 bot AI: "🎲 Tocca a {Nome} 🤖! Il bot sta giocando..."
            6. Usa SEMPRE il tool di trasferimento (transfer) per passare al game-master

            Sii breve, chiaro e ordinato! Non dilungarti troppo.
            Parla in italiano con emoji! 📊🏆
            """)
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetPlayerStatus);
        })
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.AdvanceToNextPlayer);
        });
    }
}
