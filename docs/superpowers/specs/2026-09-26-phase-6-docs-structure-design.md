# Docs Structure Design — Extraction, Trialist Path, and a Docs/Code Trigger

## Context

This is the design companion to [`2026-09-26-phase-6-docs-journey-design.md`](2026-09-26-phase-6-docs-journey-design.md), which scoped Phase 6 as a problem space. That document establishes the problem, the three audiences, the success criteria and the stop conditions; it is not restated here. This document decides *how*.

The problem-space scoping produced a finding that changes the shape of the work. The reader-journey structure the slice calls for **already exists**: `docs/guides/getting-started.md` is already an ordered path (clone an example → prerequisites → add Vite → add a UI component → register it → add Phoria Server → Client Entry → Server Entry → add Phoria to the .NET app → Tag Helpers → render an Island → run → preview), and the eight placeholder guides are named after exactly the concepts that path walks through.

Worse, and more useful: **seven links into those empty files are already live.** Both real guides route readers into a warning box mid-instruction.

| Link site | Points at |
| --- | --- |
| `getting-started.md:187`, `creating-phoria-island-components.md:55` | `component-register.md` |
| `getting-started.md:204` (twice, in one aside) | `configuration.md`, `workspaces.md` |
| `getting-started.md:389` | `client-entry.md` |
| `getting-started.md:411` | `server-entry.md` |
| `getting-started.md:434` | `phoria-web-app.md` |

`getting-started.md:434` is the sharpest case: the phrase "we will refer to it as the [Phoria Web App](./phoria-web-app.md)" *defines* the term by linking to an empty file.

So the slice is an **extraction, not an invention**. The prose that belongs in most of those files already exists in `getting-started.md` — line 389 already explains what a Client Entry is and then links away for more. This collapses the cost, and it means the completeness problem and the rot problem have the same fix.

## Goals

- Move each concept's explanation into the guide the reader is already being sent to, so every live link resolves to real content and no explanation has two homes.
- Give the trialist audience a real first-run path instead of a seven-line pointer.
- Give the documentation-review convention a trigger, by making a named docs-review task mandatory in every plan document.
- Bring `README.md`, the package READMEs and the example READMEs up to a documented, uniform shape.
- Document the framework-extension seam in the package that owns it, so adding a framework package is a recipe rather than an archaeology exercise.
- Bring `ARCHITECTURE.md` back into agreement with the shipped code.

## Non-goals

Unchanged from the problem-space document:

- **A docs website.** Deferred post-1.0.
- **Automated documentation checks.** No link checker, no referenced-symbol check, no code-block runner. The coupling mechanism is a review convention with no tooling backstop, by choice.
- **The HTTPS-in-Preview decision.** Research, and it belongs to the DX and tooling phase.
- **Phase 7 (Vite asset bundling) and Phase 10 (new examples, examples scope triage).**
- **Rewriting `ARCHITECTURE.md`.** It gains a link to the framework recipe and the rest of its drift pass; its structure and depth are not re-litigated.
- **Code changes to the examples.** All nine examples already carry a `docker-compose.yml`, a `Dockerfile` and a `.dockerignore` on port 8080. Nothing needs building; this slice writes no product code.

## The seam, and the test that holds it

**The rule:** prose explaining *why a Phoria concept works this way* has exactly one home — the concept guide. The journey page keeps the ordered steps, the code the reader pastes, and a one-line summary, and links to the concept guide for the explanation.

**The seam test:** for any given paragraph, does it explain *this concept*, or *this step in this order*? Step-in-order stays in the journey. Concept moves to the concept guide. The test is deliberately mechanical so that the extraction can be reviewed paragraph by paragraph without a judgement call each time — which is what keeps the next change to this structure small and local.

## Concept guide mapping

Nine files carry placeholder warnings. This is where their content comes from.

