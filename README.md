# MAF2026 — Interactive Agent & Workflow Development

> **Sessione:** 10:30–11:15 — *Interactive Agent & Workflow Development with Microsoft Agent Framework and cutting-edge DevTools*
> **Speaker:** Andrea Tosato — MAF 2026

---

## 🎲 AI Gioco dell'Oca — Demo

Una demo completa del **Gioco dell'Oca** (Goose Game) costruita con **Microsoft Agent Framework** (.NET), che mostra:

| Feature | Dove viene usata |
|---|---|
| **Handoff Workflow** | Il Game Master passa il turno all'agente specializzato della casella |
| **DevUI** | Interfaccia web interattiva per testare e debuggare gli agenti |
| **Function Tools** | Ogni agente chiama una API pubblica (no auth!) |
| **Multi-user Sessions** | `GameState` con `ConcurrentDictionary` per più giocatori contemporanei |
| **AgentWorkflowBuilder** | Costruzione dichiarativa del workflow con pattern Handoff |

---

## 🗺️ Il Tabellone

```
START → [1]🐶 → [2]😂 → [3]🐱 → [4]🍹 → [5]🎮 → [6]🎲 → [7]🐶 →
        [8]😂 → [9]🐱 → [10]🍹 → [11]🎮 → [12]🎲 → [13]🐶 →
        [14]😂 → [15]🐱 → [16]🍹 → [17]🎮 → [18]🎲 → [19]🐶 → [20]🏆 FINE
```

### Caselle e Regole di Gioco

| Casella | Agente | API Pubblica (NO AUTH) | Regola |
|---|---|---|---|
| 🐶 (1,7,13,19) | **DogAgent** | `https://dog.ceo/api/breeds/image/random` | Mostra un cane. Se razza labrador/retriever → **+2 bonus!** |
| 😂 (2,8,14) | **JokeAgent** | `https://official-joke-api.appspot.com/random_joke` | Racconta una barzelletta. Se l'utente ride → **+1 bonus** (HITL) |
| 🐱 (3,9,15) | **CatAgent** | `https://catfact.ninja/fact` | Condivide un fatto sui gatti. Se menziona "sleep"/"hours" → **turno extra!** |
| 🍹 (4,10,16) | **CocktailAgent** | `https://www.thecocktaildb.com/api/json/v1/1/random.php` | Serve un cocktail. Se analcolico → **+1 bonus** (guida responsabile!) |
| 🎮 (5,11,17) | **PokemonAgent** | `https://pokeapi.co/api/v2/pokemon/{1-151}` | Cattura un Pokémon. Se fire → **-2** 🔥. Se water → **+1** 💧 |
| 🎲 (6,12,18) | **BonusAgent** | `https://www.boredapi.com/api/activity` | Sfida bonus. Accetti → **+3**. Rifiuti → **-1** |

---

## 🏗️ Architettura

```
                    ┌─────────────────────────────────────────────┐
                    │          Goose Game Workflow                 │
                    │                                             │
      ┌─────────┐   │   ┌──────────────┐     Handoff             │
      │  User   │───┼──▶│  Game Master │◀──────────────────┐     │
      └─────────┘   │   │  🎩 (dice)   │                   │     │
                    │   └──────┬───────┘                   │     │
                    │          │ Handoff                   │     │
                    │    ┌─────┴──────────────────────┐    │     │
                    │    │                            │    │     │
                    │  [🐶]  [😂]  [🐱]  [🍹]  [🎮]  [🎲]│────┘     │
                    │ Dog  Joke  Cat Cocktail Pokemon Bonus│         │
                    │  │    │    │    │      │      │     │         │
                    │ API  API  API  API    API    API    │         │
                    └─────────────────────────────────────┘         
                                    │
                    ┌───────────────┴───────────────┐
                    │       ASP.NET Core Host        │
                    │  /devui  /openai/v1/responses  │
                    │  /game/join/{player}           │
                    │  /game/scoreboard              │
                    └────────────────────────────────┘
```

