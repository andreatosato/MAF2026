# Copilot Instructions

Questo è un progetto demo "AI Gioco dell'Oca" basato su Microsoft Agent Framework (.NET 10) con Aspire.

## Documentazione di Riferimento

- [`architecture.md`](architecture.md) - Architettura completa del sistema, stack tecnologico e dettagli dei componenti
- [`agents/maf-dotnet.agent.md`](agents/maf-dotnet.agent.md) - Pattern di sviluppo agenti MAF e best practices

## Pattern Chiave

Quando crei o modifichi agenti, consulta le istruzioni dell'agente MAF per pattern e best practices.

**Struttura del progetto**:
- Progetto unico `AIGooseGame` con tutti gli agenti in-process
- Workflow Handoff per orchestrare gli agenti specializzati
- DevUI per test e sviluppo

**Agenti del Gioco dell'Oca** (nuovo workflow):
- `GameMasterAgentRegistration` - Orchestratore principale del gioco 🎩
- `ChallengeAgentRegistration` - Gestisce le prove sulle caselle (usa come tool: Dog, Joke, Cat, Cocktail, Pokemon, Quiz) 🎯
- `PrisonAgentRegistration` - Gestisce le caselle prigione (fermo N turni) 🔒
- `ScoreAgentRegistration` - Aggiorna punteggio e passa al giocatore successivo 📊
- `WorkflowRegistration` - Registrazione workflow handoff

**Workflow Handoff**:
- Game Master → Challenge Agent (caselle con prova) o Prison Agent (caselle prigione)
- Challenge Agent → Score Agent
- Prison Agent → Score Agent
- Score Agent → Game Master (prossimo turno)

**Comunicazione tra agenti**:
- Handoff workflow: Game Master → Challenge/Prison → Score → Game Master
- Agent-as-a-Tool: l'Arbitro è esposto come tool del Game Master
- Gli ex-agenti (Dog, Joke, Cat, ecc.) sono ora TOOL del Challenge Agent
- OpenAI Responses + Conversations per endpoint compatibili
- MCP (Model Context Protocol): il Challenge Agent usa SearchMicrosoftLearn per quiz

## Build Commands

Esegui con: `aspire start` o avvia il progetto `AIGooseGame.AppHost`

## Layout del Progetto

- `AIGooseGame.AppHost/` - Aspire host (AppHost.cs) con AI Foundry + CosmosDB
- `AIGooseGame/` - Progetto principale con agenti, plugin e game logic
- `AIGooseGame/Agents/` - Registrazione agenti del gioco
- `AIGooseGame/Plugins/` - Plugin (PublicApiPlugin per API esterne, MicrosoftLearnPlugin per MCP)
- `AIGooseGame/Components/` - Componenti Blazor (Pages, Layout)
- `AIGooseGame/wwwroot/` - CSS e JS statici per Blazor
- `AIGooseGame.ServiceDefaults/` - Aspire service defaults

## Regole Importanti

- Tutti gli agenti parlano in italiano con emoji 🎲
- Il Game Master è l'orchestratore principale (handoff workflow)
- Il tabellone è DINAMICO: generato a runtime con caselle casuali (prove + prigione)
- All'inizio del gioco si selezionano il numero di giocatori (2-4) e la dimensione del tabellone
- Caselle: 🐶=Cane, 😂=Barzelletta, 🐱=Gatto, 🍹=Cocktail, 🎮=Pokémon, 📚=Quiz, 🔒=Prigione
- Human-in-the-loop: ogni giocatore decide quando lanciare il dado
- Azure AI Foundry per i modelli (gpt-4o-mini + gpt-4o-realtime-preview)
- Cosmos DB emulatore in locale, Azure in produzione