| Guide | Source of the prose | Note |
| --- | --- | --- |
| `component-register` | `getting-started.md:187` plus `creating-phoria-island-components.md:53-88` | Two existing copies already exist; the guide becomes canonical and the other two link to it |
| `client-entry` | `getting-started.md:387-408` | Explains the entry, then links away; the explanation stays, the link stops being a dead end |
| `server-entry` | `getting-started.md:409-431` | |
| `phoria-server` | `getting-started.md:206-386` | Largest block at 180 lines: what the server is, supervision, health, degradation |
| `phoria-web-app` | `getting-started.md:432-493` | The term is currently defined by a link to this empty file; the definition moves here and the journey links to it |
| `phoria-islands` | New prose, see below | Concept register — decided, see below |
| `configuration` | `getting-started.md:455` plus the configuration surface | **Expected to remain incomplete.** A complete configuration reference is a lookup table; extraction only carries what the journey happens to touch |
| `workspaces` | `getting-started.md:204` plus the `with-workspace` example | Same expectation, same reason |
| `supported-ui-frameworks` | The three framework packages and their examples | Highest effort and lowest priority for the first-run audience. **Expected to remain incomplete** when the slice lands, under the problem-space document's priority rule |

**`phoria-islands` takes the concept register, keeping three files with distinct registers.** It overlaps `creating-phoria-island-components` (a how-to) and `directives` (a reference) across one concept area, and the decision is to give it the concept — what an island is, how it maps to rendered markup, when to choose CSR over SSR — leaving the other two as how-to and reference. The distinction that justifies the split is that `creating-phoria-island-components` is React sample code while the concept is framework-agnostic: a reader on Svelte needs the concept and not that particular sample.

**Incomplete guides state what they will cover.** Any guide still incomplete when the slice lands — `configuration.md`, `workspaces.md` and `supported-ui-frameworks.md` are the expected cases — carries a notice that names what the guide will cover, points to the nearest working alternative, and promises no date. This replaces the current generic work-in-progress warning, which names neither what is missing nor what to read meanwhile.

## The journey page after extraction

`docs/guides/getting-started.md` retains:

- The two routes in: a trialist route that does not yet exist and is built by this slice (below), and the manual-add route.
- **Prerequisites, inline.** This answers an open question from the problem-space document: there is no separate prerequisites page, because `getting-started.md:26-40` already is one, including the Linux `SSL_CERT_DIR` note.
- The project-setup spine — create the .NET web app, add Vite, create `package.json`, add dependencies. These are not Phoria concepts and stay put.
- The ordered island steps, each keeping its code, surrendering its conceptual prose to the linked guide, and gaining a link.

**One declared exception.** `getting-started.md` keeps a minimal component and registration inline while `creating-phoria-island-components.md` keeps the full treatment including `PhoriaIslandComponentFactory`. The journey needs runnable code at the point of use; the guide needs to be complete. This overlap is intentional and is declared here so that it does not later read as an accident and get "fixed" by deleting one side.

## The trialist path

Three commands, with Docker as the only prerequisite:

```bash
pnpx giget gh:CMeeg/phoria/examples/getting-started getting-started
cd getting-started && docker compose up --build -d
```

Then open `http://localhost:8080` and stop with `docker compose down`. One line then points at the development loop for anyone who wants to edit what they just saw.

The path is described honestly. The first invocation pulls and builds a multi-stage .NET and Node image, so it is a few minutes rather than a minute, and the documents should not claim otherwise.

**Why this path.** It is the only route that requires no local toolchain — no .NET SDK, no Node, no pnpm, no Aspire CLI — so it is the only one a reader who has merely landed on the repository can actually complete. Its two halves are already proven: 9/9 for standalone `giget` fetch, and 9/9 for a Production container reaching `Healthy`. It is also the exact path carrying the ten currently-broken `giget` references, so building the trialist path and repairing those references are the same piece of work.

`with-workspace` qualifies alongside the other eight: every example has a compose file, a Dockerfile and a `.dockerignore`, all on port 8080, so no example needs a documented exception.

**Placement, and the rule that keeps it from duplicating.** `README.md` states the commands inline, because a reader who never clicks through must still see them, and `getting-started.md` owns the fuller version with the explanation and the development-loop pointer. To stop that becoming two homes: **at most three commands, as one contiguous block, may be restated in `README.md`; anything longer is owned by exactly one guide.** The trialist block qualifies at three commands including the teardown line.

**The `#canary` aside appears in two places, not ten** — the root `README.md` trialist block, and the `## Catalog` table in `examples/README.md`, which is partly the user-facing index of examples. It does not appear in the nine per-example READMEs: those document the released state, and a pre-1.0 branch target repeated nine times is noise that would outlive the beta stream. The aside states that appending `#canary` to the ref targets the canary branch, and that the separator is `#` and not `@`, which is npm dist-tag syntax — a distinction currently recorded only in `docs/MEMORY.md` for agents, and exactly the sort of detail that produces a 404.

