# ONE DROP - Agent Entry

Primary active governance for this repository lives in the files below.

Read order for every task:
1. `Overview.md` (Quick-reference for the multi-agent system)
2. `CurrentState.md`
3. `TaskBoard.md`
4. latest relevant lines in `ChangeLog.md`
5. `Workflow.md`

Read when needed:
6. `Architecture.md`
7. `Design.md`

Rules:
- `Workflow.md` is the only active process-rules document.
- `CurrentState.md` is accepted merged truth only.
- `TaskBoard.md` is open work only.
- `ChangeLog.md` is append-only outcome history and proof.
- `Architecture.md` is the active ownership map and technical architecture reference.
- `Design.md` is gameplay and design intent only.
- `.agent/legacy/` is frozen reference only. Do not edit archived files.

`.agent` is the shared memory across chats.

Fast bootstrap for any new chat:
- check `TaskBoard.md` for the task row and state
- check `ChangeLog.md` for the latest handoff or review decision on that `TaskID`
- check `Workflow.md` for the default chat-role model, transition markers, `TaskID` rule, and launch contract
- resume from `.agent` first instead of relying on copied chat context

Default chat model:
- main chat = `Planner` by default
- main chat may also act as `Reviewer`
- spawned chat = `Worker` by default
- separate review chat is optional and must be explicit
- one chat may switch roles only by stating the new transition marker
