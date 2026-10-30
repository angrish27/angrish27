# Repository Agent Guidelines

Before making changes, read [AgentUrlShortner/docs/00-master-context.md](AgentUrlShortner/docs/00-master-context.md), [AgentUrlShortner/docs/agent-instructions.md](AgentUrlShortner/docs/agent-instructions.md), and the docs relevant to the task.

For code changes, follow this workflow:

1. **Plan:** inspect the affected code and state the intended change and validation.
2. **Implement:** make the smallest complete change, preserving the IDesign layer boundaries.
3. **Test:** run the relevant tests and build; fix failures caused by the change.
4. **Report:** summarize changes, commands and results, and any remaining risks.
5. **Human review:** do not merge or represent changes as approved until a human has reviewed and approved them.

Use the repository's existing patterns and update directly related documentation. See [AgentUrlShortner/docs/testing.md](AgentUrlShortner/docs/testing.md) for test conventions and [AgentUrlShortner/docs/agentic-development-example.md](AgentUrlShortner/docs/agentic-development-example.md) for an example report.