**Verification of the references is a post-cut step, deliberately.** An unqualified `giget` ref resolves the repository's default branch, so it cannot be verified until `examples/` is on `main`. It becomes **step 6 of the existing stable-cut runbook** at `CONTRIBUTING.md:72-77`, performed by the maintainer running the cut, because agents cannot perform it — `gh` is unavailable in the working environment. The step confirms the references resolve on `main` and that one example fetches and builds. No new process is introduced; the cut already has a numbered runbook and this extends it.

**`README.md`'s guide index becomes journey-shaped.** The current flat five-item list under `## Usage` is replaced by an index ordered as the reader's path — run it, add an island, then go deeper — linking only the guides that are non-placeholder when the slice lands. The existing honesty property survives, in that the index advertises only real content, and the accompanying `> [!NOTE]` is kept.

## Documentation/code coupling: the trigger

Every plan document must contain a docs-review task that **names the documents it checked**. A plan without one is visibly incomplete, and the set of plans can be searched for it.

Where no public surface changed, the task still exists and states that, with the reason. This is not a formal "not applicable" rule; it is the minimum needed to make "names the documents" coherent when the list is empty.

The step is recorded in three places:

- `CONTRIBUTING.md`, upgrading the existing passive wording — that contributions "should review whether" the README and guides need to reflect a change — into an imperative statement of the new requirement.
- `AGENTS.md`, so that agent-authored plans include it.
- The writing-plans output convention, which is what actually makes the step appear in new plans.

**Existing plan documents are not retrofitted.** The eighteen plans under `docs/plans/` and `docs/superpowers/plans/` are historical records of completed work. Rewriting them prevents no future rot, and skipping them keeps this diff small. New plans only.

## Package READMEs

Seven packages are published. Five of their READMEs are 70-79 byte stubs — a title and a single emoji line — and these are the **npm and NuGet landing pages**, so this is the highest-leverage fix available for a reader who arrives via a registry rather than through GitHub. Only `phoria-opentelemetry` and `vite-plugin-dotnet-dev-certs` have any substance today.

Uniform shape: **purpose** (what the package is and when a reader would want it) → **install** (the real command, with the real version range) → **use** (a minimal working snippet) → **learn more** (a link to the relevant guide). Concepts yes, internals no, per the depth boundary in the problem-space document.

A package README's **learn more** section is where a package's own `docs` folder is reached, and the escalation is deliberate: **a README holds what fits in a README, and a `docs` folder inside the package is created when it does not.** `phoria-islands` is the first package to need one.

`Phoria.Tests` is not published and gets no README. That is stated here so its absence is a decision rather than an oversight.

A useful consequence, recorded as a consequence and not as a mechanism: once every package links to the guide that documents it, the repository gains a second, free drift detector. A guide with no package pointing at it is not wrong, but a package pointing at a guide that no longer exists is a broken link a reviewer will eventually see. This is not a check, and nothing enforces it.

## Example READMEs

All nine exist and their prose is largely right. What is missing is structure and the first-run path: none has a single `##` heading, all lead with the local development commands, and **none mentions Docker at all** — which is the path the trialist route depends on.

Uniform shape, in this order:

1. **What it demonstrates** — the existing opening paragraph, kept **verbatim**. It carries the per-example detail that makes each example's feature legible — Storybook's setup, styled-components' `ServerStyleSheet` seam, Tailwind's Vite plugin — and rewriting it would lose exactly that. The uniformity is carried entirely by the headings.
2. **Try it** — `giget` to fetch, `docker compose up --build -d`, the URL, how to stop.
3. **Develop it** — local install and build, the development command, the ports, how to stop.
4. **Test it** — the e2e suite and `PHORIA_WEBAPP_URL`.
5. **Learn more** — links to the guides relevant to that example.

Because all nine are at parity on Docker, this shape is uniform. There is no per-file exception to declare, which is the outcome the earlier parity work was supposed to produce.

## Contributor documentation: adding a framework package

`createPhoriaFrameworkPlugin` and `PhoriaFrameworkPluginOptions` are exported from `@phoria/phoria/vite` and consumed by all three framework packages, each of which is a thin composer over it. `docs/ARCHITECTURE.md:263-288` describes what the three existing adapters do. **Nothing describes how to write a fourth.**

