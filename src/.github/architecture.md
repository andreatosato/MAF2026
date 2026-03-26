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
│                                    │
│   ┌──────────────────────────┐     │
│   │ ⚖️ Arbitro (Agent-as-Tool) │     │
│   └──────────────────────────┘     │
└─────────────┬──────────────────────┘
              │ Handoff Workflow
    ┌─────────┼─────────┬─────────┬─────────┬─────────┬─────────┐
    ▼         ▼         ▼         ▼         ▼         ▼         ▼
┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐
│🐶 Dog │ │😂 Joke│ │🐱 Cat │ │🍹 Cock│ │🎮 Poke│ │🎲Bonus│ │📚 Quiz│
│ Agent │ │ Agent │ │ Agent │ │ Agent │ │ Agent │ │ Agent │ │ Agent │
└───┬───┘ └───────┘ └───┬───┘ └───┬───┘ └───┬───┘ └───────┘ └───┬───┘
    │                   │         │         │                     │
    ▼                   ▼         ▼         ▼                     ▼
┌────────────────────────────────────────────────┐  ┌──────────────────────┐
│           PublicApiPlugin (HTTP)                │  │ MicrosoftLearnPlugin │
│  dog.ceo │ catfact │ cocktaildb │ pokeapi      │  │ (MCP via Responses)  │
└────────────────────────────────────────────────┘  └──────────┬───────────┘
                                                               │
                                                               ▼
                                                  ┌──────────────────────┐
                                                  │  Microsoft Learn MCP │
                                                  │  learn.microsoft.com │
                                                  │      /api/mcp        │
                                                  └──────────────────────┘

┌────────────────────────────────────┐
│     Azure AI Foundry (LLM)         │
│  gpt-4o-mini │ gpt-4o-realtime    │
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
Orchestratore principale. Gestisce il flusso del gioco, lancia il dado, annuncia i risultati e delega agli agenti specializzati in base alla casella. Include l'Arbitro come Agent-as-a-Tool per verifiche regolamentari.

### Dog Agent 🐶 (caselle 1, 13, 19)
Chiama l'API dog.ceo per mostrare un cane casuale. Se la razza è labrador/retriever, assegna +2 caselle bonus.

### Joke Agent 😂 (caselle 2, 8)
Racconta barzellette in italiano. Se il giocatore ride, assegna +1 casella bonus.

### Cat Agent 🐱 (caselle 3, 9, 15)
Chiama l'API catfact per curiosità sui gatti. Basandosi sulle ore di sonno, può concedere un turno extra.

### Cocktail Agent 🍹 (caselle 4, 10, 16)
Chiama l'API cocktaildb per suggerire un cocktail casuale con ricetta.

### Pokemon Agent 🎮 (caselle 5, 11, 17)
Chiama l'API pokeapi per mostrare un Pokémon casuale con le sue statistiche.

### Bonus Agent 🎲 (caselle 6, 12, 18)
Lancia un dado bonus per avanzare di ulteriori caselle.

### Quiz Agent 📚 (caselle 7, 14) — MCP Microsoft Learn
Usa il protocollo MCP (Model Context Protocol) tramite le OpenAI Responses API per cercare su Microsoft Learn e generare quiz interattivi su .NET/Azure. +3 caselle se il giocatore risponde correttamente, -1 se sbaglia. Integra il `MicrosoftLearnPlugin` che si collega al server MCP pubblico `https://learn.microsoft.com/api/mcp`.

### Arbitro Agent ⚖️ (Agent-as-a-Tool)
Verifica le regole del gioco. Invocato dal Game Master come function tool per risolvere dispute.

### PublicApiPlugin
Plugin condiviso che gestisce le chiamate HTTP alle API esterne (dog.ceo, catfact, cocktaildb, pokeapi).

### MicrosoftLearnPlugin
Plugin che usa le OpenAI Responses API con MCP Tool per cercare documentazione su Microsoft Learn in tempo reale. Genera quiz a risposta multipla su argomenti .NET/Azure con fallback statico in caso di errore.

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

- **AI Foundry**: endpoint e deployment (chat, realtime) via connection string
- **Cosmos DB**: emulatore locale con `RunAsPreviewEmulator()`, Azure in produzione
- **ServiceDefaults**: OpenTelemetry, health checks, resilience
