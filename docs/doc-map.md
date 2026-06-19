# Ombor — doc map

What every Ombor doc is, where it lives, which tool reads it, and how the docs relate. This is the index — when unsure which doc owns a decision, start here. Revisable like everything else; update it when a doc is added, retired, or moved.

Physical reality (2026-06-13): docs are copied by hand into each repo's `/docs`. The shared decision docs exist in both repos; keep them in sync manually — when one is edited, update the other copy immediately. Consolidating into one shared home is a deferred convenience, not done yet.

---

## Shared decision docs — both repos

Current best thinking on what we're building and why. Revisable; not frozen. Conflicts resolve rule > plan > brief.

| Doc                 | Purpose                                                                   | Consumed by                                          |
| ------------------- | ------------------------------------------------------------------------- | ---------------------------------------------------- |
| `product-brief.md`  | The "why" — vision, who it's for, positioning, reasoning behind decisions | Chat (planning), Code & Design (context subset)      |
| `business-rules.md` | The "what must hold" — hard rules, domain model, enum reference           | Chat, Code (full), Design (behavior-relevant slices) |
| `mvp-plan.md`       | The "what ships in v1" — module scope and done-ness                       | Chat, Code (current-task slice), Design              |

## Operating-method docs — one per tool

How Claude works in each tool: senior-partner mode, the ratchet, decision discipline, response format. The shared operating layer is identical across tools; each tool's doc adds its specifics.

| Doc                  | Tool            | Notes                                        |
| -------------------- | --------------- | -------------------------------------------- |
| `operating-chat.md`  | Chat            | Planning/challenge rhythm, handoff format    |
| frontend `CLAUDE.md` | Code (frontend) | Operating layer + frontend repo specifics    |
| backend `CLAUDE.md`  | Code (backend)  | Operating layer + backend repo specifics     |
| Design operating doc | Design          | Prototype discipline, no-invented-scope rule |

## Frontend repo — `/docs`

| Doc                             | Purpose                                                                                         |
| ------------------------------- | ----------------------------------------------------------------------------------------------- |
| `CLAUDE.md`                     | Auto-loaded operating contract: pointers, commands, repo map, hard rules, git rules, repo state |
| `conventions.md`                | Frontend craft: module anatomy, MobX/RHF/MUI patterns, i18n, table/label rules                  |
| `mocking.md`                    | MSW contract-first policy; client-side-operations rule; shape-of-truth + page-level staleness   |
| `design-handoff.md`             | Claude Design → React/MUI translation; locked UI patterns; bundle mechanics; fidelity DoD       |
| `openapi.json`                  | The current backend's actual contract — authority on what exists today                          |
| (retired) `tech-change-list.md` | Historical; superseded for backend work by the two contract docs below. Not maintained further  |

## Backend repo — `/docs`

| Doc                           | Purpose                                                                                                                                 | Status                                                                     |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `CLAUDE.md`                   | Auto-loaded operating contract for the backend repo                                                                                     | to write                                                                   |
| `backend-conventions.md`      | Backend craft: layer responsibilities, EF/interceptor/validation patterns, test philosophy                                              | to write                                                                   |
| `backend-contract.md`         | Target API contract the redesigned frontend already calls (derived from frontend models + mocks). Per-resource REAL/STALE/MOCKED status | exists (status labels verified during the audit, not trusted)              |
| `backend-complexity-notes.md` | The non-obvious server-side logic each endpoint hides — WAC engine, source=allocation, balance derivation, order promotion, read models | exists (build-guide; absorbs into conventions/rules as items are verified) |

## Key relationships

- **business-rules wins** over plan and brief on conflict; the contract docs implement the rules — if a contract shape contradicts a rule, the rule wins and the contract is corrected.
- **backend-contract = target, not current.** Its shapes are trustworthy (the frontend already calls them); its REAL/STALE/MOCKED labels are inferences to verify per-endpoint during the audit, not facts.
- **backend-complexity-notes** is the rationale companion to the contract — read together.
- The frontend `openapi.json` is the *current* backend; `backend-contract.md` is the *target* backend. The gap between them is the backend work.
- **Superseded:** May `rules.md`, `claude-context.md`, `project-context.md`, the April `mvp-plan`, and the original `tech-change-list` are all retired — do not reintroduce.

## Open canon edits not yet applied to every copy

(Track here so a stale copy doesn't silently resurface; clear when propagated.)
- Partner reversal (rule 39/42, no system partner) — applied to business-rules; verify product-brief + mvp-plan copies in both repos.
- `Tenant`→`Organization` rename — applied to business-rules; the contract doc's global-conventions still says `Tenant`/`TenantId`, update on next backend-contract touch.