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

**Agenti del Gioco dell'Oca**:
- `GameMasterAgentRegistration` - Orchestratore principale del gioco 🎩
- `DogAgentRegistration` - Caselle cane (1,13,19) 🐶
- `JokeAgentRegistration` - Caselle barzelletta (2,8) 😂
- `CatAgentRegistration` - Caselle gatto (3,9,15) 🐱
- `CocktailAgentRegistration` - Caselle cocktail (4,10,16) 🍹
- `PokemonAgentRegistration` - Caselle pokemon (5,11,17) 🎮
- `BonusAgentRegistration` - Caselle bonus (6,12,18) 🎲
- `QuizAgentRegistration` - Caselle quiz MCP Microsoft Learn (7,14) 📚
- `WorkflowRegistration` - Registrazione workflow handoff

**Comunicazione tra agenti**:
- Handoff workflow: il Game Master delega agli agenti specializzati
- Agent-as-a-Tool: l'Arbitro è esposto come tool del Game Master
- OpenAI Responses + Conversations per endpoint compatibili
- MCP (Model Context Protocol): il Quiz Agent usa il server MCP di Microsoft Learn per generare quiz

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
- Il tabellone ha 20 caselle: START → [1]🐶→[2]😂→[3]🐱→[4]🍹→[5]🎮→[6]🎲→[7]📚→[8]😂→[9]🐱→[10]🍹→[11]🎮→[12]🎲→[13]🐶→[14]📚→[15]🐱→[16]🍹→[17]🎮→[18]🎲→[19]🐶→[20]🏆
- Human-in-the-loop: il giocatore umano decide quando lanciare il dado
- Azure AI Foundry per i modelli (gpt-4o-mini + gpt-4o-realtime-preview)
- Cosmos DB emulatore in locale, Azure in produzione
