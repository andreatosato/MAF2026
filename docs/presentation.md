---
marp: true
theme: default
paginate: true
backgroundColor: #1a1a2e
color: #eaeaea
style: |
  section {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  }
  h1, h2, h3 {
    color: #00d4ff;
  }
  code {
    background: #16213e;
    color: #e94560;
  }
  table {
    font-size: 0.8em;
  }
  strong {
    color: #e94560;
  }
  a {
    color: #00d4ff;
  }
  blockquote {
    border-left: 4px solid #e94560;
    padding-left: 1em;
    color: #aaa;
  }
---

<!-- _class: lead -->

# 🎲 AI Gioco dell'Oca

### Interactive Agent & Workflow Development
### with Microsoft Agent Framework and cutting-edge DevTools

**Andrea Tosato** — MAF 2026

---

# 👋 Chi sono

### Andrea Tosato

- 🏢 Software Engineer & Cloud Architect
- 💙 .NET & Azure enthusiast
- 🤖 Appassionato di AI e agenti intelligenti

### Oggi parliamo di:

> Come costruire **workflow interattivi multi-agente** con **Microsoft Agent Framework**,
> usando **DevUI**, **Function Tools** e **Human-in-the-Loop**

---

# 📋 Agenda

| Tempo | Argomento |
|---|---|
| **5 min** | 🎯 Microsoft Agent Framework — Overview |
| **5 min** | 🎲 Il Gioco dell'Oca — Concept & Architettura |
| **10 min** | 🛠️ Code Walkthrough — Workflow, Tools, Agent-as-a-Tool |
| **5 min** | 🧑 Human-in-the-Loop + Context Windows |
| **5 min** | 📊 Aspire OTel + Cosmos DB + GPT Realtime |
| **10 min** | 🎮 Live Demo — DevUI in azione |
| **5 min** | 📊 Recap & Risorse |

---

# 🎯 Microsoft Agent Framework

### Un framework completo per agenti AI in .NET

```
📦 Microsoft.Agents.AI              → Core agenti
📦 Microsoft.Agents.AI.Hosting      → Hosting in ASP.NET Core
📦 Microsoft.Agents.AI.Workflows    → Orchestrazione multi-agente
📦 Microsoft.Agents.AI.DevUI        → UI interattiva per sviluppo
📦 Microsoft.Agents.AI.Hosting.OpenAI → Endpoint OpenAI-compatibili
```

### Caratteristiche principali:
- 🔄 **Workflow**: Sequential, Concurrent, **Handoff**, Group Chat
- 🔧 **Function Tools**: `AIFunctionFactory.Create()`
- 🧑 **Human-in-the-Loop**: L'utente partecipa alle decisioni
- 🌐 **DevUI**: Debug interattivo via browser

---

# 🎲 Il Gioco dell'Oca — Concept

> Un gioco da tavolo virtuale con **20 caselle**, ciascuna associata
> a un **agente AI specializzato** che chiama una **API pubblica**

```
START → [1]🐶 → [2]😂 → [3]🐱 → [4]🍹 → [5]🎮 → [6]🎲 → [7]🐶 →
        [8]😂 → [9]🐱 → [10]🍹 → [11]🎮 → [12]🎲 → [13]🐶 →
        [14]😂 → [15]🐱 → [16]🍹 → [17]🎮 → [18]🎲 → [19]🐶 → [20]🏆
```

### Il giocatore **umano** partecipa attivamente:
- 🎲 Decide quando lanciare il dado
- 😂 Risponde alle barzellette (HITL)
- 🎲 Accetta o rifiuta le sfide bonus (HITL)
- 🧠 Risponde alle domande trivia (HITL)

---

# 🗺️ Caselle & API Pubbliche (NO AUTH!)

| Casella | Agente | API | Regola |
|---|---|---|---|
| 🐶 1,7,13,19 | **DogAgent** | Dog CEO API | Labrador/retriever → **+2** |
| 😂 2,8,14 | **JokeAgent** | Joke API | Ti fa ridere? → **+1** (HITL) |
| 🐱 3,9,15 | **CatAgent** | Cat Facts | "sleep"/"hours" → **turno extra** |
| 🍹 4,10,16 | **CocktailAgent** | CocktailDB | Analcolico → **+1** |
| 🎮 5,11,17 | **PokemonAgent** | PokéAPI | Fire → **-2** 🔥 Water → **+1** 💧 |
| 🎲 6,12,18 | **BonusAgent** | Bored API | Accetti sfida? → **+3** / **-1** (HITL) |

> Tutte le API sono **pubbliche e senza autenticazione**! 🌐

---

# 🏗️ Architettura — Handoff Workflow

