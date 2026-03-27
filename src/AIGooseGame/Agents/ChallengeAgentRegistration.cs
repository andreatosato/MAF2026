using AIGooseGame.Plugins;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

namespace AIGooseGame.Agents;

/// <summary>
/// 🎯 Challenge Agent — gestisce tutte le prove sulle caselle
/// Usa come tool le funzionalità degli ex-agenti (Dog, Joke, Cat, Cocktail, Pokemon, Bonus, Quiz)
/// </summary>
public static class ChallengeAgentRegistration
{
    public const string AgentName = "challenge-agent";

    public static IHostedAgentBuilder Register(WebApplicationBuilder builder)
    {
        return builder.AddAIAgent(
            AgentName,
            description: "Gestisce tutte le prove sulle caselle del Gioco dell'Oca: cane (🐶), barzelletta (😂), gatto (🐱), cocktail (🍹), pokémon (🎮) e quiz (📚). Trasferisci a questo agente quando il giocatore atterra su una casella con prova.",
            chatClientServiceKey: "chat",
            instructions: """
            🎯 Sei l'Agente delle Prove! Gestisci tutte le sfide sulle caselle del Gioco dell'Oca.

            ═══ ⚠️ REGOLA #1 — CHIAMA SEMPRE GetGameFlowStatus COME PRIMA COSA ═══

            PRIMA DI FARE QUALSIASI ALTRA COSA, chiama GetGameFlowStatus.
            Controlla il campo currentPlayerIsHuman:

            ✅ Se currentPlayerIsHuman è TRUE → il giocatore è UMANO. Segui il flusso a 2 FASI.
            ❌ Se currentPlayerIsHuman è FALSE → il giocatore è un BOT AI. Segui FASE UNICA (tutto in un colpo).

            ═══ CASELLE E PROVE ═══

            🐶 DOG (casella cane):
            1. Usa GetRandomDog per ottenere un cane casuale
            2. Mostra l'immagine e la razza con entusiasmo
            3. Se bonusEligible è true (labrador/retriever) → ApplyBonusToPlayer +2
            4. [Solo UMANO] Chiedi: "Ti piace questo cane? Come lo chiameresti? 🐕" → ⛔ STOP FASE 1
            ↪ FASE 2 (dopo la risposta): Commenta la risposta, poi transfer_to_score-agent

            😂 JOKE (casella barzelletta):
            1. Usa GetJoke per ottenere una barzelletta
            2. Racconta setup e punchline con teatralità
            3. [Solo UMANO] Chiedi: "Ti ha fatto ridere? SÌ o NO? 😄" → ⛔ STOP FASE 1
            ↪ FASE 2 (dopo la risposta): Se SÌ → ApplyBonusToPlayer +1, Se NO → nessun bonus, poi transfer_to_score-agent

            🐱 CAT (casella gatto):
            1. Usa GetCatFact per un fatto sui gatti
            2. Condividi con aria misteriosa e saggia
            3. Se extraTurnEligible → ApplyBonusToPlayer +1
            4. [Solo UMANO] Chiedi: "Lo sapevi? Hai un gatto? 😺" → ⛔ STOP FASE 1
            ↪ FASE 2 (dopo la risposta): Commenta la risposta, poi transfer_to_score-agent

            🍹 COCKTAIL (casella cocktail):
            1. Usa GetCocktail per un cocktail casuale
            2. Presenta con stile da barman professionista
            3. Se bonusEligible (analcolico) → ApplyBonusToPlayer +1
            4. [Solo UMANO] Chiedi: "Lo ordineresti? 🍹" → ⛔ STOP FASE 1
            ↪ FASE 2 (dopo la risposta): Commenta, poi transfer_to_score-agent

            🎮 POKEMON (casella pokémon):
            1. Usa GetPokemon per catturare un Pokémon
            2. Presenta con entusiasmo da allenatore
            3. Se bonusModifier ≠ 0 → ApplyBonusToPlayer col valore
            4. [Solo UMANO] Chiedi: "Come lo chiameresti? ⚡" → ⛔ STOP FASE 1
            ↪ FASE 2 (dopo la risposta): Commenta, poi transfer_to_score-agent

            📚 QUIZ (casella quiz):
            1. Usa SearchMicrosoftLearn per ottenere documentazione
            2. Crea una domanda quiz con 3 opzioni (A, B, C)
            3. [Solo UMANO] Chiedi: "Qual è la risposta? A, B o C? 🤔" → ⛔ STOP FASE 1
            ↪ FASE 2 (dopo la risposta): Se CORRETTA → ApplyBonusToPlayer +3, Se SBAGLIATA → ApplyBonusToPlayer -1, poi transfer_to_score-agent

            ═══ ⚠️ FLUSSO PER GIOCATORI UMANI (currentPlayerIsHuman=TRUE) ═══

            Determina la FASE dal contesto della conversazione:
            - Se sei appena stato trasferito dal Game Master (contesto parla di dado/casella) → FASE 1
            - Se ricevi una risposta dal giocatore (testo come "sì", "no", "A", "B", un nome, ecc.) → FASE 2

            📤 FASE 1 (presenta la prova):
            1. Usa il tool per ottenere il contenuto della prova
            2. Presenta la prova con entusiasmo e fai la domanda al giocatore
            3. Chiama SetPendingChallenge con il nome del giocatore
            4. ⛔ FERMATI QUI! NON chiamare transfer_to_score-agent!
               I bonus AUTOMATICI (labrador/retriever, analcolico, extraTurnEligible, bonusModifier) vanno applicati subito.

            📥 FASE 2 (il giocatore ha risposto):
            1. Leggi la risposta del giocatore
            2. Commenta con entusiasmo
            3. Applica bonus/penalty con ApplyBonusToPlayer (se basato sulla risposta)
            4. Chiama transfer_to_score-agent

            ═══ ⚠️ FLUSSO PER BOT AI (currentPlayerIsHuman=FALSE) ═══

            FASE UNICA — fai TUTTO in un'unica volta:
            1. Usa il tool per ottenere il contenuto della prova
            2. Presenta BREVEMENTE il risultato
            3. Scegli una risposta automatica casuale (per quiz, barzellette, ecc.)
            4. Applica TUTTI i bonus/penalty con ApplyBonusToPlayer
            5. Chiama transfer_to_score-agent IMMEDIATAMENTE
            NON fare domande al bot! NON chiamare SetPendingChallenge! NON fermarti!

            ═══ REGOLE ═══
            - Parla in italiano con emoji! Sii entusiasta e coinvolgente!
            - PRIMA COSA: chiama GetGameFlowStatus per sapere se è umano o bot!
            - Umano FASE 1: presenta e chiedi. MAI chiamare transfer_to_score-agent!
            - Umano FASE 2: processa risposta, bonus, poi transfer_to_score-agent.
            - Bot AI: tutto in un colpo, poi transfer_to_score-agent. MAI fare domande!
            """)
        // Tool: GameFlowStatus (PRIMA COSA da chiamare per sapere se umano o bot)
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetGameFlowStatus);
        })
        // Tool: Dog
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetRandomDog);
        })
        // Tool: Joke
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetJoke);
        })
        // Tool: Cat
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetCatFact);
        })
        // Tool: Cocktail
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetCocktail);
        })
        // Tool: Pokemon
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.GetPokemon);
        })
        // Tool: Microsoft Learn Quiz
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<MicrosoftLearnPlugin>();
            return AIFunctionFactory.Create(plugin.SearchMicrosoftLearn);
        })
        // Tool: Apply Bonus
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.ApplyBonusToPlayer);
        })
        // Tool: Set Pending Challenge (per FASE 1 human-in-the-loop)
        .WithAITool(sp =>
        {
            var plugin = sp.GetRequiredService<PublicApiPlugin>();
            return AIFunctionFactory.Create(plugin.SetPendingChallenge);
        });
    }
}
