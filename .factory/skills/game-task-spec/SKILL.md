---
name: game-task-spec
description: Generate structured JSON task specification documents for game development. Use this skill whenever the user describes a game feature, system, mechanic, or any game-related programming task they want an AI agent to execute. Also trigger when the user wants to create, review, or manage game dev task specs, or says things like "create a task", "write a spec", "generate requirement doc", or describes a game programming need in natural language.
---

# Game Task Spec Generator

Transform natural language game development requests into concise, complete JSON task specifications that any AI coding agent can execute without ambiguity.

## Core Principle

The goal is maximum information density: every field in the JSON should earn its place. An agent reading this spec should be able to start coding immediately with zero follow-up questions about *what* to build. The spec captures intent, constraints, and acceptance criteria — not implementation details (that's the agent's job).

## Workflow

### Phase 1: Intake & Clarify

1. Parse the user's natural language description of the task.
2. Identify what's clearly stated vs. what's missing or ambiguous.
3. For anything uncertain, ask the user to confirm. Group questions logically — don't bombard with one question at a time. Use the AskUser tool to present 1-4 focused questions at once.
4. Use your game development knowledge to fill in reasonable defaults and make smart inferences:
   - If the user mentions "inventory system", infer common patterns (slot-based, stackable items, drag-and-drop, persistence)
   - If they mention "health bar", infer UI layering, damage types, regeneration
   - If they mention a game genre, apply genre-specific conventions
5. Present your inferences to the user for confirmation before finalizing.

### Phase 2: Generate JSON Spec

Produce a JSON document following the schema below. Keep it lean — no fluff, no filler fields, no generic descriptions. Every value should carry actionable information.

#### JSON Schema

```json
{
  "task_name": "string — short, imperative name (e.g., 'Implement Turn-Based Combat System')",
  "genre": "string — game genre if relevant (e.g., 'RPG', 'FPS', 'Roguelike', null)",
  "engine": "string — target engine/framework if specified (e.g., 'Unity', 'Godot', 'Unreal', 'Web/Canvas', null)",
  "language": "string — primary programming language (e.g., 'C#', 'GDScript', 'TypeScript')",
  "summary": "string — 1-3 sentences capturing the core intent. What does this do and why?",
  "scope": {
    "in_scope": ["string — what this task includes"],
    "out_of_scope": ["string — what this task explicitly does NOT include"]
  },
  "requirements": [
    {
      "id": "string — e.g., 'R1'",
      "description": "string — the functional requirement",
      "priority": "MUST | SHOULD | COULD",
      "acceptance": "string — how to verify this is done correctly"
    }
  ],
  "architecture": {
    "pattern": "string — architectural approach (e.g., 'ECS', 'Component-Based', 'State Machine', 'Event-Driven')",
    "key_types": [
      {
        "name": "string — type/class name",
        "responsibility": "string — what it does",
        "members": "string — key fields/methods (brief, not full signatures)"
      }
    ],
    "data_flow": "string — how data moves through the system (1-3 sentences)"
  },
  "constraints": {
    "performance": "string — FPS targets, memory limits, or null",
    "platform": "string — target platform(s) or null",
    "dependencies": ["string — external libs, APIs, or other systems this relies on"],
    "compatibility": "string — backward compat requirements or null"
  },
  "file_structure": {
    "create": ["string — files to create with brief purpose"],
    "modify": ["string — existing files to modify with what changes"]
  },
  "test_plan": {
    "unit": ["string — key unit test scenarios"],
    "integration": ["string — integration test scenarios"],
    "manual": ["string — manual verification steps if any"]
  },
  "notes": "string — any gotchas, edge cases, or important context not covered above. Empty string if none."
}
```

#### Rules for each field

- **task_name**: Imperative verb phrase. No vague titles like "System".
- **summary**: Write this for an AI, not a human. Be precise about behavior, not aspirational.
- **scope**: Explicitly list what's out of scope to prevent scope creep during execution.
- **requirements**: Each requirement should be independently testable. Use acceptance criteria that are binary (pass/fail), not subjective ("looks good").
- **architecture**: Only include what's needed to guide implementation. Don't design the full class hierarchy — give the agent enough to make good decisions, not enough to be constrained. `key_types` should list 3-8 types max. If the architecture is straightforward, simplify.
- **file_structure**: Only include files you're confident about. Use wildcards if the agent should decide specifics (e.g., `"UI/HealthBar.*"`).
- **test_plan**: Focus on the tricky parts. Don't list obvious tests.
- **notes**: This is the catch-all for things that don't fit elsewhere. Use it for game-specific context (e.g., "This system must integrate with the existing save/load system which uses JSON serialization").

### Phase 3: Present & Decide

After generating the JSON, present it to the user and offer two options:

**Option A — Execute immediately**: Hand the JSON spec to an AI agent to start implementation. The agent should read the spec and begin coding.

**Option B — Save for later**: Save the JSON document to the persistent storage directory at `~/.game-task-specs/`. Create this directory if it doesn't exist. File naming: `{task_name-kebab-case}.json`. Confirm the save location to the user.

Ask the user which option they prefer. If they choose A, begin execution. If they choose B, save and confirm.

## Important Guidelines

- Think like a game developer, not a generic software engineer. Use game dev terminology precisely (e.g., "tick", "frame", "hitbox", "aggro radius", "state machine", "ECS").
- Infer game-specific details from context. If someone says "add a dash mechanic", infer: cooldown, distance, invincibility frames, animation triggers, input buffering.
- Keep the JSON under 150 lines whenever possible. Density over verbosity.
- If the user provides code snippets, file paths, or references to existing code, incorporate them into the spec's constraints and file_structure fields.
- For multi-part tasks, produce one spec per logical unit. Ask the user if they want to break it down.
- The spec is a contract between the human and the AI agent. It should be complete enough that the agent never needs to ask "what did the user mean by X?".