```
                 ┌──────────────────────────────────────┐
                 │        Goose Game Workflow            │
   ┌──────────┐  │   ┌──────────────┐    Handoff        │
   │  🧑 User │──┼──▶│  🎩 Game     │◀────────────┐    │
   │  (HITL)  │  │   │  Master      │             │    │
   └──────────┘  │   └──────┬───────┘             │    │
                 │          │ Handoff             │    │
                 │   ┌──────┴────────────────┐    │    │
                 │   │                       │    │    │
                 │ [🐶] [😂] [🐱] [🍹] [🎮] [🎲]│───┘    │
                 │  Dog Joke Cat  🍹  🎮  Bonus   │        │
                 │   │   │   │   │   │    │      │        │
                 │  API API API API API  API     │        │
                 └──────────────────────────────┘
```

**Pattern**: Game Master → **Handoff** → Agente Casella → **Handoff back** → Game Master

---

# 💻 Code — Registrazione Agenti

```csharp
// 🎩 Game Master — orchestratore principale
var gameMaster = builder.AddAIAgent(
    "game-master",
    """
    🎩 Sei il Game Master del Gioco dell'Oca!
    Quando l'utente dice 'lancio':
    1. Usa RollDice per lanciare il dado 🎲
    2. Annuncia il risultato con entusiasmo
    3. Fai l'handoff all'agente corretto
    """,
    chatClient)
    .WithAITool(sp => AIFunctionFactory.Create(
        sp.GetRequiredService<PublicApiPlugin>().RollDice));
```

> `AddAIAgent()` + `WithAITool()` = **agente con strumenti** in 5 righe! 🚀

---

# 💻 Code — Function Tools (API Calls)

```csharp
public class PublicApiPlugin(HttpClient httpClient)
{
    [Description("Lancia il dado (1-6)")]
    public int RollDice() => Random.Shared.Next(1, 7);

    [Description("Ottieni un cane casuale con razza e immagine")]
    public async Task<string> GetRandomDog()
    {
        var response = await httpClient
            .GetFromJsonAsync<JsonElement>(
                "https://dog.ceo/api/breeds/image/random");
        // ... parse breed, imageUrl, bonusEligible
    }
}
```

> `[Description]` → il modello AI capisce **quando** e **come** usare lo strumento

---

# 💻 Code — Handoff Workflow

```csharp
builder.AddWorkflow("goose-game", (sp, key) =>
{
    var gm = sp.GetRequiredKeyedService<AIAgent>("game-master");
    var dog = sp.GetRequiredKeyedService<AIAgent>("dog-agent");
    var joke = sp.GetRequiredKeyedService<AIAgent>("joke-agent");
    // ... altri agenti

    return AgentWorkflowBuilder
        .CreateHandoffBuilderWith(gm)       // 🎩 Parte dal GM
        .WithHandoffs(gm, [dog, joke, ...]) // GM → agenti
        .WithHandoffs([dog, joke, ...], gm) // agenti → GM
        .Build();
}).AddAsAIAgent();
```

> **3 metodi** per definire un workflow complesso con 7 agenti! ✨

---

# 🧑 Human-in-the-Loop (HITL)

### Il giocatore umano è parte attiva del gioco!

**3 momenti HITL nel flusso di gioco:**

| Momento | Agente | Domanda al Giocatore | Effetto |
|---|---|---|---|
| 😂 **Barzelletta** | JokeAgent | *"Ti ha fatto ridere?"* | Sì → +1, No → niente |
| 🎲 **Sfida Bonus** | BonusAgent | *"Accetti la sfida?"* | Sì → +3, No → -1 |
| 🧠 **Trivia** | BonusAgent | *"Rispondi alla domanda!"* | Corretto → +3, Sbagliato → -1 |

> L'agente **attende la risposta dell'utente** prima di procedere.
> Questo è **Human-in-the-Loop** nativo nel workflow! 🔄

---

# 🧑 HITL — Come Funziona

```
Game Master: "🎲 Hai fatto 2! Casella 😂 Barzelletta!"
             [HANDOFF → joke-agent]

Joke Agent:  "😂 Ecco la barzelletta..."
             "Setup: Why don't scientists trust atoms?"
             "..."
             "Punchline: Because they make up everything!"
             "🤔 Ti ha fatto ridere? Rispondi sì o no!"

  ┌─────────────────────────────────┐
  │  ⏸️  ATTESA RISPOSTA UMANA      │  ← HITL!
  └─────────────────────────────────┘

Utente:      "Sì, mi ha fatto ridere! 😂"

Joke Agent:  "🎉 +1 casella bonus!"
             [HANDOFF → game-master]
```

---

# ⚖️ Agent-as-a-Tool (4️⃣)

### Un agente come strumento di un altro agente!

