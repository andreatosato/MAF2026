# MAF2026 — Interactive Agent & Workflow Development

> **Sessione:** 10:30–11:15 — *Interactive Agent & Workflow Development with Microsoft Agent Framework and cutting-edge DevTools*
> **Speaker:** Andrea Tosato — MAF 2026

---

## 🎲 AI Gioco dell'Oca — Demo

Una demo completa del **Gioco dell'Oca** (Goose Game) costruita con **Microsoft Agent Framework** (.NET), che mostra:

| Feature | Dove viene usata |
|---|---|
| **1️⃣ Workflow Handoff** | Il Game Master passa il turno al Challenge Agent o Prison Agent |
| **2️⃣ Context Windows** | `SlidingWindowCompactionStrategy` per gestire la storia conversazionale |
| **3️⃣ Aspire / OpenTelemetry** | Log, trace e metriche AI con dashboard Aspire |
| **4️⃣ Agent-as-a-Tool** | Gli ex-agenti (Dog, Joke, Cat, ecc.) sono ora tool del Challenge Agent |
| **5️⃣ Cosmos DB** | Salvataggio cronologia chat con `WithCosmosDBChatHistoryProvider` |
| **Human-in-the-Loop** | Il giocatore umano risponde a domande, accetta sfide, e prende decisioni |
| **DevUI** | Interfaccia web interattiva per testare e debuggare gli agenti |
| **Function Tools** | Ogni tipo di casella corrisponde a un tool che chiama un'API pubblica (no auth!) |

---

## 🗺️ Il Tabellone

Il tabellone è **dinamico**: generato casualmente a runtime. Ogni partita ha caselle diverse!
All'inizio si selezionano il numero di giocatori (2-4) e la dimensione del tabellone (10-40 caselle).

### Tipi di caselle

| Casella | Tool (del Challenge Agent) | API Pubblica (NO AUTH) | Regola |
|---|---|---|---|
| 🐶 Dog | `GetRandomDog` | `https://dog.ceo/api/breeds/image/random` | Mostra un cane. Se razza labrador/retriever → **+2 bonus!** |
| 😂 Joke | `GetJoke` | `https://official-joke-api.appspot.com/random_joke` | Racconta una barzelletta. Se l'utente ride → **+1 bonus** (HITL) |
| 🐱 Cat | `GetCatFact` | `https://catfact.ninja/fact` | Condivide un fatto sui gatti. Se menziona "sleep"/"hours" → **turno extra!** |
| 🍹 Cocktail | `GetCocktail` | `https://www.thecocktaildb.com/api/json/v1/1/random.php` | Serve un cocktail. Se analcolico → **+1 bonus** (guida responsabile!) |
| 🎮 Pokemon | `GetPokemon` | `https://pokeapi.co/api/v2/pokemon/{1-151}` | Cattura un Pokémon. Se fire → **-2** 🔥. Se water → **+1** 💧 |
| 📚 Quiz | `SearchMicrosoftLearn` | `https://learn.microsoft.com/api/search` | Quiz su .NET/Azure. Corretto → **+3**. Sbagliato → **-1** |
| 🔒 Prison | — (Prison Agent) | — | Prigione! Fermo **1-2 turni** |

---

## 🏗️ Architettura

```
                    ┌─────────────────────────────────────────────┐
                    │          Goose Game Workflow                 │
                    │                                             │
      ┌─────────┐   │   ┌──────────────┐     Handoff             │
      │  User   │───┼──▶│  Game Master │◀──────────────────┐     │
      │ (HITL)  │   │   │  🎩 (dice)   │                   │     │
      └─────────┘   │   └──────┬───────┘                   │     │
                    │          │ Handoff                   │     │
                    │    ┌─────┴─────────┐                 │     │
                    │    │               │                 │     │
                    │  ┌─▼────────────┐ ┌▼──────────────┐  │     │
                    │  │🎯 Challenge  │ │🔒 Prison      │  │     │
                    │  │  Agent       │ │  Agent        │  │     │
                    │  │              │ │  (fermo N     │  │     │
                    │  │ Tools:       │ │   turni)      │  │     │
                    │  │ 🐶🐱😂🍹🎮📚│ └──────┬────────┘  │     │
                    │  └──────┬───────┘        │          │     │
                    │         └────────┬───────┘          │     │
                    │                  ▼                  │     │
                    │          ┌───────────────┐          │     │
                    │          │📊 Score Agent │──────────┘     │
                    │          │ (turni/punti) │               │
                    │          └───────────────┘               │
                    └─────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │       ASP.NET Core Host        │
                    │  /devui  /openai/v1/responses  │
                    │  /game/join  /game/scoreboard  │
                    └───────┬────────────────────────┘
                            │
                    ┌───────┴────────┐
                    │                │
              ┌─────▼──────┐  ┌─────▼──────┐
              │ Cosmos DB  │  │   Aspire   │
              │ Chat Store │  │   OTel     │
              └────────────┘  └────────────┘
```