---

## 📁 Struttura del Progetto

```
src/
└── AIGooseGame/
    ├── AIGooseGame.csproj          # Packages: Microsoft.Agents.AI.*
    ├── Program.cs                  # 7 agenti + Handoff workflow + DevUI + endpoints
    ├── GameState.cs                # Multi-player con ConcurrentDictionary
    ├── Plugins/
    │   └── PublicApiPlugin.cs      # 6 API pubbliche + RollDice
    ├── appsettings.json            # Azure OpenAI config
    ├── appsettings.Development.json
    └── Properties/
        └── launchSettings.json    # http://localhost:5150
```

---

## 📦 NuGet Packages

```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-rc4" />
<PackageReference Include="Microsoft.Agents.AI.DevUI" Version="1.0.0-preview.260311.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.260311.1" />
<PackageReference Include="Microsoft.Agents.AI.Hosting.OpenAI" Version="1.0.0-alpha.260311.1" />
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.0.0-rc4" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
<PackageReference Include="Azure.Identity" Version="1.13.2" />
```

---

## ⚙️ Prerequisiti

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Azure OpenAI** con deployment di `gpt-4o-mini` (o modello compatibile)
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

---

## 🎮 Come si Gioca (Esempio in Italiano)

```
Utente: "Voglio giocare! Mi chiamo Mario"

# Prima entra nella partita:
POST /game/join/Mario
→ { "message": "🎉 Benvenuto Mario! Sei pronto a giocare al Gioco dell'Oca!" }

# Poi nella DevUI (o via API):
Utente: "Lancia il dado per Mario!"

Game Master: "🎲 Lancio il dado... HO OTTENUTO UN 4! 🎉
             Mario avanza da 0 a 4! Sei atterrato sulla casella 🍹 Cocktail!
             Passo la parola all'Agente Cocktail..."

[HANDOFF → cocktail-agent]

Cocktail Agent: "🍹 Benvenuto al bar! Ho preparato per te...
                *sfoglia il menù con stile*
                ...un Mojito! 🌿🍋
                È un cocktail ALCOLICO.
                Istruzioni: Mint, lime, rum, sugar, soda water...
                Non c'è bonus questa volta, ma che gusto! 😅"

[HANDOFF BACK → game-master]

Game Master: "Ottimo! Mario è alla casella 4. 
             Chi è il prossimo? 🎲"
```

---

## 🛠️ Feature del Framework Dimostrate

| Feature | Implementazione |
|---|---|
| `AddAIAgent()` | 7 agenti registrati in DI con istruzioni in italiano |
| `WithAITool()` | Ogni agente ha il suo Function Tool collegato alla API |
| `AgentWorkflowBuilder.CreateHandoffBuilderWith()` | Game Master come punto di partenza |
| `.WithHandoffs(gm, [agents])` | GM può passare il turno a 6 agenti |
| `.WithHandoffs([agents], gm)` | Gli agenti tornano sempre al GM |
| `AddWorkflow().AddAsAIAgent()` | Il workflow esposto come agente |
| `builder.AddDevUI()` + `app.MapDevUI()` | DevUI su `/devui` |
| `AddOpenAIResponses()` + `MapOpenAIResponses()` | Endpoint OpenAI-compatibile |
| `ConcurrentDictionary<string, PlayerState>` | Multi-player thread-safe |
| `AIFunctionFactory.Create()` | Creazione tool da metodi del plugin |

---

## 🔗 Risorse

- [Microsoft Agent Framework — GitHub](https://github.com/microsoft/agent-framework)
- [Documentazione ufficiale](https://learn.microsoft.com/agent-framework/overview/)
- [NuGet — MicrosoftAgentFramework](https://www.nuget.org/profiles/MicrosoftAgentFramework)
- [Discord Community](https://discord.gg/b5zjErwbQM)
- [Blog post di annuncio](https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/)

---

*Demo creata per MAF 2026 — Andrea Tosato*
