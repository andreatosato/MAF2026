# 🎲 Come si Gioca — AI Gioco dell'Oca

## Panoramica

Il **Gioco dell'Oca AI** è un gioco da tavolo virtuale a 20 caselle in cui il giocatore umano sfida il tabellone interagendo con **agenti AI specializzati**. Ogni casella attiva una **prova** diversa: barzellette, sfide, quiz su animali, cocktail misteriosi e battaglie Pokémon!

Il gioco è costruito con **Microsoft Agent Framework** e dimostra: Workflow Handoff, Agent-as-a-Tool, Context Windows, GPT Realtime (voce), salvataggio su Cosmos DB e telemetria Aspire.

---

## 🗺️ Il Tabellone

```
START → [1]🐶 → [2]😂 → [3]🐱 → [4]🍹 → [5]🎮 → [6]🎲 → [7]🐶 →
        [8]😂 → [9]🐱 → [10]🍹 → [11]🎮 → [12]🎲 → [13]🐶 →
        [14]😂 → [15]🐱 → [16]🍹 → [17]🎮 → [18]🎲 → [19]🐶 → [20]🏆 FINE
```

Il tabellone ha **20 caselle**, ciascuna con un tipo di prova:

| Simbolo | Tipo | Caselle | Prova |
|---------|------|---------|-------|
| 🐶 | Cane | 1, 7, 13, 19 | Scopri una razza di cane |
| 😂 | Barzelletta | 2, 8, 14 | Ascolta e reagisci a una barzelletta |
| 🐱 | Gatto | 3, 9, 15 | Impara un fatto sui gatti |
| 🍹 | Cocktail | 4, 10, 16 | Scopri un cocktail misterioso |
| 🎮 | Pokémon | 5, 11, 17 | Cattura un Pokémon casuale |
| 🎲 | Sfida Bonus | 6, 12, 18 | Accetta o rifiuta una sfida |
| 🏆 | Traguardo | 20 | Vittoria! |

---

## 🎯 Obiettivo

Raggiungere la **casella 20** (🏆 FINE) per primo. Il giocatore avanza lanciando il dado e affrontando le prove di ogni casella. Bonus e malus possono accelerare o rallentare il percorso.

---

## 🧑 Il Giocatore Umano (Human-in-the-Loop)

Tu sei il giocatore umano! Il gioco **non procede senza di te**. Ecco i tuoi momenti di decisione:

### Momenti di Interazione

| Momento | Cosa fai | Effetto |
|---------|----------|--------|
| **🎲 Lancio del dado** | Scrivi `"lancio"` quando sei pronto | Il dado viene lanciato (1-6) |
| **🐶 Casella Cane** | Rispondi: *"Come lo chiameresti?"* | Interazione personale |
| **😂 Casella Barzelletta** | Rispondi: *"Ti ha fatto ridere? SÌ o NO"* | SÌ → **+1 casella bonus** |
| **🐱 Casella Gatto** | Rispondi: *"Lo sapevi? Hai un gatto?"* | Interazione personale |
| **🍹 Casella Cocktail** | Rispondi: *"Lo ordineresti?"* | Interazione personale |
| **🎮 Casella Pokémon** | Rispondi: *"Come lo chiameresti?"* | Interazione personale |
| **🎲 Casella Sfida** | Rispondi: *"Accetti? SÌ o NO"* | SÌ → **+3 bonus**, NO → **-1 malus** |

> ⏸️ L'agente **si ferma e attende** la tua risposta prima di procedere!

---

## 📋 Le Prove nel Dettaglio

### 🐶 Prova del Cane (caselle 1, 7, 13, 19)

L'**Agente Cane** chiama l'API Dog CEO e ti mostra un cane casuale con la sua razza.

- **Bonus**: Se la razza è **labrador** o **retriever** → **+2 caselle bonus! 🎉**
- **Interazione**: L'agente ti chiede come chiameresti il cane
- **API**: `https://dog.ceo/api/breeds/image/random`

### 😂 Prova della Barzelletta (caselle 2, 8, 14)

L'**Agente Barzellette** chiama l'API delle barzellette e te ne racconta una in modo teatrale.

- **HITL**: *"Ti ha fatto ridere? Rispondi SÌ o NO!"*
- **Bonus**: Se rispondi SÌ → **+1 casella bonus! 😂**
- **API**: `https://official-joke-api.appspot.com/random_joke`

### 🐱 Prova del Gatto (caselle 3, 9, 15)

L'**Agente Gatto** condivide un fatto misterioso sui felini.

- **Bonus**: Se il fatto menziona **"sleep"** o **"hours"** → **Turno extra! 🐈**
- **Interazione**: L'agente ti chiede se lo sapevi e se hai un gatto
- **API**: `https://catfact.ninja/fact`