---

## 🧑 Human-in-the-Loop (HITL)

Il giocatore umano è **parte attiva del gioco**! Gli agenti non procedono autonomamente ma **attendono le decisioni del giocatore**:

| Agente | Momento HITL | Domanda al Giocatore | Effetto |
|---|---|---|---|
| 🎩 **Game Master** | Ogni turno | *"Scrivi 'lancio' per lanciare il dado!"* | Il gioco procede solo quando il giocatore lo decide |
| 🎯 **Challenge Agent** | Dopo ogni prova | Domanda specifica in base alla casella (es: "Ti piace questo cane?", "Ti ha fatto ridere?") | Interazione personale + bonus |
| 🔒 **Prison Agent** | Arrivo in prigione | *"Sei finito in prigione! ⛓️"* | Fermo 1-2 turni |

### Come funziona il HITL:

```
Joke Agent:  "😂 Ecco la barzelletta..."
             "Setup: Why don't scientists trust atoms?"
             "Punchline: Because they make up everything!"
             "🤔 Ti ha fatto ridere? Rispondi SÌ o NO!"

  ┌─────────────────────────────────┐
  │  ⏸️  ATTESA RISPOSTA UMANA      │  ← Human-in-the-Loop!
  └─────────────────────────────────┘

Utente:      "Sì! 😂"

Joke Agent:  "🎉 +1 casella bonus! La risata è la miglior medicina!"
             [HANDOFF → game-master]
```

> L'agente **si ferma e attende** la risposta dell'utente prima di decidere il bonus.
> Questo è il pattern **Human-in-the-Loop nativo** nel workflow Handoff! 🔄

---

## 📊 Presentazione

La presentazione completa in formato Marp è disponibile in [`docs/presentation.md`](docs/presentation.md).

Per visualizzarla:
- **VS Code**: Installa l'estensione [Marp for VS Code](https://marketplace.visualstudio.com/items?itemName=marp-team.marp-vscode)
- **CLI**: `npx @marp-team/marp-cli docs/presentation.md --html`
- **Browser**: Apri il file con qualsiasi viewer Marp-compatible

---

## 4️⃣ Agent-as-a-Tool

L'**Agente Arbitro** (⚖️) è un agente specializzato nelle regole del gioco, esposto come **Function Tool** tramite `.AsAIFunction()`:

```csharp
// L'Arbitro viene creato come agente standalone
var arbitroAgent = chatClient.AsAIAgent(
    name: "arbitro-agent",
    instructions: "⚖️ Sei l'Arbitro del Gioco dell'Oca! Verifica le regole...",
    description: "Agente Arbitro che verifica le regole del gioco");

// ...e registrato come tool del Game Master
gameMaster.WithAITool(sp => arbitroAgent.AsAIFunction());
```

Il Game Master può **chiamare l'Arbitro come un tool** per verificare regole complesse — questo è il pattern **Agent-as-a-Tool** di Microsoft Agent Framework!

---

## 5️⃣ Salvataggio Conversazione su Cosmos DB

Se `COSMOS_CONNECTION_STRING` è configurata, la cronologia della chat viene salvata automaticamente su **Azure Cosmos DB** tramite `CosmosChatHistoryProvider`:

```csharp
var options = new ChatClientAgentOptions();
options.WithCosmosDBChatHistoryProvider(
    cosmosConnectionString, cosmosDatabase, cosmosContainer);
```

Configurazione in `appsettings.json`:
```json
{
  "COSMOS_CONNECTION_STRING": "AccountEndpoint=https://YOUR-COSMOS.documents.azure.com:443/;AccountKey=...",
  "COSMOS_DATABASE_NAME": "GooseGameDB",
  "COSMOS_CONTAINER_NAME": "ChatHistory"
}
```

---

## 3️⃣ Aspire / OpenTelemetry — AI Logging

