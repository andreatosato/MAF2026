# Architecture — AI Gioco dell'Oca

```
┌────────────────────────────────────┐
│         🎲 Game UI (Blazor)         │
│    Components/Pages + DevUI       │
└─────────────┬──────────────────────┘
              │ HTTP / WebSocket
              ▼
┌────────────────────────────────────┐
│     🎩 Game Master Agent           │
│   (Orchestratore Handoff)          │
│   Tools: RollDice, GetBoardInfo,   │
│   JoinGame, InitializeGame, ...    │
└───────┬────────────────┬───────────┘
        │ Handoff         │ Handoff
        ▼                 ▼
┌──────────────────┐ ┌──────────────────┐
│ 🎯 Challenge Agent│ │ 🔒 Prison Agent   │
│  (Prove caselle)  │ │  (Fermo N turni)  │
│                   │ │                   │
│  Tools (ex-agenti │ │  Tools:           │
│  ora come tool):  │ │  ApplyPrison      │
│  🐶 GetRandomDog  │ └────────┬─────────┘
│  😂 GetJoke       │          │
│  🐱 GetCatFact    │          │
│  🍹 GetCocktail   │          │
│  🎮 GetPokemon    │          │
│  📚 SearchLearn   │          │
│  ApplyBonus       │          │
└────────┬─────────┘          │
         │                     │
         └──────────┬──────────┘
                    ▼
         ┌──────────────────┐
         │ 📊 Score Agent    │
         │ (Punteggio/Turni) │
         │                   │
         │ Tools:            │
         │ GetPlayerStatus   │
         │ AdvanceToNext     │
         └────────┬─────────┘
                  │ Handoff
                  ▼
           🎩 Game Master
           (Prossimo turno)

─── Plugin condivisi ──────────────────────────

┌────────────────────────────────────────────────┐
│           PublicApiPlugin (HTTP)                │
│  dog.ceo │ catfact │ cocktaildb │ pokeapi      │
│  + GameState (dado, turni, tabellone, bonus)   │
└────────────────────────────────────────────────┘

┌──────────────────────┐
│ MicrosoftLearnPlugin │
│ (Search API HTTP)    │
│ learn.microsoft.com  │
└──────────────────────┘

─── Infrastruttura ────────────────────────────

┌────────────────────────────────────┐
│     Azure AI Foundry (LLM)         │
│           gpt-4o-mini              │
└────────────────────────────────────┘

┌────────────────────────────────────┐
│   Azure Cosmos DB (Persistenza)    │
│  Emulatore locale │ Azure in prod  │
└────────────────────────────────────┘

┌────────────────────────────────────┐
│   .NET Aspire (Orchestrazione)     │
│  AppHost │ ServiceDefaults         │
└────────────────────────────────────┘
```

## Technology Stack

**Frontend:** Blazor Server interattivo (Game.razor, ResponsesDemo.razor) + DevUI per sviluppo
→ `AIGooseGame/Components/Pages/`

**Backend:** .NET 10 + Microsoft Agent Framework
→ `AIGooseGame/AIGooseGame.csproj`

**Infrastructure:** Azure AI Foundry (LLM) + Azure Cosmos DB (persistenza) + .NET Aspire (orchestrazione)
→ `AIGooseGame.AppHost/AppHost.cs`

**Protocollo:** Handoff Workflow per comunicazione tra agenti

## Componenti

### Game Master Agent 🎩
Orchestratore principale. Gestisce il flusso del gioco, lancia il dado, annuncia i risultati e fa handoff al Challenge Agent (caselle con prove) o al Prison Agent (caselle prigione). Usa i tool di `PublicApiPlugin` per dado, tabellone, registrazione giocatori e controllo flusso di gioco.

### Challenge Agent 🎯
Gestisce tutte le prove sulle caselle del tabellone. Gli ex-agenti specializzati (Dog, Joke, Cat, Cocktail, Pokemon, Quiz) sono ora **tool** di questo agente. In base al tipo di casella chiama il tool appropriato e gestisce bonus/malus. Dopo la prova fa handoff allo Score Agent.

### Prison Agent 🔒
Gestisce le caselle prigione. Applica la penalità di turni da saltare (1-2 turni) al giocatore che atterra su una casella 🔒. Dopo l'annuncio fa handoff allo Score Agent.

### Score Agent 📊
Aggiorna il punteggio del giocatore, verifica la posizione, gestisce il passaggio al giocatore successivo (saltando chi è in prigione) e fa handoff al Game Master per il prossimo turno.

### PublicApiPlugin
Plugin condiviso che gestisce le chiamate HTTP alle API esterne (dog.ceo, catfact, cocktaildb, pokeapi) e la logica di gioco (dado, tabellone dinamico, registrazione giocatori, prigione, bonus/malus, turni). Il tabellone è generato **casualmente** a runtime: ogni partita ha caselle diverse.

### MicrosoftLearnPlugin
Plugin che usa la Search API pubblica di Microsoft Learn per cercare documentazione in tempo reale. La generazione dei quiz è delegata all'agente (IChatClient via MAF), non al plugin.

### Aspire AppHost
Orchestrazione dell'applicazione: AI Foundry con deployment modelli, Cosmos DB con emulatore locale.

## Data Flow

1. **Input Giocatore**: L'utente scrive "lancio" nella UI o tramite DevUI
2. **Game Master**: Lancia il dado, determina la casella, fa handoff all'agente corretto
3. **Agente Specializzato**: Esegue la logica della casella (API esterna, quiz, bonus)
4. **Human-in-the-Loop**: L'agente interagisce con il giocatore, poi fa handoff al Game Master
5. **Persistenza**: La conversazione viene salvata su Cosmos DB

## Configurazione

Aspire gestisce tutte le connessioni tramite injection di variabili d'ambiente:

- **AI Foundry**: endpoint e deployment (chat) via connection string
- **Cosmos DB**: emulatore locale con `RunAsPreviewEmulator()`, Azure in produzione
- **ServiceDefaults**: OpenTelemetry, health checks, resilience