The canonical document is **`packages/phoria-islands/docs/framework-plugin.md`**, in the package's own `docs` folder, per the placement rule below. It is a complete recipe: the four-entry pattern, the `registerCsrService` / `registerSsrService` registrations, what the shared factory owns versus what the framework package must supply, the include and exclude globs, the optimise-dependency entries, the SSR external, and the environment guards. The per-framework SSR and CSR differences already tabulated at `ARCHITECTURE.md:282-288` become the worked examples.

`ARCHITECTURE.md` keeps its existing summary at `:263-288` — it is already correct, and it is where agents arrive — and gains a link to the recipe. **Duplication is a permitted fallback, not a defect.** If a later agent reads `ARCHITECTURE.md` and does not follow the link, the fix is to duplicate the recipe into `ARCHITECTURE.md`, decided by that observation rather than in advance. The three framework package READMEs link to the recipe as well, which is part of why it has to be complete rather than a pointer.

This is the first `docs` folder inside a package, and the recipe is too large for a README — it is a reference, while the README is the npm and NuGet landing page. The convention it establishes: **a package's documentation lives in its README, or in a `docs` folder inside the package, one file per topic, linked from that README.** Content that fits in a README section stays in the README; a `docs` folder is created when it does not, and never for a single short page placed ad hoc beside a source file, because a document at `vite/framework.md` is invisible to anyone browsing the package rather than reading it. Package docs are not shipped in the published artefact.

There is a live consumer for it: the open `TODO.md` item to add a `phoria-preact` package and a `framework-preact` example. The recipe is the prerequisite for that work, and writing the recipe before the work is a fair test of whether it is actually sufficient.

## Document placement rule

A document describing **one package's own public surface** lives with that package — in its README, or in a `docs` folder inside the package. Anything describing **how Phoria's pieces cooperate** stays whole in `docs/guides/` and links into the package for whatever is genuinely package-local.

The reason is that Phoria's concepts are not package-decomposable. A reader's question is "how do I get an island working", and the answer is distributed across `phoria-islands`, a framework package, the `Phoria` NuGet package, the dev-certs plugin and `@phoria/opentelemetry`. Splitting the documentation along package lines would separate the framework-agnostic half of a concept from its framework-specific half and force the reader to know which package folder to open. The framework packages are deliberately thin — that thinness is the point of the shared factory — and a thin package earns a thin README, which is the package-README section above.

**The audiences differ, and the rule covers both.** `README.md` and `docs/guides/` are written for humans first and must stand alone, because a human will not follow a link chain to assemble an answer. `ARCHITECTURE.md` is written for agents first and remains readable by humans, so it may summarise a canonical document and link to it rather than restating it. Where the two disagree about how much to restate, `ARCHITECTURE.md` follows the link and duplication is added only if following it proves insufficient.

**A companion distinction, for "supported frameworks" and similar questions.** A reader asking *does this work with Svelte, and what does it look like* is best served by a link to `examples/framework-svelte`, which is executable and already green under its own e2e suite. A package README answers *what does this export do*, not *show me it working*. So: *does it work with X, show me X* → the example; *what does this export, option or entry point do* → the package.

## Verification

Proportionate to the class of document, because the failure mode this slice is fixing — a command that looks correct and is not — cannot be caught by reading.

| Class | Verified by |
| --- | --- |
| The trialist path | **Executed**: `giget` fetch, `docker compose up --build -d`, `/health` reporting `Healthy`, and the page loading |
| The ten `giget` references | Post-cut only, as step 6 of the stable-cut runbook. An unqualified `giget` ref resolves the repository's default branch, so it cannot be verified until `examples/` is on `main` |
| The extracted concept guides | Read-through against the code. The prose already exists and is being moved rather than rewritten, so execution would test the movement rather than the content |
| The `getting-started` spine | Read-through, with every command checked against the target `package.json` scripts |
| `README.md` | Read-through, plus the three-command trialist block executed as part of the trialist-path check |
| Package READMEs | Read-through, with install commands and version ranges checked against each `package.json` |
| Example READMEs | Read-through. The flows they describe are already proven by the nine examples' e2e suites |
| `packages/phoria-islands/docs/framework-plugin.md` | Read-through against `src/vite/framework.ts` and the three framework packages' `vite/plugin.ts` composers |
| `ARCHITECTURE.md` | Read-through against the code, per the existing definition of done for the milestone |

