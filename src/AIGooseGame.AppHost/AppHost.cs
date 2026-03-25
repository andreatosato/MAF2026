var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AIGooseGame>("aigoosegame");

builder.Build().Run();
