using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.DevUI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient("chat")
    .AddChatClient("chat");

builder.AddAIAgent("writer", "You write short stories (300 words or less) about the specified topic.");

// OpenAI Responses + Conversations + DevUI
builder.AddOpenAIResponses();
builder.AddOpenAIConversations();
builder.AddDevUI();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

app.MapDefaultEndpoints();

app.Run();