L'app è configurata con **OpenTelemetry** compatibile con la **dashboard .NET Aspire**, per monitorare log, trace e metriche di tutte le chiamate AI:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("AIGooseGame"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint)))
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
        .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint)));
```

Per avviare la dashboard Aspire:
```bash
docker run --rm -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard
```
Poi apri `http://localhost:18888` per visualizzare trace e log degli agenti.

---

## 📖 Descrizione Completa del Gioco

Le regole complete, le prove, il flusso di gioco e la spiegazione dettagliata di tutte le caselle sono in [`docs/come-si-gioca.md`](docs/come-si-gioca.md).

---

## 📁 Struttura del Progetto

```
src/
├── AIGooseGame.AppHost/          # Aspire host — AI Foundry + Cosmos DB
├── AIGooseGame/
│   ├── AIGooseGame.csproj
│   ├── Program.cs                  # 4 agenti + Workflow Handoff + Cosmos + OTel
│   ├── GameState.cs                # Multi-player con tabellone dinamico casuale
│   ├── Agents/                     # GameMaster, Challenge, Prison, Score
│   ├── Plugins/
│   │   ├── PublicApiPlugin.cs      # 6 API pubbliche + dado + game logic
│   │   └── MicrosoftLearnPlugin.cs # Microsoft Learn Search per quiz
│   ├── Components/                 # Blazor pages e layout
│   ├── appsettings.json
│   └── Properties/
│       └── launchSettings.json
├── AIGooseGame.ServiceDefaults/    # Aspire service defaults
├── WriterAgent/                    # Agente standalone — scrive racconti brevi
├── EditorAgent/                    # Agente standalone — revisiona racconti
docs/
├── come-si-gioca.md               # Descrizione completa del gioco e delle prove
└── presentation.md                # Slide della presentazione (Marp)
```

---

## 📦 NuGet Packages

```xml
<!-- Microsoft Agent Framework -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-rc4" />
<PackageReference Include="Microsoft.Agents.AI.DevUI" Version="1.0.0-preview.260311.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.260311.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.OpenAI" Version="1.0.0-alpha.260311.1" />
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.0.0-rc4" />
<PackageReference Include="Microsoft.Agents.AI.CosmosNoSql" Version="1.0.0-preview.260311.1" />

<!-- Azure OpenAI -->
<PackageReference Include="Azure.AI.OpenAI" Version="2.9.0-beta.1" />
<PackageReference Include="Azure.Identity" Version="1.17.1" />

<!-- OpenTelemetry / Aspire-compatible logging -->
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.15.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.0" />
```

---

## ⚙️ Prerequisiti

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Azure AI Foundry** con deployment di `gpt-4o-mini` (o modello compatibile)
- (Opzionale) **Azure Cosmos DB** per il salvataggio delle conversazioni
- (Opzionale) **Docker** per l'emulatore Cosmos DB e la dashboard Aspire
- (Opzionale) Azure CLI per l'autenticazione con `DefaultAzureCredential`

---

## 🚀 Come Eseguire

### 1. Configura Azure OpenAI

**Opzione A — `appsettings.json`:**
```json
{
  "AZURE_OPENAI_ENDPOINT": "https://YOUR-RESOURCE.openai.azure.com/",
  "AZURE_OPENAI_DEPLOYMENT_NAME": "gpt-4o-mini"
}
```

**Opzione B — Variabili d'ambiente:**
```bash
export AZURE_OPENAI_ENDPOINT="https://YOUR-RESOURCE.openai.azure.com/"
export AZURE_OPENAI_DEPLOYMENT_NAME="gpt-4o-mini"
```

**Opzione C — User Secrets (sviluppo):**
```bash
cd src/AIGooseGame
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://YOUR-RESOURCE.openai.azure.com/"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT_NAME" "gpt-4o-mini"
```

### 2. Avvia l'applicazione

```bash
cd src/AIGooseGame
dotnet run
```

### 3. Accedi agli endpoints

| URL | Descrizione |
|---|---|
| `http://localhost:5150/devui` | **DevUI** — interfaccia interattiva per testare gli agenti |
| `http://localhost:5150/openai/v1/responses` | OpenAI Responses API compatibile |
| `http://localhost:5150/openai/v1/conversations` | OpenAI Conversations API |
| `POST http://localhost:5150/game/join/{nome}` | Entra nella partita |
| `GET http://localhost:5150/game/scoreboard` | Classifica corrente |
| `http://localhost:18888` | **Dashboard Aspire** (se Docker attivo) |