## Plan decomposition

This is two plans, not one. The two additions made during scoping — a README sweep across sixteen files and a contributor recipe — are mechanical and independently verifiable, while the extraction and restructure is the part carrying genuine risk, because it moves prose. One plan would bury that risk under sixteen README rewrites.

- **Plan A — structure, first-run path, and the trigger.** The seam test applied across `getting-started.md`; the concept guide mapping, with `phoria-islands` taking the concept register; the trialist path in `README.md` and `getting-started.md`, the journey-shaped guide index, and the `#canary` aside in its two locations; the repair of the ten `giget` references; the trigger in `CONTRIBUTING.md`, `AGENTS.md` and the writing-plans convention, plus step 6 of the stable-cut runbook; the framework recipe at `packages/phoria-islands/docs/framework-plugin.md` with a link from `ARCHITECTURE.md`; the rest of the `ARCHITECTURE.md` drift pass.
- **Plan B — the README sweep.** Seven package READMEs and nine example READMEs to the shapes above. Mechanical. Its only hard dependency is Plan A's trialist path existing, since the example READMEs link to it.

The trigger work lands early in Plan A, so the remainder of the slice is done under the mechanism the slice introduces.

## Open questions

None. All six questions raised during scoping were closed before planning:

- **`phoria-islands`** resolves to the **concept register**, keeping three files with distinct registers — concept, how-to, reference. The how-to is React sample code while the concept is framework-agnostic, so a reader on Svelte needs the concept and not that particular sample.
- **A retained placeholder** must name what the guide will cover, point to the nearest working alternative, and promise no date. This replaces the current generic work-in-progress notice, which names neither what is missing nor what to read meanwhile. It applies to `configuration.md`, `workspaces.md` and `supported-ui-frameworks.md`, which are expected to remain incomplete.
- **Example READMEs take a uniform heading skeleton with their existing prose kept verbatim** as the body of the first section. Neither rewriting nor keeping wholesale: rewriting risks losing the per-example detail that makes each example's feature legible, and keeping it whole risks nine different shapes.
- **The `#canary` aside appears in two locations, not ten** — the root `README.md` trialist block and the `## Catalog` of `examples/README.md`. It does not appear in the nine per-example READMEs, which document the released state, and repeating a pre-1.0 branch target nine times is noise that would outlive the beta stream. The aside states that appending `#canary` targets the canary branch, and that the separator is `#` and not `@`, which is npm dist-tag syntax.
- **Post-cut verification of the ten `giget` references becomes step 6 of the existing stable-cut runbook** at `CONTRIBUTING.md:72-77`, performed by the maintainer running the cut. Agents cannot perform it, because `gh` is unavailable in the working environment. The step verifies that the references resolve on `main` and that one example fetches and builds.
- **`README.md` gets a journey-shaped index** in place of the current flat five-item list, linking only the guides that are non-placeholder when the slice lands. The existing honesty property — the index advertises only real content — survives, and the accompanying `> [!NOTE]` is kept.

## Riskiest unknowns

- **The mechanism remains unenforced and its efficacy is unproven.** The convention already exists in `CONTRIBUTING.md` and demonstrably did not prevent six phases of drift. Giving it a trigger is a better bet than restating it, but nothing in this slice will report whether it worked. The honest position is that this stays unknown until the next phase's work has been under way for a while, and it should be reviewed at that boundary rather than treated as settled. This is the largest single risk in the slice, and it is a risk accepted rather than mitigated.
- **Extraction can quietly lose content.** Moving prose between files under a mechanical test is exactly the operation where a paragraph gets dropped and nobody notices, because the paragraph is then absent from both the source and the destination. Mitigation is the read-through pass over both sides of every move, and the plan should treat "did anything get lost" as an explicit check rather than an assumed outcome.
- **The reorganisation can create overlap the reader cannot resolve.** The declared exception around `getting-started` and `creating-phoria-island-components` is the known case, now that `phoria-islands` has been given its own concept register. If the extraction goes wrong, the symptom is a reader unable to tell which of two guides to read — a worse outcome than the empty placeholders being replaced, because the content exists but cannot be found.
- **Sixteen README rewrites are individually small and collectively easy to get wrong.** A version range copied from the wrong `package.json`, or a port belonging to a different example, is the likely failure. The nine e2e suites do not cover the READMEs, so this rests on the read-through.
