using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace AIGooseGame.Plugins;

/// <summary>
/// Plugin con tutte le chiamate alle API pubbliche (NO auth!) 🌐
/// e il lancio del dado per il Gioco dell'Oca 🎲
/// </summary>
public class PublicApiPlugin(HttpClient httpClient)
{
    private static readonly Random _random = Random.Shared;

    // ─── 🎲 Dado ───────────────────────────────────────────────────────────────

    [Description("Lancia il dado e restituisce un numero da 1 a 6")]
    public int RollDice() => _random.Next(1, 7);

    // ─── 🐶 Dog CEO API ────────────────────────────────────────────────────────

    [Description("Ottieni un'immagine di un cane casuale. Restituisce JSON con breed e imageUrl. Se la razza contiene 'labrador' o 'retriever' il giocatore ottiene +2 caselle bonus!")]
    public async Task<string> GetRandomDog()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://dog.ceo/api/breeds/image/random");
        var imageUrl = response.GetProperty("message").GetString() ?? "";
        var parts = imageUrl.Split('/');
        var breed = parts.Length >= 6 ? parts[^2].Replace("-", " ") : "unknown";

        return JsonSerializer.Serialize(new
        {
            breed,
            imageUrl,
            bonusEligible = breed.Contains("labrador", StringComparison.OrdinalIgnoreCase)
                         || breed.Contains("retriever", StringComparison.OrdinalIgnoreCase)
        });
    }

    // ─── 😂 Official Joke API ──────────────────────────────────────────────────

    [Description("Ottieni una barzelletta casuale. Restituisce JSON con setup e punchline. Se l'utente ride, +1 casella bonus!")]
    public async Task<string> GetJoke()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://official-joke-api.appspot.com/random_joke");
        var setup = response.GetProperty("setup").GetString() ?? "";
        var punchline = response.GetProperty("punchline").GetString() ?? "";

        return JsonSerializer.Serialize(new { setup, punchline });
    }

    // ─── 🐱 Cat Facts API ──────────────────────────────────────────────────────

    [Description("Ottieni un fatto sui gatti. Se il fatto menziona 'sleep' o 'hours', il giocatore ottiene un turno extra!")]
    public async Task<string> GetCatFact()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://catfact.ninja/fact");
        var fact = response.GetProperty("fact").GetString() ?? "";

        return JsonSerializer.Serialize(new
        {
            fact,
            extraTurnEligible = fact.Contains("sleep", StringComparison.OrdinalIgnoreCase)
                             || fact.Contains("hours", StringComparison.OrdinalIgnoreCase)
        });
    }

    // ─── 🍹 CocktailDB API ─────────────────────────────────────────────────────

    [Description("Ottieni un cocktail casuale. Restituisce JSON con nome, se è alcolico, istruzioni e immagine. Se non alcolico → +1 casella bonus (guida responsabile!)")]
    public async Task<string> GetCocktail()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://www.thecocktaildb.com/api/json/v1/1/random.php");
        var drink = response.GetProperty("drinks")[0];

        var name = drink.GetProperty("strDrink").GetString() ?? "";
        var alcoholicStr = drink.GetProperty("strAlcoholic").GetString() ?? "";
        var isAlcoholic = !alcoholicStr.Equals("Non alcoholic", StringComparison.OrdinalIgnoreCase);
        var instructions = drink.GetProperty("strInstructions").GetString() ?? "";
        var image = drink.GetProperty("strDrinkThumb").GetString() ?? "";

        return JsonSerializer.Serialize(new
        {
            name,
            isAlcoholic,
            instructions = instructions.Length > 200 ? instructions[..200] + "..." : instructions,
            imageUrl = image,
            bonusEligible = !isAlcoholic
        });
    }

    // ─── 🎮 PokéAPI ────────────────────────────────────────────────────────────

    [Description("Cattura un Pokémon casuale (ID 1-151). Restituisce JSON con nome, tipi e sprite. Se tipo 'fire' → -2 caselle (bruciato! 🔥). Se tipo 'water' → +1 casella 💧")]
    public async Task<string> GetPokemon()
    {
        var id = _random.Next(1, 152);
        var response = await httpClient.GetFromJsonAsync<JsonElement>($"https://pokeapi.co/api/v2/pokemon/{id}");

        var name = response.GetProperty("name").GetString() ?? "";
        var types = response.GetProperty("types")
            .EnumerateArray()
            .Select(t => t.GetProperty("type").GetProperty("name").GetString() ?? "")
            .ToArray();
        var sprite = response.GetProperty("sprites").GetProperty("front_default").GetString() ?? "";

        var bonusModifier = 0;
        if (types.Contains("fire")) bonusModifier = -2;
        else if (types.Contains("water")) bonusModifier = 1;

        return JsonSerializer.Serialize(new
        {
            name,
            types,
            spriteUrl = sprite,
            bonusModifier
        });
    }

    // ─── 🎲 Bored API ──────────────────────────────────────────────────────────

    [Description("Proponi una sfida bonus dal Bored API. Se il giocatore accetta → +3 caselle. Se rifiuta → -1 casella.")]
    public async Task<string> GetBonusActivity()
    {
        var response = await httpClient.GetFromJsonAsync<JsonElement>("https://www.boredapi.com/api/activity");
        var activity = response.GetProperty("activity").GetString() ?? "";
        var type = response.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

        return JsonSerializer.Serialize(new
        {
            activity,
            type,
            bonusIfAccepted = 3,
            penaltyIfRefused = -1
        });
    }
}
