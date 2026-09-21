# OpenCode project configuration

Keep project-specific OpenCode helpers here only when they are repeatedly useful.

Avoid creating specialized agents or commands before a repeated workflow exists.

The repository-level `AGENTS.md` is the primary orientation file.

Suggested future additions:

```text
.opencode/
├── agents/
│   ├── researcher.md
│   ├── reviewer.md
│   └── tester.md
└── commands/
```

Do not add a specialized agent merely to imitate a human team role. Add one when different permissions, context, or repeated instructions make it materially useful.