```csharp
// 1. Crea l'Agente Arbitro
var arbitroAgent = chatClient.AsAIAgent(
    name: "arbitro-agent",
    instructions: "⚖️ Verifica le regole del gioco...",
    description: "Arbitro delle regole");

// 2. Registralo come tool del Game Master
gameMaster.WithAITool(sp => arbitroAgent.AsAIFunction());
```

Il Game Master **chiama l'Arbitro come un tool** quando serve una verifica delle regole!

> `.AsAIFunction()` = trasforma un agente intero in un Function Tool ✨

---

# 🔄 Context Windows (2️⃣)

### Gestione intelligente della storia conversazionale

Il framework offre strategie di **compattazione** per gestire il context window:

- **`SlidingWindowCompactionStrategy`** — Mantiene le ultime N conversazioni
- **`SummarizationCompactionStrategy`** — Riassume la storia precedente
- **`TruncationCompactionStrategy`** — Taglia i messaggi più vecchi
- **`PipelineCompactionStrategy`** — Combina più strategie in pipeline

```csharp
// Il ChatHistoryMemoryProvider gestisce automaticamente
// la storia conversazionale di ogni sessione
```

> Il **context window** è fondamentale per agenti multi-turno come il nostro gioco! 🧠

---

# 📊 Aspire / OpenTelemetry (3️⃣)

### Osservabilità nativa per agenti AI

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AIGooseGame"))
    .WithMetrics(m => m.AddAspNetCoreInstrumentation()
        .AddOtlpExporter(...))
    .WithTracing(t => t.AddAspNetCoreInstrumentation()
        .AddOtlpExporter(...));
```

**Dashboard Aspire** (`http://localhost:18888`):
- 📈 **Metriche**: richieste/sec, latenza, errori
- 🔍 **Trace**: ogni chiamata AI, tool invocation, handoff
- 📝 **Log**: messaggi strutturati da ogni agente

> `docker run --rm -p 18888:18888 -p 4317:18889 mcr.microsoft.com/dotnet/aspire-dashboard`

---

# 💾 Cosmos DB (5️⃣)

### Salvataggio automatico delle conversazioni

```csharp
var options = new ChatClientAgentOptions();
options.WithCosmosDBChatHistoryProvider(
    connectionString, "GooseGameDB", "ChatHistory");
```

**Cosa viene salvato su Cosmos DB:**
- 💬 Ogni messaggio utente e agente
- 🔄 Le transizioni di handoff
- 🎲 I risultati dei tool calls
- 🏆 Lo stato della partita

> Le conversazioni **sopravvivono** ai restart dell'app! 🔒

---

# 🎤 GPT Realtime (6️⃣)

### Interazione vocale in tempo reale

```csharp
#pragma warning disable OPENAI002
var realtimeClient = azureClient.GetRealtimeClient();
var session = await realtimeClient
    .StartConversationSessionAsync("gpt-4o-realtime-preview");
#pragma warning restore OPENAI002
```

**Come funziona:**
1. 🎙️ Il client si connette via WebSocket
2. 🗣️ Il giocatore **parla** al Game Master
3. 🔊 Il Game Master **risponde a voce**
4. 🎲 Stesse regole, stesse prove, ma a voce!

> Endpoint: `GET /realtime/session` → info di connessione WebSocket

---

# 🛠️ DevUI — Debug Interattivo

### L'arma segreta per lo sviluppo di agenti!

```csharp
// Registrazione (3 righe!)
builder.AddDevUI();
// ...
app.MapDevUI();
```

**Cosa offre DevUI:**
- 💬 Chat interattiva con gli agenti
- 🔄 Visualizzazione Handoff in tempo reale
- 📊 Ispezione dei messaggi e tool calls
- 🐛 Debug senza Postman o curl

> Apri il browser su `http://localhost:5150/devui` e gioca! 🎮

---

# 💻 Code — DevUI + Endpoints

```csharp
// OpenAI-compatible endpoints
builder.AddOpenAIResponses();
builder.AddOpenAIConversations();
builder.AddDevUI();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

// Game REST endpoints
app.MapPost("/game/join/{playerName}", ...);
app.MapGet("/game/scoreboard", ...);
```

> **DevUI** + **OpenAI Responses** + **REST** = API completa in poche righe

---

# 💻 Code — Multi-Player State

```csharp
public record PlayerState(
    string Name,
    int Position = 0,
    int TurnsPlayed = 0,
    bool HasFinished = false,
    bool IsHuman = false        // 🧑 Giocatore umano!
);

public class GameState
{
    private readonly ConcurrentDictionary<string, PlayerState> _players
        = new(StringComparer.OrdinalIgnoreCase);

    public PlayerState JoinGame(string name, bool isHuman = false)
    {
        var player = new PlayerState(name, IsHuman: isHuman);
        _players[name] = player;
        return player;
    }
}
```