### 🍹 Prova del Cocktail (caselle 4, 10, 16)

L'**Agente Cocktail** prepara un cocktail casuale con stile da barman.

- **Bonus**: Se il cocktail è **analcolico** → **+1 casella bonus! 🚗 Guida responsabile!**
- **Interazione**: L'agente ti chiede se ti piace e se lo ordineresti
- **API**: `https://www.thecocktaildb.com/api/json/v1/1/random.php`

### 🎮 Prova Pokémon (caselle 5, 11, 17)

L'**Agente Pokémon** cattura un Pokémon della 1ª generazione (1-151).

- **Malus**: Se il tipo è **fire** → **-2 caselle! 🔥 Sei stato bruciato!**
- **Bonus**: Se il tipo è **water** → **+1 casella! 💧 L'acqua ti aiuta!**
- **Interazione**: L'agente ti chiede un soprannome per il Pokémon
- **API**: `https://pokeapi.co/api/v2/pokemon/{id}`

### 🎲 Prova Sfida Bonus (caselle 6, 12, 18)

L'**Agente Sfida** propone un'attività da completare come sfida.

- **HITL**: *"Accetti la sfida? Rispondi SÌ o NO!"*
- **Bonus**: Se accetti → **+3 caselle bonus! 💪**
- **Malus**: Se rifiuti → **-1 casella! 😅**
- **API**: `https://www.boredapi.com/api/activity`

---

## 🔄 Flusso di una Partita

```
1. 🧑 Giocatore dice: "Lancio!"
   │
2. 🎩 Game Master lancia il dado → es. 4
   │
3. 🎩 Game Master: "Hai fatto 4! Casella 🍹 Cocktail!"
   │                [HANDOFF → cocktail-agent]
   │
4. 🍹 Cocktail Agent chiama API → "Mojito"
   │  "Ecco il tuo cocktail: Mojito! È alcolico."
   │  "Ti piace? Lo ordineresti? 🍹"
   │
5. 🧑 Giocatore risponde: "Sì, lo adoro!"
   │
6. 🍹 Cocktail Agent: "Ottimo gusto! Nessun bonus questa volta."
   │                   [HANDOFF → game-master]
   │
7. 🎩 Game Master: "Sei alla casella 4!"
   │  "Vuoi continuare? Scrivi 'lancio'! 🎲"
   │
   └─── Il ciclo ricomincia...
```

---

## 🏆 Condizione di Vittoria

Il gioco termina quando un giocatore raggiunge o supera la **casella 20** (🏆 FINE). Il Game Master annuncia la vittoria con grande entusiasmo! 🎉🏆

Se il punteggio supera 20, il giocatore viene posizionato esattamente alla casella 20.

---

## 🎤 Modalità Voce (GPT Realtime)

Il gioco supporta anche la **modalità voce** tramite **GPT-4o Realtime API**:

- Collegati all'endpoint WebSocket `/realtime`
- Parla al Game Master con la tua voce
- L'agente risponde in audio in tempo reale
- Stesse regole, stessa interattività, ma a voce! 🎙️

---

## 📊 Classifica

In qualsiasi momento puoi vedere la classifica su `GET /game/scoreboard`:

```json
{
  "title": "🏆 Classifica Gioco dell'Oca",
  "players": [
    {
      "rank": 1,
      "name": "Mario",
      "position": 14,
      "turnsPlayed": 5,
      "hasFinished": false,
      "squareType": "joke",
      "isHuman": true
    }
  ]
}
```

---

## 🛠️ Feature Tecniche Dimostrate

| # | Feature | Come è usata nel gioco |
|---|---------|----------------------|
| 1 | **Workflow Handoff** | Game Master → Agente Casella → Game Master |
| 2 | **Context Windows** | `SlidingWindowCompactionStrategy` per gestire la storia della conversazione |
| 3 | **Aspire + OpenTelemetry** | Log e trace di ogni chiamata AI con la dashboard Aspire |
| 4 | **Agent-as-a-Tool** | L'agente Arbitro è esposto come tool via `.AsAIFunction()` |
| 5 | **Cosmos DB** | Salvataggio automatico della cronologia chat con `WithCosmosDBChatHistoryProvider` |
| 6 | **GPT Realtime** | Endpoint WebSocket `/realtime` per interazione vocale |
| 7 | **Human-in-the-Loop** | Il giocatore decide quando lanciare e risponde alle prove |
| 8 | **Function Tools** | 6 API pubbliche + RollDice chiamate come tool dagli agenti |
| 9 | **DevUI** | Interfaccia web interattiva per debug su `/devui` |

---

*Creato per MAF 2026 — Andrea Tosato*
