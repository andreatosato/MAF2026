# 🎲 AI Gioco dell'Oca

Un gioco dell'oca interattivo alimentato da agenti AI, costruito con **Microsoft Agent Framework** (.NET 10) e orchestrato con **.NET Aspire**.

![.NET 10](https://img.shields.io/badge/.NET-10-purple)
![Aspire](https://img.shields.io/badge/Aspire-Orchestration-blue)
![MAF](https://img.shields.io/badge/Microsoft-Agent%20Framework-green)

## 🏗️ Architettura

Il gioco usa un **Workflow Handoff** per orchestrare agenti AI specializzati:

```
                    ┌─────────────────┐
                    │  🎩 Game Master  │
                    │  (Orchestratore) │
                    └───────┬─────────┘
                            │ Handoff
               ┌────────────┼────────────┐
               ▼                         ▼
    ┌──────────────────┐      ┌──────────────────┐
    │ 🎯 Challenge Agent│      │ 🔒 Prison Agent   │
    │  (Prove caselle)  │      │  (Fermo N turni)  │
    └────────┬─────────┘      └────────┬─────────┘
             │                         │
             └────────────┬────────────┘
                          ▼
               ┌──────────────────┐
               │ 📊 Score Agent    │
               │ (Punteggio/Turni) │
               └────────┬─────────┘
                         │
                         ▼
                  🎩 Game Master
                  (Prossimo turno)
```

### Agenti

| Agente | Ruolo |
|--------|-------|
| 🎩 **Game Master** | Orchestratore principale, gestisce il flusso di gioco e il lancio del dado |
| 🎯 **Challenge Agent** | Gestisce le prove sulle caselle usando tool dedicati (Dog, Joke, Cat, Cocktail, Pokemon, Quiz) |
| 🔒 **Prison Agent** | Applica la penalità prigione (fermo 1-2 turni) |
| 📊 **Score Agent** | Aggiorna il punteggio e gestisce il passaggio al giocatore successivo |

### Caselle del tabellone

Il tabellone è **dinamico**: generato a runtime con caselle casuali.

| Emoji | Tipo | Descrizione |
|-------|------|-------------|
| 🐶 | Dog | Immagine cane casuale — bonus +2 se labrador/retriever |
| 😂 | Joke | Barzelletta casuale — bonus +1 se fa ridere |
| 🐱 | Cat | Curiosità sui gatti — turno extra se menziona "sleep"/"hours" |
| 🍹 | Cocktail | Cocktail casuale — bonus +1 se non alcolico |
| 🎮 | Pokemon | Pokémon casuale (gen 1) — +1 se acqua, -2 se fuoco 🔥 |
| 📚 | Quiz | Quiz su .NET/Azure via Microsoft Learn — +3 se corretto, -1 se sbagliato |
| 🔒 | Prison | Prigione — fermo 1-2 turni |

## 📁 Struttura del progetto

```
src/
├── AIGooseGame.AppHost/          # Aspire host — AI Foundry + Cosmos DB
├── AIGooseGame/                  # Progetto principale
│   ├── Agents/                   # Registrazione agenti (GameMaster, Challenge, Prison, Score)
│   ├── Plugins/                  # PublicApiPlugin (API esterne) + MicrosoftLearnPlugin (MCP)
│   ├── Components/               # Blazor pages e layout
│   │   ├── Pages/
│   │   │   ├── Game.razor        # UI principale del gioco
│   │   │   └── ResponsesDemo.razor
│   │   └── Layout/
│   ├── wwwroot/                  # CSS e JS statici
│   └── GameState.cs              # Stato di gioco, tabellone, giocatori
├── AIGooseGame.ServiceDefaults/  # Aspire service defaults
├── WriterAgent/                  # Agente standalone — scrive racconti brevi
└── EditorAgent/                  # Agente standalone — revisiona e formatta racconti
```

## 🛠️ Stack tecnologico

- **Frontend**: Blazor Server interattivo
- **Backend**: .NET 10 + Microsoft Agent Framework (MAF)
- **LLM**: Azure AI Foundry — `gpt-4o-mini`
- **Database**: Azure Cosmos DB (emulatore locale / Azure in produzione)
- **Orchestrazione**: .NET Aspire
- **Protocollo**: Handoff Workflow + Agent-as-a-Tool
- **MCP**: Model Context Protocol per quiz Microsoft Learn

## 🚀 Come eseguire

### Prerequisiti

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/) oppure Visual Studio 2026
- Docker (per l'emulatore Cosmos DB)

### Avvio

```bash
# Con Aspire CLI
aspire start

# Oppure da Visual Studio: avvia il progetto AIGooseGame.AppHost
```

## 🎮 Come giocare

1. All'avvio, seleziona il **numero di giocatori** (2-4) e la **dimensione del tabellone**
2. Ogni giocatore (umano o bot AI) gioca a turno
3. Scrivi **"lancio"** per tirare il dado 🎲
4. Atterra su una casella e completa la prova per guadagnare bonus o subire penalità
5. Il primo giocatore a raggiungere l'ultima casella vince! 🏆

## 🔌 API esterne utilizzate

| API | Utilizzo |
|-----|----------|
| [dog.ceo](https://dog.ceo/dog-api/) | Immagini di cani casuali |
| [Official Joke API](https://official-joke-api.appspot.com/) | Barzellette |
| [Cat Facts](https://catfact.ninja/) | Curiosità sui gatti |
| [CocktailDB](https://www.thecocktaildb.com/api.php) | Cocktail casuali |
| [PokéAPI](https://pokeapi.co/) | Pokémon gen 1 |
| [Microsoft Learn](https://learn.microsoft.com/) | Quiz .NET/Azure |

## 📖 Documentazione

- [Architettura](/.github/architecture.md) — Dettagli completi dell'architettura
- [Pattern MAF](/.github/agents/maf-dotnet.agent.md) — Best practices per agenti MAF
- [Copilot Instructions](/.github/copilot-instructions.md) — Istruzioni per Copilot

## 📄 Licenza

Progetto demo per scopi educativi e presentazioni.