---

# 🎮 LIVE DEMO!

### Giochiamo insieme al Gioco dell'Oca! 🎲

**Cosa vedremo:**
1. 🚀 Avvio dell'app + Dashboard Aspire
2. 🧑 Registrazione del giocatore umano
3. 🎲 Lancio del dado e navigazione del tabellone
4. 🐶 Handoff al Dog Agent → immagine cane da API
5. 😂 Handoff al Joke Agent → barzelletta + HITL
6. ⚖️ Game Master chiama l'Arbitro (Agent-as-a-Tool)
7. 📊 Trace nella dashboard Aspire
8. 🏆 Classifica finale

> `http://localhost:5150/devui`

---

# 📊 Recap — Le 6 Feature Dimostrate

| # | Feature | Come la usiamo |
|---|---|---|
| 1️⃣ | **Workflow Handoff** | `AgentWorkflowBuilder` → 8 agenti orchestrati 🔄 |
| 2️⃣ | **Context Windows** | `SlidingWindowCompactionStrategy` per la storia 🧠 |
| 3️⃣ | **Aspire Log AI** | OpenTelemetry → Dashboard Aspire 📊 |
| 4️⃣ | **Agent-as-a-Tool** | `arbitroAgent.AsAIFunction()` → tool del GM ⚖️ |
| 5️⃣ | **Cosmos DB** | `WithCosmosDBChatHistoryProvider` 💾 |
| 6️⃣ | **GPT Realtime** | `StartConversationSessionAsync()` 🎤 |
| | **HITL** | Giocatore umano prende decisioni 🧑 |
| | **Function Tools** | 6 API pubbliche + RollDice 🔧 |
| | **DevUI** | Debug interattivo via browser 🛠️ |

---

# 🏗️ Sotto il Cofano — Il Codice Completo

```
src/AIGooseGame/
├── AIGooseGame.csproj          → 7 pacchetti NuGet
├── Program.cs                  → 7 agenti + workflow + endpoints
│                                  (~120 righe di logica!)
├── GameState.cs                → Multi-player state
├── Plugins/
│   └── PublicApiPlugin.cs      → 6 API + RollDice
├── appsettings.json            → Azure OpenAI config
└── Properties/
    └── launchSettings.json     → http://localhost:5150
```

> **~250 righe di codice** per un gioco multi-agente completo! 🎯

---

# 🔗 Risorse

### Dove trovare tutto:

- 🐙 **Repo Demo**: [github.com/andreatosato/MAF2026](https://github.com/andreatosato/MAF2026)
- 📦 **Microsoft Agent Framework**: [github.com/microsoft/agent-framework](https://github.com/microsoft/agent-framework)
- 📖 **Documentazione**: [learn.microsoft.com/agent-framework](https://learn.microsoft.com/agent-framework/overview/)
- 💬 **Discord**: [discord.gg/b5zjErwbQM](https://discord.gg/b5zjErwbQM)
- 📝 **NuGet**: [nuget.org/profiles/MicrosoftAgentFramework](https://www.nuget.org/profiles/MicrosoftAgentFramework)

---

<!-- _class: lead -->

# Grazie! 🙏

### 🎲 AI Gioco dell'Oca
### con Microsoft Agent Framework

**Andrea Tosato**

> Domande? 🤔

---

<!-- _class: lead -->

# Appendice

---

# 📎 API Pubbliche Utilizzate

| API | URL | Dati |
|---|---|---|
| 🐶 **Dog CEO** | `dog.ceo/api/breeds/image/random` | Immagini cani random |
| 😂 **Official Joke** | `official-joke-api.appspot.com` | Barzellette (setup + punchline) |
| 🐱 **Cat Facts** | `catfact.ninja/fact` | Fatti sui gatti |
| 🍹 **CocktailDB** | `thecocktaildb.com/api` | Cocktail random |
| 🎮 **PokéAPI** | `pokeapi.co/api/v2/pokemon` | Pokémon (gen 1) |
| 🎲 **Bored API** | `boredapi.com/api/activity` | Attività random |

> **Tutte gratuite, tutte senza autenticazione!** 🌐

---

# 📎 Pattern Handoff — Dettaglio

```
              ┌──────────┐
              │   User   │
              └────┬─────┘
                   │ "Lancio!"
              ┌────▼─────┐
              │   Game   │──── RollDice() → 4
              │  Master  │
              └────┬─────┘
                   │ Handoff (casella 4 = 🍹)
              ┌────▼─────┐
              │ Cocktail │──── GetCocktail() → "Mojito"
              │  Agent   │
              └────┬─────┘
                   │ Handoff back
              ┌────▼─────┐
              │   Game   │ "Sei alla casella 4!"
              │  Master  │
              └──────────┘
```
