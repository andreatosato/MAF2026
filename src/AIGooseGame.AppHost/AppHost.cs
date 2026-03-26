using Aspire.Hosting.Azure;

#pragma warning disable ASPIRECOSMOSDB001 // RunAsPreviewEmulator è in preview
#pragma warning disable ASPIREAIFDRY001   // AI Foundry è in preview

var builder = DistributedApplication.CreateBuilder(args);

// Azure AI Foundry — modello gpt-4o-mini
var aiFoundry = builder.AddAzureAIFoundry("goosegame");
var chatDeployment = aiFoundry.AddDeployment("chat", AIFoundryModel.OpenAI.Gpt52Chat);

// Azure Cosmos DB — emulatore in locale, Cosmos su Azure in produzione
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator();
var cosmosDb = cosmos.AddCosmosDatabase("GooseGameDB");

builder.AddProject<Projects.AIGooseGame>("aigoosegame")
    .WithReference(chatDeployment)
    .WithReference(cosmosDb)
    .WaitFor(cosmos);

builder.Build().Run();
