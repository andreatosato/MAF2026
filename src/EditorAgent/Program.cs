using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient("chat")
    .AddChatClient("chat");

builder.AddAIAgent("editor", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return new ChatClientAgent(
        chatClient,
        name: key,
        instructions: "You edit short stories to improve grammar and style, ensuring the stories are less than 300 words. Once finished editing, you select a title and format the story for publishing.",
        tools: [AIFunctionFactory.Create(FormatStory)]
    );
});

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

[Description("Formats the story for publication, revealing its title.")]
static string FormatStory(string title, string story) => $"""
    **Title**: {title}

    {story}
    """;
