# Docs Slice Design — Reader Journeys and Doc/Code Coupling

## Context

The v1 milestone in [`docs/PROJECT.md`](../../PROJECT.md) defines Phase 6 as a docs and guides pass. Phases 0–5 are complete; Phase 5 (examples parity) closed out on 2026-09-25. This document scopes Phase 6 as a problem space. The phase ordering, its position between examples parity and the Vite asset-bundling spike, and every other phase's scope are unchanged in `PROJECT.md`.

Verified repository state on 2026-09-26, branch `canary`:

- `docs/guides/` holds **14 files**. **8 are wholly placeholder** — a title plus a `> [!WARNING]` work-in-progress notice and nothing else: `client-entry`, `component-register`, `configuration`, `phoria-islands`, `phoria-server`, `phoria-web-app`, `server-entry`, `workspaces`. `supported-ui-frameworks.md` is a ninth file carrying placeholder notices, but has a real introduction and framework index above them.
- **5 files hold real content**, ~52KB total: `getting-started` (24KB), `deployment` (11.5KB), `creating-phoria-island-components` (7.7KB), `building-for-production` (6.4KB), `directives` (2KB).
- **10 documentation locations instruct a command that fails today.** All nine `examples/*/README.md` files and `examples/README.md` tell the reader to run `pnpx giget gh:cmeeg/phoria/examples/<name>`, which resolves the repository's default branch — `main`. `examples/` does not exist on `main`. The path resolves only on `canary` via an explicit `#canary` suffix, which no committed document currently mentions.
- The README's guides index links only the 5 real guides. The 9 placeholder-bearing files are therefore **orphaned** — unreachable from the documentation entry point, while the directory listing presents them as the framework's conceptual vocabulary.
- `canary` is **19 commits ahead of `main`**, and `main` carries no `examples/` directory at all.
- `gh` is not available in the working environment, so the parked `canary` → `main` pull request (#36 per `docs/MEMORY.md`) can be neither inspected nor actioned from here.

## Problem

Three problems, in descending order of how much they cost a reader.

**A reader cannot get from "what is this" to "a working island" using the repository's documentation.** The information that exists is split between a 24KB `getting-started` guide, a `creating-phoria-island-components` guide, and eight empty files named after Phoria's own concepts. Nothing in the guides is organised around what the reader is trying to do, so a reader must already know that `component-register` is the thing they need in order to look for it — and it is empty when they arrive.

**Ten documented commands are wrong.** This is not hypothetical: the documented `giget` invocation 404s for anyone who follows it, today, on either branch.

**The existing review convention has never fired.** `CONTRIBUTING.md` already asks contributors to review the README and guides when a change alters how Phoria works, and to keep `ARCHITECTURE.md` consistent with the code. The v1 milestone then added six phases of work — a test foundation, a server-robustness programme, dual-runtime OpenTelemetry, a canary release pipeline, a component-path wire-format change, and seven new examples — and the documentation drifted through all of it, caught afterwards by inspection. The convention is passive, has no trigger, and has no owner at the point where work is actually planned. Restating it would change nothing; giving it a trigger might.

The framing matters: **this slice is not "fill nine placeholders."** Completing the placeholder count is the cheapest available version of this work and leaves the rot mechanism — the thing that recreates the first two problems within a month — exactly as it is.

## Who it's for

Three audiences, in the order they are served. They are served by different media, and the separation is deliberate rather than incidental.

1. **Evaluating and trying it out.** Someone who has landed on the repository from a search or an announcement and wants to know what Phoria is and get something running. Served by `README.md` and the front of `docs/guides/`. Their cost of abandonment is highest here and their patience is lowest, so this path is the priority.
2. **Digging deeper.** Someone who likes the idea and now needs to configure it: component structure, entries, the client/server split, configuration keys, supported frameworks, deployment, production builds. Served by the body of `docs/guides/`.
3. **Contributing.** Someone who needs to know how the thing works before they can change it. Served by [`docs/ARCHITECTURE.md`](../../ARCHITECTURE.md) (the canonical runtime model) and [`AGENTS.md`](../../../AGENTS.md) (how to work in this repository). Both already exist and are substantial; this slice brings them back into agreement with the code rather than rewriting them.

## Goals

- Reorganise the guides around what a reader is trying to do, **without losing the depth already written.** The five substantial guides are the accumulated work of six phases and are an asset, not clutter to be dissolved into a new structure.
- Close the placeholder gap along the getting-started and first-island path. Deeper and reference material may still carry placeholders at the end of this slice.
- Make every non-placeholder document accurate, with every example and command correct against the current branch.
- Give the documentation-review convention a **trigger**: a required step in task definitions, recorded in `CONTRIBUTING.md`, `AGENTS.md`, and the project's plan documents.
- Bring `ARCHITECTURE.md` back into agreement with the shipped code.

## Non-goals

Confirmed out of scope for this slice:

- **A docs website.** Already deferred post-1.0 in `PROJECT.md`; a Phoria app on Render remains the eventual candidate.
- **Automated documentation checks.** No link checker, no referenced-symbol existence check, no code-block runner. The mechanism is a review convention, by choice, and this slice does not build tooling to backstop it.
- **The HTTPS-in-Preview decision.** The http-versus-https trade-off for the Phoria Server is research and belongs to the DX and tooling phase, as `PROJECT.md` already records.
- **Phase 7 (Vite bundling of .NET-referenced static assets) and Phase 10 (new examples, examples scope triage).** Untouched here.
- **Rewriting `ARCHITECTURE.md`.** It is brought back into agreement; its structure and depth are not re-litigated.

## Branch baseline

The current branch is treated as `main`. This slice writes documentation as though `canary` were already `main`, because the `canary` → `main` cut is the final step of the task and its content becomes `main` at that point.

Two consequences follow, and both are deliberate:

- **Branch-sensitive references are written unqualified**, in the form that will be correct on `main` — the plain `gh:CMeeg/phoria/examples/<name>` ref with no suffix. Where a reader on `canary` genuinely needs different behaviour, the `#canary` suffix is given as an aside rather than as the default. This is the form that stops being wrong the moment the cut lands, and writing it any other way guarantees a second correction pass.
- **Those references cannot be verified before the cut.** An unqualified `giget` ref is only resolvable once `examples/` is on the default branch, so verification of all ten locations is necessarily a post-cut step, not a pre-merge one. This is recorded as an open question rather than glossed over.

## Depth boundary

Guides may explain the concepts a user needs in order to configure Phoria correctly: what an island is, how hydration works, when to choose CSR over SSR, why there is a client entry and a server entry, that a Node process serves the interactive parts and that it therefore has to be reachable and supervised.

Guides do not explain runtime internals: process and module boundaries, telemetry wiring, buffer and stream lifetimes, Vite or Rolldown build specifics, the tag-helper and manifest layers. Those belong to `ARCHITECTURE.md`.

The promotion rule, for when a guide needs to say more than the boundary allows: if a reader cannot make a correct configuration decision without the detail, the detail is conceptual and stays in the guide; if the detail describes how Phoria is built rather than how it is used, it moves to `ARCHITECTURE.md`.

## Success

- The getting-started and initial-journey path is complete and accurate. This is the priority; deeper guides and reference material may still contain placeholders when the slice lands.
- Every non-placeholder document is accurate, and every example and command in them is correct against the current branch.
- The documentation-review step exists in `CONTRIBUTING.md`, in `AGENTS.md`, and in the project's plan documents.
- `ARCHITECTURE.md` is consistent with the shipped code.

## Stop conditions

Agreed during scoping. Any of these is grounds to halt or re-scope rather than push through:

- **It does not unblock 1.0.0.** Time spent here should visibly shorten the path to a shippable release. If it does not, it defers.
- **The documentation rots back within a month.** If the same drift reappears quickly, this slice was cosmetic and the real problem is that documentation is not coupled to code — which is a different, larger problem than the one scoped here.
- **It destabilises `canary`.** If branch health, the release pipeline, or example parity regress while this work is in flight, it halts.

## Open questions (TODO)

- TODO: the file-by-file mapping of the 14 guides onto the three audiences, and which of the 9 placeholder-bearing files sit on the first-island path (must be written) versus deeper material (may stay placeholder). This is the first thing `/spec` should settle.
- TODO: the form the documentation-review step takes in a plan document, and whether the existing plan documents under `docs/plans/` and `docs/superpowers/plans/` are retrofitted or only new plans are affected.
- TODO: whether the getting-started path is verified by actually following it end to end, which requires a working local environment (Node v24.18.0, pnpm 11.17.0, .NET SDK 10.0.302, Aspire CLI 13.4.6) and is a materially larger claim than a read-through.
- TODO: whether a reader needs a standalone prerequisites or requirements page, separate from `getting-started`, or whether that content belongs at its front.
- TODO: the post-cut verification pass for the ten `giget` references, and who performs it.
- TODO: how the review step is honoured on the `canary` → `main` cut itself, which is a fast-moving merge rather than ordinary feature work.

## Riskiest unknowns

- **The mechanism is unenforced and its efficacy is unproven.** The convention already exists and demonstrably did not prevent six phases of drift. Giving it a trigger is a better bet than restating it, but nothing in this slice will tell us whether it worked — the honest position is that this is unknown until the next phase's work has been done for a while. Review it at the next phase boundary rather than treating it as settled.
- **Restructuring fourteen files around a new axis risks losing or duplicating the content that is already good.** The failure mode is precisely the one the brief rules out: a structure in which `getting-started`, `creating-phoria-island-components`, and `directives` overlap and the reader is unsure which to read. The mitigating constraint is that reorganisation must be additive at the entry-point level — reorder and add, do not delete and rewrite.
- **Writing documentation for a state that does not exist yet.** Bounded to the length of the task, and accepted deliberately, but it means the slice cannot be fully verified within itself.

## Findings

Drift noticed while scoping this slice. Recorded, not fixed here — each belongs to a later phase or to the implementation of this one.

- `AGENTS.md` states that peer ranges "reconcile to `^1.0.0` when all packages reach `1.0.0` (Phase 10)". `PROJECT.md` places release prep at Phase 11.
- `docs/MEMORY.md` records the `giget` ref as `gh:CMeeg/phoria`; all nine example READMEs use `gh:cmeeg/phoria`. The repository is `CMeeg/phoria` and GitHub resolves the owner and repository case-insensitively, so this is cosmetic — but the documents should agree, and the repository's own casing is the one to standardise on.
- The `#`-versus-`@` ref-syntax note (`@` is npm dist-tag syntax, `#` is a git ref) is recorded in `MEMORY.md` for agents but appears in no document a reader would consult, where it is exactly the kind of detail that produces a 404.
- The README's guides index is honest — it links only the 5 real guides. Preserve that property: if placeholders are re-linked as part of the restructure, they must be written first.
