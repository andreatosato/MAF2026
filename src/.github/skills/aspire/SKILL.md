---
name: aspire
description: "Orchestrates Aspire distributed applications using the Aspire CLI for running, debugging, and managing distributed apps. USE FOR: aspire start, aspire stop, start aspire app, aspire describe, list aspire integrations, debug aspire issues, view aspire logs, add aspire resource, aspire dashboard, update aspire apphost. DO NOT USE FOR: non-Aspire .NET apps (use dotnet CLI), container-only deployments (use docker/podman), Azure deployment after local testing (use azure-deploy skill). INVOKES: Aspire CLI commands (aspire start, aspire describe, aspire otel logs, aspire docs search, aspire add), bash. FOR SINGLE OPERATIONS: Use Aspire CLI commands directly for quick resource status or doc lookups."
---

# Aspire Skill

This repository uses Aspire to orchestrate its distributed application. Resources are defined in the AppHost project (`AIGooseGame.AppHost/AppHost.cs`).

## Resources in this project

| Resource | Type | Description |
|---|---|---|
| `ai` | Azure AI Foundry | LLM models (gpt-4o-mini, gpt-4o-realtime-preview) |
| `chat` | AI Foundry Deployment | gpt-4o-mini deployment |
| `realtime` | AI Foundry Deployment | gpt-4o-realtime-preview deployment |
| `cosmos` | Azure Cosmos DB | Database (emulator locally, Azure in prod) |
| `GooseGameDB` | Cosmos Database | Game data and chat history |
| `aigoosegame` | Project | Main game application |

## CLI command reference

| Task | Command |
|---|---|
| Start the app | `aspire start` |
| Start isolated (worktrees) | `aspire start --isolated` |
| Restart the app | `aspire start` (stops previous automatically) |
| Wait for resource healthy | `aspire wait <resource>` |
| Stop the app | `aspire stop` |
| List resources | `aspire describe` or `aspire resources` |
| Run resource command | `aspire resource <resource> <command>` |
| Start/stop/restart resource | `aspire resource <resource> start|stop|restart` |
| View console logs | `aspire logs [resource]` |
| View structured logs | `aspire otel logs [resource]` |
| View traces | `aspire otel traces [resource]` |
| Logs for a trace | `aspire otel logs --trace-id <id>` |
| Add an integration | `aspire add` |
| List running AppHosts | `aspire ps` |
| Update AppHost packages | `aspire update` |
| Search docs | `aspire docs search <query>` |
| Get doc page | `aspire docs get <slug>` |
| List doc pages | `aspire docs list` |
| Environment diagnostics | `aspire doctor` |
| List resource MCP tools | `aspire mcp tools` |
| Call resource MCP tool | `aspire mcp call <resource> <tool> --input <json>` |

Most commands support `--format Json` for machine-readable output. Use `--apphost <path>` to target a specific AppHost.

## Key workflows

### Running in agent environments

Use `aspire start` to run the AppHost in the background. When working in a git worktree, use `--isolated` to avoid port conflicts:

```bash
aspire start --isolated
```

Use `aspire wait <resource>` to block until a resource is healthy before interacting with it:

```bash
aspire start --isolated
aspire wait aigoosegame
```

Relaunching is safe — `aspire start` automatically stops any previous instance. Re-run `aspire start` whenever changes are made to the AppHost project.

### Debugging issues

Before making code changes, inspect the app state:

1. `aspire describe` — check resource status
2. `aspire otel logs aigoosegame` — view structured logs
3. `aspire logs aigoosegame` — view console output
4. `aspire otel traces aigoosegame` — view distributed traces

### Adding integrations

Use `aspire docs search` to find integration documentation, then `aspire docs get` to read the full guide. Use `aspire add` to add the integration package to the AppHost.

After adding an integration, restart the app with `aspire start` for the new resource to take effect.

## Important rules

- **Always start the app first** (`aspire start`) before making changes to verify the starting state.
- **To restart, just run `aspire start` again** — it automatically stops the previous instance. NEVER use `aspire stop` then `aspire run`. NEVER use `aspire run` at all.
- Use `--isolated` when working in a worktree.
- **Avoid persistent containers** early in development to prevent state management issues.
- **Never install the Aspire workload** — it is obsolete.
- Prefer `aspire.dev` and `learn.microsoft.com/dotnet/aspire` for official documentation.
- The Cosmos DB emulator requires Docker running locally.