---

## 🎮 Come si Gioca (Esempio in Italiano)

```
# 1️⃣ Prima entra nella partita come giocatore umano:
POST /game/join/Mario?isHuman=true
→ { "message": "🎉 Benvenuto Mario! (🧑 Giocatore Umano) Sei pronto!" }

# 2️⃣ Poi nella DevUI (http://localhost:5150/devui):
Utente: "Ciao! Sono Mario, voglio giocare!"

Game Master: "🎩 Benvenuto Mario nel Gioco dell'Oca!
             Il tabellone ti aspetta con 20 caselle di avventura!
             Scrivi 'lancio' quando sei pronto! 🎲"

# 3️⃣ Il giocatore decide quando lanciare (HITL):
Utente: "Lancio!"

Game Master: "🎲 Lancio il dado... HO OTTENUTO UN 2! 🎉
             Mario avanza alla casella 2! Casella 😂 Barzelletta!
             Passo la parola all'Agente delle Prove..."

[HANDOFF → challenge-agent]

# 4️⃣ L'agente esegue la prova e ATTENDE la risposta umana (HITL):
Challenge Agent: "😂 Attenzione, attenzione!
            Setup: Why don't scientists trust atoms?
            ...
            🥁 Punchline: Because they make up everything!
            
            🤔 Ti ha fatto ridere? Rispondi SÌ o NO!"

                    ⏸️ ATTESA RISPOSTA UMANA (HITL)

Utente: "Sì, mi ha fatto ridere! 😂"

Challenge Agent: "🎉 +1 casella bonus! La risata è la miglior medicina! 😂"

[HANDOFF → score-agent → game-master]

# 5️⃣ Il Game Master aspetta il prossimo turno (HITL):
Game Master: "Ottimo Mario! Sei alla casella 3!
             Vuoi continuare? Scrivi 'lancio' per il prossimo turno! 🎲"
```

---

## 🛠️ Feature del Framework Dimostrate

| Feature | Implementazione |
|---|---|
| **1️⃣ Workflow** `AddWorkflow()` | Handoff workflow tra 4 agenti: GM → Challenge/Prison → Score → GM |
| **2️⃣ Context Windows** | `SlidingWindowCompactionStrategy` per gestione storia |
| **3️⃣ Aspire Log AI** | OpenTelemetry con OTLP exporter verso dashboard Aspire |
| **4️⃣ Agent-as-a-Tool** | Gli ex-agenti ora sono tool del Challenge Agent |
| **5️⃣ Cosmos DB** | `WithCosmosDBChatHistoryProvider` per persistenza chat |
| `AddAIAgent()` | 4 agenti registrati in DI con istruzioni in italiano |
| `WithAITool()` | Challenge Agent usa 6 tool (API pubbliche) + SearchMicrosoftLearn |
| `AgentWorkflowBuilder.CreateHandoffBuilderWith()` | Game Master come punto di partenza |
| `.WithHandoffs(gm, [challenge, prison])` | GM può passare a Challenge o Prison |
| `.WithHandoffs(challenge, [score])` | Challenge → Score |
| `.WithHandoffs(prison, [score])` | Prison → Score |
| `.WithHandoffs(score, [gm])` | Score torna al GM |
| `AddWorkflow().AddAsAIAgent()` | Il workflow esposto come agente |
| `builder.AddDevUI()` + `app.MapDevUI()` | DevUI su `/devui` |
| `AddOpenAIResponses()` + `MapOpenAIResponses()` | Endpoint OpenAI-compatibile |
| **Human-in-the-Loop** | Agenti attendono decisioni del giocatore umano |
| `ConcurrentDictionary<string, PlayerState>` | Multi-player thread-safe (human + AI) |
| `AIFunctionFactory.Create()` | Creazione tool da metodi del plugin |
| **Tabellone Dinamico** | Caselle generate casualmente a runtime |

---

## 🔗 Risorse

- [Microsoft Agent Framework — GitHub](https://github.com/microsoft/agent-framework)
- [Documentazione ufficiale](https://learn.microsoft.com/agent-framework/overview/)
- [NuGet — MicrosoftAgentFramework](https://www.nuget.org/profiles/MicrosoftAgentFramework)
- [Discord Community](https://discord.gg/b5zjErwbQM)
- [Blog post di annuncio](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/)

---

*Demo creata per MAF 2026 — Andrea Tosato*
