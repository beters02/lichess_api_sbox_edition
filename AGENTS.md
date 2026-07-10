# Agent Instructions

<!-- lean-ctx-rules-condensed -->

## lean-ctx
lean-ctx is active — the MCP tools replace native equivalents.

# `ctx_*` Tool Operating Contract

Use `ctx_*` as primary layer for exploration, composition, search, compressed reads, exact edits, validation, and context discipline. Prefer live MCP schemas for exact arguments; this file defines workflow/evidence/risk/fallback.

## Understand Tool Execution Economy
Batch independent read-only calls; sequence only on dependency or mutation. Prefer direct `ctx_*`; if hidden use `ctx_call`: `{"tool":"ctx_call","arguments":{"name":"ctx_compose","arguments":{"task":"semantic task + keywords/symbols","path":"."}}}`. Start unfamiliar tasks with `ctx_compose`; use it to choose targets, not final proof. Confirm exact targets before edits/final claims.

## Gather Repository Context
Orientation: `ctx_compose`, `ctx_overview`, `ctx_tree`, `ctx_semantic_search`, compressed `ctx_read`. Candidate: `ctx_search`, exact matches, related files, compressed shell. Strong: `ctx_symbol`, `ctx_read lines:N-M/full/diff`, targeted validation. Verification: `ctx_shell` tests/build/lint; `raw:true` only if compression hides needed detail.

## Follow Standard Tool Execution Workflow
`ctx_compose` unfamiliar task; `ctx_tree` shape; `ctx_search` exact regex/text; `ctx_semantic_search` conceptual search; `ctx_symbol` known body; `ctx_read full` edit target; `ctx_read lines:N-M` exact region; `ctx_read diff` post-edit; `ctx_multi_read` confirmed batch; `ctx_edit` simple exact edit/create; `ctx_shell` validation/debug. Escape regex metacharacters for literal search.

## Classify and Manage Tool Output Information
Internally classify after compose/search/semantic search. `KEEP`: exact error/route/config/symbol/test/path, stack trace, inlined symbol, import/export, edit target/caller/callee/failing test. `MAYBE`: semantic relation/no exact proof, related file, plausible utility/config/schema/migration/fixture, stale finding. `DISCARD`: generated/vendor/cache/binary/duplicate, unrelated name, docs-only when code needed, unrelated fixture. Read KEEP; promote MAYBE only after exact evidence.

## Assess Risk of Mutating Tools
Risky if delete/move/rename/overwrite, `replace_all`, 3+ files, >150 LOC, public/exported/API/DB/auth/payments/secrets/crypto/routing/CI/package/lock/build/deploy/data-loss, shared middleware/global/cache/concurrency/init/serialization/error handling, no runnable validation, generated/vendor/dependency, weakened tests, stale/compressed-only evidence. Before risky edits collect impact/search/full-read; for exported/widely used symbols add `ctx_callgraph risk|callers` or `ctx_refactor references`. Edit only after target text + blast radius. Use `ctx_edit` for exact replacements/create; `replace_all` only after search proves all occurrences. After edits: `ctx_read diff`, narrow validation, raw/narrower only if evidence hidden, report unresolved failures.

## Utilise High ROI Tools
Use strategically: `ctx_impact/ctx_graph/ctx_callgraph` blast radius; `ctx_refactor` LSP symbols; `ctx_architecture` layers/cycles/hotspots; `ctx_control` hygiene; `ctx_compress/ctx_plan/ctx_compile/ctx_pack` context/PR; `ctx_review/ctx_smells/ctx_analyze/ctx_session/ctx_task/ctx_knowledge` review/memory. Compression default; bypass only for missing evidence via `lines:N-M` → `full` → `ctx_retrieve` → `ctx_shell raw:true`; return to compressed defaults; do not bypass with `cat/sed/grep/rg/find/scripts`. Native fallback only when `ctx_*` unavailable/hidden+no wrapper, output insufficient, target unsupported, stronger transaction/refactor needed, or validation must run outside `ctx_*`.

## Plan Mode
When finishing or making a revision on a plan, always give condensed instructions on how to test.

<!-- /lean-ctx-rules-condensed -->