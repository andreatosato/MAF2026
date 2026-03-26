using Aspire.Hosting.Foundry;

#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator è in preview
#pragma warning disable ASPIREFOUNDRY001  // Foundry è in preview

var builder = DistributedApplication.CreateBuilder(args);

// Microsoft Foundry — modello gpt-4o-mini
var foundry = builder.AddFoundry("goosegame");
var chatDeployment = foundry.AddDeployment("chat", FoundryModel.OpenAI.Gpt52Chat);

// Azure Cosmos DB — emulatore in locale, Cosmos su Azure in produzione
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator();
var cosmosDb = cosmos.AddCosmosDatabase("GooseGameDB");

builder.AddProject<Projects.AIGooseGame>("aigoosegame")
    .WithReference(chatDeployment)
    .WithReference(cosmosDb)
    .WaitFor(cosmos);

builder.Build().Run();
