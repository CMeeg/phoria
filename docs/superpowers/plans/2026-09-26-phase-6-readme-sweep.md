# Phase 6 README Sweep Implementation Plan (Plan B)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the seven package READMEs and the nine example READMEs to the two uniform shapes the spec defines, so that a reader arriving at an npm, NuGet or GitHub landing page finds a real purpose, a real install command, a working snippet and a way onward.

**Architecture:** Sixteen files, no new prose to invent. The example READMEs already carry per-example prose that is right and must survive verbatim; this slice gives it headings. The package READMEs are mostly title-and-emoji stubs on the npm and NuGet landing pages, so they need four sections each, and their install commands and version ranges must be taken from the registry and each package's own `package.json` rather than from a sibling file. The nine per-example `giget` references move into the new uniform `## Try it` section — that move is the repair Plan A deferred here — and they stay unverifiable until the `canary` → `main` cut, exactly as Plan A left them.

**Tech Stack:** pnpm 11 workspaces; npm and NuGet registries; Markdown; Docker Compose (every example on port 8080). No product code is written by this slice.

**Spec:** [`docs/superpowers/specs/2026-09-26-phase-6-docs-structure-design.md`](docs/superpowers/specs/2026-09-26-phase-6-docs-structure-design.md) — the plan argues from the spec, so the spec travels with it; executors read both. `## Package READMEs` (spec:119) and `## Example READMEs` (spec:131) are the two shapes. `## Plan decomposition` (spec:183) is what puts this work in a second plan.

> **Dependency: this plan runs after [`docs/superpowers/plans/2026-09-29-canary-dist-tag.md`](docs/superpowers/plans/2026-09-29-canary-dist-tag.md), and after its maintainer action has landed.** Every npm install line in this slice names the `canary` dist-tag, and that tag does not exist until the maintainer runs the `npm dist-tag` repair recorded in that plan. Writing `@canary` into seven live npm landing pages before the tag exists would ship broken install commands to every reader, and unlike the `giget` references — which resolve the moment the `canary` → `main` cut lands — this one is fixable today, so there is no reason to ship it broken. Confirm the tag before Task 1: `npm view @phoria/phoria dist-tags --json` must contain `canary`.

## Global Constraints

- **Prose and commit messages in British English** — `centralise`, not `centralize`. Historical documents that predate this are exempt.
- **Do not hard-wrap prose.** Every paragraph is one unbroken line; block elements (headings, tables, fences) and each list item — including continuation prose — get their own line. This is `AGENTS.md`'s `### Markdown & prose` rule.
- **This slice writes no product code.** No `.ts`, `.cs`, `.csproj` or `package.json` file is modified. Sixteen Markdown files change, and the plan's own document. A `git status` listing any other file means something outside the plan ran — find it before committing.
- **Guides never reference `ARCHITECTURE.md`.** The dependency is one-way: `ARCHITECTURE.md` links to guides, guides do not link back. Carried from Plan A.
- **A `## Related` or `## Learn more` entry says what the link gives the reader** — at least thirty characters after the em dash. Nine identical link lists that tell a reader nothing are the failure this prevents. Carried from Plan A, extended here to the new section.
- **A reference document states the supported surface, never what is absent.** No README says what it lacks.
- **A document must never state another document's completeness.** No README says a guide is incomplete, in progress, or a stub.
- **No counts of examples.** Never "all nine", "the eight single-WebApp examples". Counts of frameworks and adapters are fine, because they are fixed parts of the surface rather than a tally of files in the repository.
- **The `#canary` aside appears in exactly two places** — the root `README.md` trialist block and the `## Catalog` of `examples/README.md`. It does **not** appear in the nine per-example READMEs: those document the released state, and a pre-1.0 branch target repeated nine times is noise that would outlive the beta stream.
- **`giget` commands use lowercase `gh:cmeeg/phoria`.** Links use canonical `github.com/CMeeg/phoria`. The two casing rules point opposite ways on purpose: a command is typed by a reader, a link is followed by a case-sensitive tool. Carried from Plan A Task 13.
- **The nine `giget` references stay unverifiable.** An unqualified `giget` ref resolves the repository's default branch, and `examples/` is absent from `main` until the `canary` → `main` cut. No check in this plan may assert that any `giget` reference resolves. Verification is step 6 of the stable-cut runbook in `CONTRIBUTING.md`, performed by the maintainer running the cut.
- **A package's documentation lives in its README, or in a `docs` folder inside the package** — one file per topic, linked from that README. `packages/phoria-islands/docs/framework-plugin.md` is that folder's first file and is created; nothing here adds a second one.
- **Every check is self-validating, control-able, and carries a negative control.** A check that no mutation can turn red proves the fixture, not the check. This is Plan A's standing rule and it cost four defective checks to learn.
- **A check that reads a fact must assert it read something.** A filter that greps a *path list* for content words silently produces an empty scan; a suite will run nineteen checks over nothing and report nineteen passes. Every file-reading check carries an input-count floor.
- **Derive facts from source, never from the file being rewritten.** The ports come from `AppHost/Program.cs` and `appsettings.json`. The versions come from the registry and each `package.json`. The old README is evidence of what the prose said, never evidence of a fact.

## Review Focus

The failure modes this spec implies but no check exercises, most likely to bite a reader first:

1. **A port belonging to a different example.** The nine examples' development ports interleave on one ladder — `5073`, `5173`, `5273`, `5373`, `5473`, `5573`, `5673`, `5773`, `5873`, `5973` — with each example's Phoria Server port sitting immediately *below* its WebApp port and immediately *above* its neighbour's. A neighbouring example's number is therefore a plausible-looking wrong answer, and nothing on the page distinguishes the two. Worse, `AppHost/Program.cs` computes the Phoria Server port as a fallback literal that differs from the effective value: `getting-started` falls back to `5173` while its effective port is `5273`, read from `appsettings.json`. Reading the code and printing the literal produces a confidently wrong port. Expected: each README's WebApp and Phoria Server ports equal the values derived from source for *that* example — pinned by `ports_match_source` in Task 5, Task 6 and again across all nine in Task 8.
2. **An install command that resolves to a version the documentation does not describe.** For five of the six published npm packages the `latest` dist-tag is a *stable* release older than the version in this repository, so a bare `npm i @phoria/phoria` installs `0.4.2` while the repository is at `0.5.0-beta.1`. `@phoria/opentelemetry` was the inverse trap — its prerelease tag sat at its first publish, *older* than its `latest` — and `docs/superpowers/plans/2026-09-29-canary-dist-tag.md` repairs it, so all six install lines are now the same tagged form. The trap is gone; the bare-name and caret-range forms are wrong for every package and remain the live risk. Expected: every install command resolves to the version this repository ships, verified against a recorded registry baseline — pinned by `nuget_install_command` in Task 1 and by `islands_install_command`, `framework_install`, `dev_certs_install` and `otel_install` in Tasks 2 through 4.
3. **The per-example prose lost in the reshape.** The uniform shape is the whole point of the slice, and reshaping nine files by hand is the easiest way to drop a sentence. `with-storybook` carries four paragraphs beyond the common three — the Storybook commands and port `6006`, the Vite pin and why it exists, and the note on `@storybook/addon-essentials` — and they are the most likely thing to go missing. Expected: every non-empty line of the pre-slice README still appears in the post-slice README — pinned by `premise_survives`, a whole-line loss check over all nine files in Task 5, Task 6 and Task 8.
4. **A `## Learn more` link to an incomplete guide.** Three guides carry an incomplete-guide notice: `configuration.md`, `workspaces.md` and `supported-ui-frameworks.md`. `workspaces.md` is the obvious link for the `with-workspace` example and is one of the three, so the natural choice is the wrong one. Expected: every `## Learn more` target is a guide with no incomplete notice — pinned by `learn_more_targets_are_complete` in Task 1 through Task 6 and across all sixteen files in Task 8.
5. **A `giget` path whose fetch directory and `cd` disagree.** The `## Try it` block has two copies of the example's name: the `giget` target directory and the directory changed into. They are the same string in the root `README.md` and must be the same string in all nine, and the path segment must be the example's real directory name — `framework-multiple`, not `frameworks-multiple`, and not the Phoria Server port. Expected: the two names agree and match the directory on disk — pinned by `try_it_path_is_self_consistent` in Task 5, Task 6 and Task 8.

## Rulings

Decisions this plan makes where the spec is silent, or where reading the repository changed the answer. Each names what it decides and why, so a later reader does not re-litigate it.

- **A package README's `# H1` is the published package name.** npm shows `@phoria/phoria`; NuGet shows `Phoria`. The current headings are a mix — `phoria-vue`, `vite-plugin-dotnet-dev-certs`, and `phoria-islands/README.md` says `# phoria`, which is neither the directory name nor the published name. A landing page that names the thing the reader installed is uniform and unambiguous.
- **An npm install command names the dist-tag, and the dist-tag is `canary`.** Every npm install line in this slice is `npm i <name>@canary`, for all six published packages without exception. The tag is set by the `tag` field of `.changeset/pre.json` and is renamed from `beta` by [`docs/superpowers/plans/2026-09-29-canary-dist-tag.md`](docs/superpowers/plans/2026-09-29-canary-dist-tag.md), which also repairs `@phoria/opentelemetry` — its `beta` tag is frozen at its first publish and every later publish was routed to `latest`, so the tag that was right for the other five was a silent two-version downgrade there. A caret range on a `0.x` prerelease is worse than a tagged install: it silently admits versions the guides have never described. **This slice is gated on the `canary` tag existing** — see the dependency note above.
- **The NuGet install command pins the version, and the pin is the newest published one.** `dotnet add package Phoria --version 0.5.0-beta.1`. NuGet has no dist-tags, and a landing page that installs a moving target documents a version nobody chose. The dist-tag rename does **not** change this line: `0.5.0-beta.1` is published and remains installable, and the version family moves to `-canary.N` only at the next version PR. Do not "correct" the pin to a `-canary` version — none exists yet, and a pin to an unpublished version fails to restore. Re-pin at the next release, the same as any version bump.
- **`## Learn more` links only guides that are complete.** A reader sent from a landing page to a guide carrying an incomplete notice has been handed a dead end, and the guide's own notice does not fix that for someone who arrived looking for an answer. This matches the rule Plan A applied to the root `README.md`'s index. Where the natural link is an incomplete guide, the coverage moves into the file that would have held the link: `with-workspace` does not link `workspaces.md`, so its own `## What it demonstrates` says in full that it is a complete workspace layout that installs, builds and runs standalone — which is the sentence `workspaces.md`'s notice sends readers to, and the reason the reader who wanted that guide is not left without an answer.
- **The per-example `## What it demonstrates` body is the pre-slice opening paragraph, byte for byte.** Where an example carries extra rationale paragraphs, they join that section. `with-storybook`'s Vite pin and its `@storybook/addon-essentials` note both explain what the example demonstrates and why it is shaped this way, so they belong there; its Storybook commands and port `6006` are development-loop content and go in `## Develop it`.
- **Each example README states both its WebApp and its Phoria Server development port.** `examples/README.md` states only the WebApp port, and says so in the sentence under its table. That is a catalog-width decision about a nine-row table, not a competing claim, and the per-example file remains the authority for that example's ports.
- **The `SSL_CERT_DIR` prose stays in both `getting-started.md` and the dev-certs package README.** They are different registers: one is a prerequisite warning inside a numbered list, the other is the package's own troubleshooting section. Neither is a pointer to the other, and no third mention is added — the dev-certs README's `## Learn more` does not link to the guide's note, because a link there would be the duplicate.
- **The NuGet README is what nuget.org renders.** `packages/Phoria/Phoria.csproj` packs `README.md` into the `.nupkg`. That is the reason this file matters as much as the npm ones, and it is why Task 1 is its own task rather than being folded into Task 2.
- **`Phoria.Tests` gets no README.** Stated in the spec (spec:127) so its absence is a decision. It is not published, and there is no landing page to land on.

## Findings Recorded During Planning

Four places where the repository disagreed with the spec, or with a fact a reader would assume. None changes the work; all are recorded so the discrepancy is not rediscovered as a surprise.

1. **The spec's stub count is off by one shape, not by one file.** Spec:121 says "Five of their READMEs are 70-79 byte stubs". Four are in that range — `Phoria` 70, `phoria-vue` 73, `phoria-react` 77, `phoria-svelte` 79. The fifth, `phoria-islands`, is 153 bytes and already carries the `## Learn more` link Plan A Task 11 added. It is a stub with one section on it, not a title and an emoji line. The work is the same for all five; only the description is imprecise.
2. **The nine `giget` repairs are structural, not a casing fix.** Plan A Step 5 hands them here, and spec:174 calls them "the ten currently-broken `giget` references". They are not syntactically broken: GitHub resolves the lowercase owner case-insensitively, and they fail only because `examples/` is absent from `main` until the cut — Plan A Task 13's finding. The repair is that each one moves into a uniform `## Try it` section that names the fetch, the build, the URL and the teardown. The casing stays lowercase, per the global constraint.
3. **The Phoria Server port cannot be read from `AppHost/Program.cs`.** Every example computes it as `int.TryParse(webAppConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : <literal>`, and that literal is a *different number* from the effective port in all nine cases — `getting-started` falls back to `5173` and runs on `5273`. The effective value is `phoria.server.port` in the example's `appsettings.json`. The key is lowercase in the JSON and the lookup in the code is capitalised, and the .NET configuration binder is case-insensitive, so the running application and a case-sensitive reader of the same file disagree. A probe that asks `appsettings.json` for `Phoria` reports the key as absent, which is how this was nearly recorded as a defect in the READMEs rather than as a fact about them. The derivation is in Task 0 and the check is in Task 5.
4. **The spec's verification table has no row for the new `## Try it` sections.** Spec:179 puts example READMEs at "Read-through", which was correct when the Docker path was not in them. This slice adds a Docker claim to nine files. Task 7 executes it once, for `with-workspace`, because that is the only example whose Dockerfile sits at depth 3 (`apps/WebApp/Dockerfile`) and the only one whose layout differs.

## File Structure

Sixteen files change. No file is created except this plan. Each entry names the single responsibility the file holds after this slice.

| File | Class | Shape it reaches | Source of its facts |
| --- | --- | --- | --- |
| `packages/Phoria/README.md` | package, NuGet | purpose → install → use → learn more | `Phoria.csproj`, nuget.org version list, `ServiceCollectionExtensions.cs`, `ApplicationBuilderExtensions.cs` |
| `packages/phoria-islands/README.md` | package, npm | purpose → install → use → learn more | `package.json`, `src/vite/plugin.ts`, `docs/framework-plugin.md` |
| `packages/phoria-react/README.md` | package, npm | purpose → install → use → learn more | `package.json`, `src/vite/plugin.ts` |
| `packages/phoria-vue/README.md` | package, npm | purpose → install → use → learn more | `package.json`, `src/vite/plugin.ts` |
| `packages/phoria-svelte/README.md` | package, npm | purpose → install → use → learn more | `package.json`, `src/vite/plugin.ts` |
| `packages/vite-plugin-dotnet-dev-certs/README.md` | package, npm | purpose → install → use → learn more, existing Linux section kept | `package.json`, `src/plugin.ts`, the existing README |
| `packages/phoria-opentelemetry/README.md` | package, npm | purpose → install → use → learn more, existing snippet kept | `package.json`, the existing README |
| `examples/getting-started/README.md` | example | what → try → develop → test → learn more | `AppHost/Program.cs`, `WebApp/appsettings.json`, `WebApp/package.json` |
| `examples/framework-react/README.md` | example | as above | as above |
| `examples/framework-vue/README.md` | example | as above | as above |
| `examples/framework-svelte/README.md` | example | as above | as above |
| `examples/framework-multiple/README.md` | example | as above | as above |
| `examples/with-tailwind/README.md` | example | as above | as above |
| `examples/with-styled-components/README.md` | example | as above | as above |
| `examples/with-storybook/README.md` | example | as above, four extra paragraphs rehomed | as above, plus `WebApp/package.json` Storybook scripts |
| `examples/with-workspace/README.md` | example | as above, commands differ | `AppHost/Program.cs`, `apps/WebApp/appsettings.json`, root and app `package.json` |

**Not modified.** The root `README.md` and `examples/README.md` — Plan A already brought both to shape, and this slice's job is to point *at* them, not to change them. The fourteen files in `docs/guides/` — this slice links to them and never edits them. `packages/Phoria.Tests/` — no README, by decision. The `docs` folder inside `phoria-islands` — the recipe is complete and this slice links to it.

## Sequencing Decisions

- **The seven package READMEs precede the nine example READMEs.** They are independent, but the package READMEs establish the `## Learn more` convention and the "entry says what the link gives you" rule that the example READMEs then follow. Doing packages first means the convention is exercised twice on small files before it is applied nine times.
- **`Phoria` is its own task, first.** It is the only file in the slice with a different install mechanism, and it is packed into a NuGet package, so a mistake in it ships to a registry rather than staying in the repository. Reviewing it alone against the six npm files is cheap.
- **The three framework packages are one task.** They are the same shape with three names, the same check catches all three, and the recipe link lands in all three at once. Splitting them would spend three review gates on one edit.
- **`with-workspace` is separate from the other eight.** Its install and build run from the workspace root, its remaining scripts run through `pnpm --dir apps/WebApp`, and its Dockerfile is at depth 3. A reviewer could reasonably approve the eight and reject this one; that is exactly the boundary worth a gate.
- **The `with-workspace` execution follows the writing, not the reverse.** Writing the block and proving the block are separate deliverables with separate failure modes. If the execution goes red the fix may land in any of the nine files, which is why it is not folded into a writing task.
- **The final task is a sweep across all sixteen, not a per-file review.** The per-file checks run in the writing tasks. Task 8 runs them again over the whole set at once, because the failure this slice is most likely to produce is one that only appears *between* files — nine READMEs that are each correct and jointly inconsistent.

---

### Task 0: Verification scaffolding and the facts baseline

**Files:**
- Create: `/tmp/opencode/ph7-lib.sh`
- Create: `/tmp/opencode/ph7-facts.sh`
- Create: `/tmp/opencode/ph7-task0-verify.sh`

**Interfaces:**
- Consumes: `/tmp/opencode/ph6-lib.sh` and `/tmp/opencode/ph6-facts.sh` if either still exists from Plan A. Plan A's helpers are the reference implementation; this task rewrites what Plan B needs rather than importing across plans, because Plan B may be executed on a machine where Plan A's `/tmp` files are gone.
- Produces: `ph7-facts.sh` exports `example_dirs`, `webapp_dir <example>`, `webapp_port <example>`, `phoria_server_port <example>`, `pkg_json_field <pkgdir> <field>`, and `registry_baseline`. `ph7-lib.sh` exports `no_placeholder`, `links_resolve`, `has_relative_links`, `hard_wraps`, `expect_pass`, `expect_fail`, and `loss_check`.

- [ ] **Step 1: Capture the registry baseline, once, and record the date it was taken**

The install commands in Tasks 1 through 4 are checked against a recorded baseline rather than a live registry, so the suite is hermetic and a registry outage cannot turn it red. Capture the live values now, put them in `ph7-facts.sh` as literals with the capture date beside them, and re-capture deliberately when a version is bumped.

```bash
for p in "@phoria/phoria" "@phoria/phoria-react" "@phoria/phoria-vue" "@phoria/phoria-svelte" "@phoria/vite-plugin-dotnet-dev-certs" "@phoria/opentelemetry"; do
  printf '%-42s ' "$p"; curl -s --max-time 20 "https://registry.npmjs.org/-/package/$p/dist-tags" | tr -d '\n '; echo
done
curl -s --max-time 20 "https://api.nuget.org/v3-flatcontainer/phoria/index.json"
```

The baseline as captured, for the record. `latest` and `beta` for all six npm packages, and the NuGet version list:

| Package | `latest` | `beta` | In this repository |
| --- | --- | --- | --- |
| `@phoria/phoria` | `0.4.2` | `0.5.0-beta.1` | `0.5.0-beta.1` |
| `@phoria/phoria-react` | `0.4.2` | `0.5.0-beta.1` | `0.5.0-beta.1` |
| `@phoria/phoria-vue` | `0.3.2` | `0.4.0-beta.1` | `0.4.0-beta.1` |
| `@phoria/phoria-svelte` | `0.3.2` | `0.4.0-beta.1` | `0.4.0-beta.1` |
| `@phoria/vite-plugin-dotnet-dev-certs` | `0.2.1` | `0.3.0-beta.0` | `0.3.0-beta.0` |
| `@phoria/opentelemetry` | `0.2.0-beta.2` | `0.2.0-beta.0` | `0.2.0-beta.2` |
| `Phoria` (NuGet) | `0.5.0-beta.1` is the newest of nine published versions | — | `0.5.0-beta.1` |

Two rows carry the trap. For five packages `latest` is a stable release *older* than this repository, so the bare name installs something the guides do not describe. For `@phoria/opentelemetry` the columns invert: `beta` is older than `latest`, and `latest` is itself a prerelease.

**This table predates the dist-tag repair and must be re-captured at Task 0.** After `docs/superpowers/plans/2026-09-29-canary-dist-tag.md`'s maintainer action, the `beta` column is gone and a `canary` column carries the same values for the five healthy packages, `@phoria/opentelemetry`'s `canary` is `0.2.0-beta.2` — the value its `beta` column wrongly understated — and its `latest` is gone entirely, so a bare install fails rather than silently handing out a prerelease. The `In this repository` column is unaffected: the version family changes at the *next* version PR, not in this repository's current state. Re-capture rather than editing these rows, so the baseline is a record of the registry and not of an expectation.

- [ ] **Step 2: Write `example_dirs`, `webapp_dir` and the two port readers**

`example_dirs` is the nine example directories, derived — `for d in examples/*/; do [ -f "$d/README.md" ] || continue; …` — not hand-listed, so a tenth example cannot be silently skipped. A hand-enumerated set that omits a member is the exact defect Plan A found in Task 8.

`webapp_dir` is `WebApp` for the eight standalone examples and `apps/WebApp` for `with-workspace`. Derive it by testing for `apps/WebApp` rather than by testing the name, so the rule is structural:

```bash
webapp_dir() { local d="examples/$1"; if [ -d "$d/apps/WebApp" ]; then echo "apps/WebApp"; else echo "WebApp"; fi }
```

`webapp_port` reads the AppHost. The eight standalone examples declare it as a named variable; `with-workspace` passes a bare literal to `WithHttpEndpoint`. Both forms must be read, and a reader that only handles the first silently reports nothing for `with-workspace` — so the function must fall through to the literal and the check must assert a non-empty result for all nine:

```bash
webapp_port() {
  local src; src=$(cat "examples/$1/AppHost/Program.cs")
  local m
  m=$(printf '%s' "$src" | grep -oE 'var webAppPort = [0-9]+' | grep -oE '[0-9]+$' | head -1)
  [ -n "$m" ] || m=$(printf '%s' "$src" | grep -oE 'WithHttpEndpoint\(port: [0-9]+, name: "http"' | grep -oE '[0-9]+' | head -1)
  printf '%s' "$m"
}
```

`phoria_server_port` reads `appsettings.json`, **not** `AppHost/Program.cs`. The key is lowercase `phoria.server.port`, and the .NET configuration binder is case-insensitive, so a reader that asks for `Phoria` finds nothing and a reader that asks for `phoria` finds the value. Use a case-insensitive walk rather than a literal key, and never read the `Program.cs` fallback — it is a different number in all nine examples:

```bash
phoria_server_port() {
  node -e '
    const fs = require("fs")
    const path = process.argv[1]
    const j = JSON.parse(fs.readFileSync(path, "utf8"))
    const get = (o, k) => { if (o == null) return undefined
      for (const key of Object.keys(o)) if (key.toLowerCase() === k.toLowerCase()) return get(o[key])
      return undefined }
    const p = get(get(get(j, "phoria"), "server"), "port")
    if (p == null) process.exit(1)
    process.stdout.write(String(p))
  ' "examples/$1/$(webapp_dir "$1")/appsettings.json"
}
```

- [ ] **Step 3: Write the per-example facts table into `ph7-facts.sh`**

Derived values, captured at plan-authoring time. **These are the baseline the checks assert against, not the source of truth** — if a check disagrees with this table, find out which is wrong before changing either.

| Example | WebApp dev port | Phoria Server dev port |
| --- | ---: | ---: |
| `framework-multiple` | `5573` | `5473` |
| `framework-react` | `5173` | `5073` |
| `framework-svelte` | `5473` | `5373` |
| `framework-vue` | `5273` | `5173` |
| `getting-started` | `5373` | `5273` |
| `with-storybook` | `5973` | `5873` |
| `with-styled-components` | `5873` | `5773` |
| `with-tailwind` | `5773` | `5673` |
| `with-workspace` | `5673` | `5573` |

Every Docker port is `8080`, and `examples/README.md` already carries this table with the same values. That agreement is a cross-check, not the source: the derivation above is, and the two were compared when this plan was written.

- [ ] **Step 4: Write `ph7-task0-verify.sh` and make it fail on its own fixtures**

The suite must exercise each helper against a known-good input and a known-bad one before any task relies on it. Plan A's `LOST` detector was proved in Task 0 for exactly this reason, and Plan A's `no_placeholder` had to be fixed twice after tasks had already used it.

- `example_dirs` returns exactly nine paths, and a control that runs it against a directory holding a tenth example README returns ten. **The control cannot be omitted** — a hand-listed set that returns nine forever is indistinguishable from a correct one until an example is added.
- `webapp_port` returns `5673` for `with-workspace`, exercising the literal fallback, and `5373` for `getting-started`, exercising the named variable. A control that points it at an example directory with no AppHost returns empty and the caller reports a failure rather than an empty comparison that passes.
- `phoria_server_port` returns `5273` for `getting-started`. Its control is the case-sensitivity defect itself: a variant that looks up the capitalised key `Phoria` must return non-zero, and the suite must assert that it does. This is the defect that nearly produced a false finding in this plan; it is the single most valuable control in Task 0.
- `phoria_server_port` must **not** agree with the `Program.cs` fallback. Assert that for `getting-started` the two differ (`5273` against `5173`), which pins the fact that the code literal is not the answer.
- `no_placeholder` returns false for `docs/guides/workspaces.md` and true for `docs/guides/phoria-islands.md`.
- `loss_check` reports a dropped line when one is removed, and reports none when a line is moved. Both directions, because Plan A's defect was a check that could not tell "fixed" from "broke differently".

- [ ] **Step 5: Run the suite and record the result in the ledger**

```bash
bash /tmp/opencode/ph7-task0-verify.sh
```

Expected: every check passes, and the reported check count is non-zero. A suite that exits `0` having run nothing is the worst outcome available and is asserted against explicitly.

- [ ] **Step 6: Append this task's rulings to `.superpowers/sdd/2026-09-26-phase-6-readme-sweep/progress.md` in the same call as the completion line**

Create the ledger if it does not exist. Record: the registry baseline and its capture date; the nine-example port table and the two places it is derivable from; the `phoria` lowercase key finding; the `Program.cs` fallback finding; the two dist-tag inversions; and any defect found in a helper while proving it.

---

### Task 1: The `Phoria` NuGet landing page

**Files:**
- Modify: `packages/Phoria/README.md` (currently 70 bytes: a title and an emoji line)
- Create: `/tmp/opencode/ph7-task1-verify.sh`

**Interfaces:**
- Consumes: `no_placeholder`, `links_resolve`, `hard_wraps`, `registry_baseline` from Task 0.
- Produces: the `## Learn more` entry convention — a bullet, the guide, an em dash, and what the guide gives the reader — which Tasks 2 through 6 follow. Nothing downstream parses this file; the convention is the product.

- [ ] **Step 1: Write the check that fails**

`packages/Phoria/README.md` is the only file in the slice rendered by nuget.org: `Phoria.csproj` declares `<None Include="README.md" Pack="true" PackagePath="\" />`. Four checks, each with a control:

- `nuget_shape` — the file has `# Phoria` as its H1 and exactly the four `##` headings `Install`, `Use`, `Learn more`, with `Use` before `Learn more`. Control: a fixture missing `Install` fails.
- `nuget_install_command` — the file contains `dotnet add package Phoria --version 0.5.0-beta.1`, and the version equals the newest value in the NuGet baseline. Control: substituting `0.4.2` — a real published version, and therefore a *plausible* wrong answer — fails. A control using a version that does not exist would prove nothing, because the check could be rejecting it for being malformed.
- `nuget_api_names` — the file contains `AddPhoria` and `UsePhoria`, and both appear in the source: `services.AddPhoria(` and `app.UsePhoria()`. Control: renaming to a plausible neighbour, `AddPhoriaServer`, fails.
- `nuget_no_placeholder` — `no_placeholder` passes, `links_resolve` passes against the file's own directory, and every `## Learn more` entry has at least thirty characters after the em dash. Control: an entry reading `— the configuration reference.` fails on length.

- [ ] **Step 2: Run it and watch it fail**

Expected: `nuget_shape` fails on the missing headings. The other three fail too — the current file has none of them. Confirm the failure is a *shape* failure and not a harness error by reading the first failing line.

- [ ] **Step 3: Write the README**

The H1 is `Phoria`, the published NuGet package name.

**Purpose**, one paragraph: what the package is — the .NET half of Phoria, bringing islands to Razor Pages and MVC — and when a reader would want it, which is at the moment they add Phoria to an existing .NET application. The existing csproj description is "Islands architecture for dotnet powered by Vite."; the README's purpose section is that sentence plus the two sentences about when to reach for it, and it is a different register from the title-and-emoji line it replaces.

**Install**, the fenced command from Step 1, and one sentence saying the package targets `net8.0` and `net10.0` — which is `Phoria.csproj`'s `TargetFrameworks`, and is a fact a reader needs before the command rather than after it.

**Use**, a minimal snippet using the two real entry points:

```csharp
using Phoria;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPhoria();

var app = builder.Build();

app.UsePhoria();

app.MapRazorPages();

app.Run();
```

The two signatures are the snippet's contract, and both are non-obvious enough that composing the snippet from memory produces code that does not compile:

- `AddPhoria` is `AddPhoria(this IServiceCollection services, Action<PhoriaOptions>? configure = null)`. **It does not take an `IConfiguration`** — it binds `PhoriaOptions.SectionName` itself via `BindConfiguration`, so the minimal call passes nothing. A snippet written as `AddPhoria(builder.Configuration)` is the obvious guess and it is wrong.
- `UsePhoria` is `UsePhoria(this IApplicationBuilder app)` and adds `PhoriaServerMiddleware`. No arguments.
- The namespace is `Phoria`, declared in both files.
- `PhoriaOptions.SectionName` is `"Phoria"` and `PhoriaObservabilityOptions.SectionName` is `"Phoria:Observability"`. The .NET binder is case-insensitive, which is why the example `appsettings.json` files can write the key as lowercase `phoria` and be read — see Finding 3, where that same case-insensitivity is what makes a case-sensitive probe of those files report the key as absent.

One sentence under the snippet says configuration lives under a `Phoria` section, and points at the getting-started guide for the settings a first-time reader meets. That is the supported surface, not a table of everything the options object accepts; a complete configuration reference is `configuration.md`, which is incomplete, and this landing page does not apologise for it or link to it.

**Learn more**, one bullet per guide, each naming what it gives:

- Getting started — adding Phoria to an existing .NET application, step by step.
- Building for production — the build and preview scripts a Phoria solution needs in production.
- Deployment — deploying a production build to Azure Container Apps.

All three are complete guides. `configuration.md` is the guide a reader may expect here and it carries an incomplete notice, so it is not linked; the getting-started guide covers the configuration surface a first-time reader meets.

- [ ] **Step 4: Run the check and watch it pass**

```bash
bash /tmp/opencode/ph7-task1-verify.sh
```

Expected: exit `0`, with a non-zero check count.

- [ ] **Step 5: Read it back against the sources**

The definition of done for this file is a read-through, per spec:178. Confirm the namespace, the signature and the target frameworks against the code — not against this plan, which is a starting point and not a source.

- [ ] **Step 6: Record the rulings and commit**

Append this task's rulings to the ledger in the same call. Commit only `packages/Phoria/README.md`.

```bash
git add packages/Phoria/README.md
git commit -m "docs(phoria): complete the NuGet landing page"
```

---

### Task 2: The `@phoria/phoria` package README

**Files:**
- Modify: `packages/phoria-islands/README.md` (currently 153 bytes: a title, an emoji line, and the `## Learn more` link Plan A added)
- Create: `/tmp/opencode/ph7-task2-verify.sh`

**Interfaces:**
- Consumes: the `## Learn more` convention from Task 1; the recipe at `packages/phoria-islands/docs/framework-plugin.md`; Task 0's helpers.
- Produces: the `## Install` / `## Use` section shape the six remaining package READMEs follow. The three framework packages in Task 3 and the two in Task 4 differ only in the names inside the commands.

- [ ] **Step 1: Write the check that fails**

- `islands_shape` — H1 is `@phoria/phoria`; the `##` headings are `Install`, `Use`, `Learn more` in that order. Control: a fixture with `Use` before `Install` fails.
- `islands_install_command` — the file contains exactly one install fence, `npm i @phoria/phoria@canary @phoria/phoria-react@canary`, and **both** names resolve against the recorded baseline to the versions in their own `package.json`. The bare name `@phoria/phoria` must not appear as an install command on its own. Two controls, both required: dropping the framework package fails, because the snippet below imports it and a file that installs one thing and imports another does not work; and `npm i @phoria/phoria` alone fails, because it resolves to `0.4.2`, a real published version, so a check that only rejected malformed commands would pass it. The check resolves each name against the baseline and compares against the repository's own `package.json` — it does not pattern-match the command's text. Assert the install-fence count is exactly one, because a file carrying two forms is the failure this catches.
- `islands_use_snippet` — the file contains `import { phoria } from "@phoria/phoria/vite"`, and that export is real: `packages/phoria-islands/src/vite/plugin.ts` line 212 exports `phoriaPlugin as phoria`. Control: `import { phoriaPlugin } from "@phoria/phoria/vite"` fails, because that is the *internal* name and a reader cannot import it.
- `islands_learn_more` — the existing `./docs/framework-plugin.md` link survives, resolves, and its entry has at least thirty characters after the em dash. Control: shortening the entry fails.
- `islands_no_placeholder`, `links_resolve`, `hard_wraps`, and the prose floor (no `#canary`, no `ARCHITECTURE.md`, no count of examples).

- [ ] **Step 2: Run it and watch it fail**

Expected: `islands_shape` fails. `islands_use_snippet` fails — the current file has no snippet. Confirm each failure is a real assertion and not an unbound variable.

- [ ] **Step 3: Write the README**

**H1** is `@phoria/phoria`, per the ruling. The current `# phoria` is neither the directory name nor the published name, and a landing page names the thing the reader installed.

**Purpose**: the framework-agnostic core — the Phoria Vite plugin and the client and server entries every island renders through. When a reader wants it: always, as the base under a framework package, or on its own for a framework-agnostic island.

**Install**: one command, and the second name in it is not decoration:

```shell
npm i @phoria/phoria@canary @phoria/phoria-react@canary
```

`@phoria/phoria` is the framework-agnostic core and cannot render an island on its own — a framework package is what turns components into islands, and installing the core alone leaves the reader to discover the missing half. One sentence says to substitute `@phoria/phoria-vue` or `@phoria/phoria-svelte` for the second name, each linking to that package's own README. One more sentence names the version it resolves to (`0.5.0-beta.1`) so a reader can see what they got; the tag is not decoration either, because `latest` on this package is `0.4.2`, a stable release older than this documentation.

**Use**: a minimal `vite.config.ts`. This one is `examples/getting-started/WebApp/vite.config.ts` verbatim, because a working example's config is known-good and a composed one is a guess:

```ts
import { phoria } from "@phoria/phoria/vite"
import { phoriaReact } from "@phoria/phoria-react/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"

export default defineConfig({
  plugins: [dotnetDevCerts(), phoria(), phoriaReact()],
})
```

Two facts about that block, both derived rather than assumed. All nine examples call `dotnetDevCerts()`, so omitting it here would show a config none of them use. And eight of the nine also carry a `resolve: { tsconfigPaths: true }` block — `getting-started` is the only one that does not, which is why it is the one to copy for a *minimal* config. Say so in one sentence: the `resolve` block is what lets Vite resolve the path alias, and a reader whose own project does not use one does not need it. A snippet that shows a block eight of nine examples use, in a file whose job is to be minimal, teaches the reader to add configuration they do not need.

**Learn more**, three bullets:

- [Framework plugin recipe](./docs/framework-plugin.md) — the full contract for `createPhoriaFrameworkPlugin`, worked through for all three frameworks. *(the existing link, with its entry completed)*
- [Phoria Islands](../../docs/guides/phoria-islands.md) — what an island is, the three rendering modes, and where it sits in the page.
- [Getting started](../../docs/guides/getting-started.md) — adding Phoria to an existing .NET application, step by step.

From `packages/phoria-islands/`, `docs/guides/` is `../../docs/guides/`. Verify with `links_resolve` rather than by reading the path.

- [ ] **Step 4: Run the check and watch it pass**

- [ ] **Step 5: Read the snippet back against the source**

Compare the snippet's imports and call against `packages/phoria-islands/src/vite/plugin.ts` and a working example config. A snippet that names an export the package does not have is the failure mode this file is most exposed to, because the file is a landing page for the export itself.

- [ ] **Step 6: Record the rulings and commit**

```bash
git add packages/phoria-islands/README.md
git commit -m "docs(phoria-islands): complete the npm landing page"
```

---

### Task 3: The three framework package READMEs

**Files:**
- Modify: `packages/phoria-react/README.md` (77 bytes)
- Modify: `packages/phoria-vue/README.md` (73 bytes)
- Modify: `packages/phoria-svelte/README.md` (79 bytes)
- Create: `/tmp/opencode/ph7-task3-verify.sh`

**Interfaces:**
- Consumes: the section shape from Task 2; the recipe at `packages/phoria-islands/docs/framework-plugin.md`.
- Produces: the "one check covers three files" pattern that Task 4 reuses for the two remaining packages.

- [ ] **Step 1: Write one check that covers all three files**

The three files are the same shape with three names, so one check runs per file in a loop, and the loop's input count is asserted — a loop over an empty list exits `0` having checked nothing, and that is the whole defect this step exists to prevent.

- `framework_shape <pkg>` — H1 is the published name (`@phoria/phoria-react`, `@phoria/phoria-vue`, `@phoria/phoria-svelte`); `##` headings are `Install`, `Use`, `Learn more` in that order.
- `framework_install <pkg>` — one install line, `npm i <pkg>@canary`, resolving against the baseline to the version in that package's own `package.json`. The three versions differ (`0.5.0-beta.1`, `0.4.0-beta.1`, `0.4.0-beta.1`) and two of the three `latest` tags are `0.3.2`, so a check that hardcoded one version would pass two files and fail one. **Control: substitute a sibling's version** — writing `0.4.0-beta.1` into the react README must fail. This is the plan's named likely failure, and the control is the only thing that proves the check would catch it.
- `framework_use <pkg>` — the `## Use` snippet imports the package's own plugin from its `/vite` subpath, and that export is real. The three are `phoriaReact` (`packages/phoria-react/src/vite/plugin.ts` line 40), `phoriaVue` (`packages/phoria-vue/src/vite/plugin.ts`, `export { phoriaVue }`) and `phoriaSvelte` (`packages/phoria-svelte/src/vite/plugin.ts` line 39). Controls: writing `phoriaVue` into the react README fails; writing the internal `phoriaReactPlugin` fails; omitting the `/vite` subpath fails.
- `framework_recipe_link <pkg>` — each links `../phoria-islands/docs/framework-plugin.md`, which is Plan A Task 11's requirement, and the link resolves from that package's directory. **The three paths are structurally different from the islands package's own**, and getting them wrong is easy because the correct answer for `phoria-islands` is `./docs/`. Control: substituting the `./docs/framework-plugin.md` form into a framework README fails on resolution.
- `framework_learn_more_complete <pkg>` — every `## Learn more` target is a complete guide, and every entry has at least thirty characters after the em dash. The natural link here is `supported-ui-frameworks.md`, which carries an incomplete notice and is therefore excluded. Control: a fixture linking it fails.
- `framework_example_link <pkg>` — each links its own example, per the spec's companion distinction: *does it work with X, show me X* → the example. The relative path from `packages/<pkg>/` is `../../examples/framework-<x>/`. Control: pointing the react README at `../../examples/framework-vue/` fails.

- [ ] **Step 2: Run it and watch it fail for all three**

Expected: three files, three `framework_shape` failures. Assert the reported file count is three.

- [ ] **Step 3: Write the three READMEs**

Each keeps its existing one-line purpose — they are accurate and specific — and gains the three sections. The purposes:

- `phoria-react`: React components in a Phoria island.
- `phoria-vue`: Vue components in a Phoria island.
- `phoria-svelte`: Svelte components in a Phoria island.

The existing wording is "Use React components in a Phoria Islands for dotnet project." Keep the substance; the plural "Islands" and the lowercase "dotnet" are corrected, because this is a landing page and the copy rules apply to prose this plan writes. Say what the package is *and when a reader would want it*, which the one-liner does not: reach for it when you want your island components written in this framework.

`## Use` is the same config shape as Task 2 with this package's plugin in place of the other — one import from `@phoria/phoria/vite` for `phoria()`, one from the package's own `/vite` subpath, and the plugins array. Take the surrounding shape from the framework's own example: `examples/framework-react/WebApp/vite.config.ts`, `examples/framework-vue/WebApp/vite.config.ts`, `examples/framework-svelte/WebApp/vite.config.ts`.

`## Learn more`, three bullets each:

- The framework plugin recipe — the shared factory, the four-entry pattern, and what this adapter supplies.
- The framework's own example — the package working end to end, with its own e2e suite.
- Phoria Islands — what an island is, the three rendering modes, and where it sits in the page.

The recipe link's path is the exception to the `../../docs/guides/` pattern: `../phoria-islands/docs/framework-plugin.md`.

- [ ] **Step 4: Run the check and watch all three pass**

- [ ] **Step 5: Read each snippet back against its own `src/vite/plugin.ts`**

Three files, three sources. The check resolves the export names; this step confirms the *options* the snippet implies are the ones the composer accepts. `packages/phoria-svelte/src/vite/plugin.ts` sets `ssrExternal: ["@phoria/phoria-svelte/server", "svelte"]` while react and vue set a single entry, and a snippet that shows an option only svelte supports would be a defect the export-name check cannot see.

- [ ] **Step 6: Record the rulings and commit**

```bash
git add packages/phoria-react/README.md packages/phoria-vue/README.md packages/phoria-svelte/README.md
git commit -m "docs(framework-packages): complete the three adapter landing pages"
```

---

### Task 4: The two package READMEs that already have substance

**Files:**
- Modify: `packages/vite-plugin-dotnet-dev-certs/README.md` (874 bytes)
- Modify: `packages/phoria-opentelemetry/README.md` (1096 bytes)
- Create: `/tmp/opencode/ph7-task4-verify.sh`

**Interfaces:**
- Consumes: the section shape from Task 2; the existing prose in both files, which is kept.
- Produces: nothing downstream reads. This is the last package task.

- [ ] **Step 1: Write one check that covers both files**

Both files have the same defect: purpose and substance, no `## Install`, no `## Learn more`. One check, two files, input count asserted.

- `substantive_shape <pkg>` — H1 is the published name; `##` headings are `Install`, then whatever sections the file already had, then `Learn more` last. The dev-certs file keeps `## Linux certificate trust`, so the check asserts `Learn more` is last and `Install` is first, rather than asserting an exact heading list. **A check asserting an exact list would have to encode the pre-existing heading, and a check that encodes the file's current shape cannot catch a section being dropped.**
- `dev_certs_install` — `npm i -D @phoria/vite-plugin-dotnet-dev-certs@canary`, resolving to `0.3.0-beta.0` against the baseline. `-D` is the dev dependency flag and is correct: the plugin is a build-time tool. Control: dropping `-D` fails, and so does substituting the sibling's version.
- `dev_certs_use` — the snippet imports `{ dotnetDevCerts }` from `@phoria/vite-plugin-dotnet-dev-certs`, which is the real export (`packages/vite-plugin-dotnet-dev-certs/src/plugin.ts` line 158, `export { dotnetDevCertsPlugin as dotnetDevCerts }`). Controls: `dotnetDevCertsPlugin` fails; omitting the call `()` fails.
- `dev_certs_linux_section_intact` — the existing `## Linux certificate trust` section survives with its `SSL_CERT_DIR` line and its `dotnet dev-certs https --trust` command. This is a loss check, not a shape check. Control: deleting the `export SSL_CERT_DIR` line fails.
- `otel_install` — `npm i @phoria/opentelemetry@canary`, uniform with the other five. This package used to be the hard case: its `beta` tag sat at its first publish while every later publish was routed to `latest`, so the tag that was right everywhere else was a silent downgrade here. `docs/superpowers/plans/2026-09-29-canary-dist-tag.md` repairs that, which is why the exception is gone. **The control is no longer "the other five's form"** — with all six uniform, that string is correct here and cannot be a control. The controls are the two forms that are wrong for every package: a bare `npm i @phoria/opentelemetry` resolves to `latest`, which is a stable release older than this repository, and a caret `^0.2.0-beta.2` admits versions no guide has described. Both must fail. A check written as "the install line contains a tag" would pass a caret range, so assert the tag is `canary` and that no caret or bare form appears.
- `otel_snippet_intact` — the existing fenced TypeScript snippet survives, including its `parsePhoriaAppSettings`, `createPhoriaObservability`, `withPhoriaOtelInstrumentation` and `shutdown` calls. Loss check. Control: removing the `shutdown` function fails.
- `learn_more_complete <pkg>` — as in Task 3. For opentelemetry the natural targets are `getting-started.md` and `building-for-production.md`, the two guides that mention OpenTelemetry; both are complete. Control: a fixture linking `configuration.md` fails.

- [ ] **Step 2: Run it and watch both files fail**

- [ ] **Step 3: Write the two READMEs**

**dev-certs.** H1 becomes `@phoria/vite-plugin-dotnet-dev-certs`. Purpose gains the "when you would want it" sentence the current one lacks: it configures the ASP.NET Core developer certificate for Vite's dev server, and you want it when you serve the Phoria Web App over HTTPS in development. `## Install` carries the `-D` command and one sentence on what it does. `## Use` is the config line — `plugins: [dotnetDevCerts(), phoria(), phoriaReact()]` — with the import. `## Linux certificate trust` survives **verbatim**, including both `aspire certs trust` and `dotnet dev-certs https --trust`, and moves after `## Use`. `## Learn more` gains the getting-started guide, whose prerequisites section is where a reader meets the same `SSL_CERT_DIR` requirement in a different register — but per the ruling, the link is to the guide as a whole, not to its note, because a link to the note would be the third copy.

**opentelemetry.** H1 is already `@phoria/opentelemetry` and stays. The existing opening sentence is the purpose and stays. `## Install` carries the pinned version and one sentence saying why it is pinned rather than tagged. The existing fenced snippet becomes the body of `## Use`, unchanged. `## Learn more` gains getting-started and building for production, each naming what it gives.

- [ ] **Step 4: Run the check and watch both pass**

- [ ] **Step 5: Read both back against their sources**

The dev-certs plugin's option name and defaults, from `src/plugin.ts`. The opentelemetry exports, from `src/main.ts` and its `package.json` exports map — every symbol in the snippet must be one the package actually exports, because this file is a landing page for those exports.

- [ ] **Step 6: Record the rulings and commit**

```bash
git add packages/vite-plugin-dotnet-dev-certs/README.md packages/phoria-opentelemetry/README.md
git commit -m "docs(packages): add install and learn-more to two package READMEs"
```

---

### Task 5: The eight standalone example READMEs

**Files:**
- Modify: `examples/getting-started/README.md`
- Modify: `examples/framework-react/README.md`
- Modify: `examples/framework-vue/README.md`
- Modify: `examples/framework-svelte/README.md`
- Modify: `examples/framework-multiple/README.md`
- Modify: `examples/with-tailwind/README.md`
- Modify: `examples/with-styled-components/README.md`
- Modify: `examples/with-storybook/README.md`
- Create: `/tmp/opencode/ph7-task5-verify.sh`

**Interfaces:**
- Consumes: `example_dirs`, `webapp_dir`, `webapp_port`, `phoria_server_port` and the port table from Task 0; the `## Learn more` convention from Task 1.
- Produces: the five-section example shape, which Task 6 applies to `with-workspace` and Task 8 asserts across all nine.

- [ ] **Step 1: Snapshot the eight files before editing them**

The loss check needs the pre-slice text. Copy the eight to `/tmp/opencode/ph7-pre/` preserving relative paths, and have the check read from there. A loss check run against the file being written compares the file to itself and passes having verified nothing.

- [ ] **Step 2: Write the checks**

One set of checks, run per file in a loop over the eight, with the loop's input count asserted as eight.

- `example_shape <example>` — the `##` headings are exactly `What it demonstrates`, `Try it`, `Develop it`, `Test it`, `Learn more`, in that order. This is the uniformity the slice exists to produce, so the check asserts an exact ordered list. Control: a fixture with `Develop it` before `Try it` fails.
- `premise_survives <example>` — every non-empty line of the pre-slice README appears in the post-slice README. This is the whole-line loss check, and it is the strongest single check in the plan. Control: deleting one sentence from the post-slice file fails; the check must not be a count comparison, because two deletions and two additions balance in a count and do not in a set.
- `try_it_path_is_self_consistent <example>` — the `giget` command's path segment equals the example's directory name, its target directory equals the same name, and the `cd` in the next line equals that same name. All three agree. Control: making the target directory `frameworks-react` fails; so does making the `cd` name a sibling example.
- `try_it_has_no_canary <example>` — `#canary` appears zero times. The aside lives in two places and this is not one of them. Control: appending the aside fails.
- `ports_match_source <example>` — the `## Develop it` section states the WebApp port from `webapp_port <example>` and the Phoria Server port from `phoria_server_port <example>`, and no other four-digit port in the `5000`–`6000` range appears. The last clause is what catches a neighbour's port appearing *in addition to* the right one. **Controls, both required:** substituting a neighbouring example's port — `5173` for `framework-react`'s `5073` server port — fails; and the `Program.cs` fallback literal is rejected, so writing `5173` for `getting-started`'s server port fails even though the fallback literal in its own source says `5173`. Without that second control the check would be teaching the exact defect this plan exists to catch.
- `develop_commands_exist <example>` — every `pnpm <script>` named in `## Develop it` and `## Test it` is a script in the target `package.json`. Read from `examples/<example>/<webapp_dir>/package.json`, never from the README. Control: naming a plausible neighbour, `pnpm test:unit`, fails.
- `test_it_env_var <example>` — the `## Test it` section names `PHORIA_WEBAPP_URL` and its default equals the WebApp dev port from source. Control: defaulting it to the Docker port `8080` fails, which is a mistake a reader would make and which the section must not encourage.
- `learn_more_targets_are_complete <example>` — every `## Learn more` target is a complete guide, and every entry has at least thirty characters after the em dash. Control: a fixture linking `workspaces.md` or `supported-ui-frameworks.md` fails.
- `example_prose_floors <example>` — no `#canary` beyond the section just checked, no `ARCHITECTURE.md`, no count of examples, no hard wrapping, and `no_placeholder` passes.

- [ ] **Step 3: Run it and watch all eight fail**

Expected: eight `example_shape` failures, since none of the eight has a single `##` heading today. Assert the count is eight and that no other check errored.

- [ ] **Step 4: Write the eight READMEs**

The shape, identical in all eight:

**`## What it demonstrates`** — the pre-slice opening paragraph, byte for byte, and nothing else. This is where the spec puts the per-example detail that makes each feature legible, and rewriting it is how that detail would be lost. The `loss_check` enforces it.

**`## Try it`** — the fence, plus the sentences around it. This is `getting-started`'s, exactly:

```shell
pnpx giget gh:cmeeg/phoria/examples/getting-started getting-started
cd getting-started && docker compose up --build -d
```

Each of the other seven substitutes its own directory name in all three positions: `framework-react`, `framework-vue`, `framework-svelte`, `framework-multiple`, `with-tailwind`, `with-styled-components`, `with-storybook`. The name appears three times in the block — path segment, fetch target, `cd` — and all three are the example's directory name on disk. The owner is lowercase `gh:cmeeg/phoria` because the reader types it; that is the reader-typed form, not a link.

Then one sentence: open <http://localhost:8080>, and stop with `docker compose down`. The Docker port is the same for every example, so only the name changes. No `#canary` aside. One sentence saying the first build pulls the .NET and Node base images, so it takes a few minutes — the root `README.md` and `examples/README.md` both say it, and a landing page that promises a one-minute build is wrong.

**`## Develop it`** — the pre-slice prose about `pnpm install`, `pnpm build` and `pnpm dev`, kept, plus the port sentence. Keep the sentence that names both ports and the Aspire dashboard; it is accurate and the new shape has no better place for it.

**`## Test it`** — the pre-slice prose about `pnpm preview`, `pnpm stop` and `pnpm test:e2e`, kept, and the `PHORIA_WEBAPP_URL` default. The e2e suite's own coverage sentence stays: what each example's suite checks is the per-example detail that distinguishes it.

**`## Learn more`** — two to four complete guides, each entry naming what it gives. Per-example choices:

- All nine: [Getting started](../../docs/guides/getting-started.md) and [Phoria Islands](../../docs/guides/phoria-islands.md).
- Every framework example: [Creating Phoria Island components](../../docs/guides/creating-phoria-island-components.md).
- Every example: [Component register](../../docs/guides/component-register.md), because every one shows a `register.ts`.

The three framework examples must **not** link `supported-ui-frameworks.md`, and `with-workspace` must not link `workspaces.md`. Both are the obvious choice and both are incomplete. `with-workspace` links its own layout note in `getting-started.md` instead, and says the example is the complete workspace layout — which is what the guide's own notice says.

**`with-storybook` specifically.** Its four paragraphs beyond the common three rehome as the ruling says: the Vite pin and its reason, and the `@storybook/addon-essentials` note, join `## What it demonstrates`, byte for byte. The Storybook commands and port `6006` join `## Develop it`, byte for byte. The pin is the reason the example does not build with current Vite, and a reader who loses that sentence will file a bug against an example that is behaving as documented.

- [ ] **Step 5: Run the check and watch all eight pass**

- [ ] **Step 6: Read each file against its source**

Eight read-throughs, per spec:179. For each: the `pnpm` scripts exist in the target `package.json`; the ports match the derivation; the commands run from the directory the section says they run from. `pnpm dev` in these examples is `aspire run`, which starts the WebApp, the Phoria Server and the dashboard — the pre-slice prose's claim, and it is what the development command does.

- [ ] **Step 7: Record the rulings and commit**

```bash
git add examples/getting-started/README.md examples/framework-react/README.md examples/framework-vue/README.md examples/framework-svelte/README.md examples/framework-multiple/README.md examples/with-tailwind/README.md examples/with-styled-components/README.md examples/with-storybook/README.md
git commit -m "docs(examples): give the eight standalone READMEs a uniform shape"
```

---

### Task 6: The `with-workspace` example README

**Files:**
- Modify: `examples/with-workspace/README.md`
- Create: `/tmp/opencode/ph7-task6-verify.sh`

**Interfaces:**
- Consumes: everything from Task 5, plus `webapp_dir` returning `apps/WebApp` for this example.
- Produces: the ninth file in the shape, with commands that differ. Task 7 executes its `## Try it` block.

- [ ] **Step 1: Snapshot the file and write the checks**

Snapshot to `/tmp/opencode/ph7-pre/examples/with-workspace/README.md` alongside Task 5's.

Every check from Task 5 applies, with three differences, and each difference is a check in its own right:

- `workspace_install_from_root` — the `pnpm install` and `pnpm build` in `## Develop it` are stated as run from `examples/with-workspace`, the workspace root, not from the app directory. **Control: moving them under the app directory fails.**
- `workspace_scripts_through_dir` — `dev`, `preview`, `stop` and `test:e2e` are written as `pnpm --dir apps/WebApp <script>`, because the workspace root's `package.json` has only `dev`, `build`, `check` and `lint` and **no** `preview`, `stop` or `test:e2e`. A README that says `pnpm preview` at the root names a script that does not exist. **Control: writing `pnpm preview` without `--dir` fails**, and the check verifies the absence against the root `package.json` rather than assuming it.
- `workspace_ports` — `5673` and `5573`, read through `webapp_port` and `phoria_server_port` with `webapp_dir` returning `apps/WebApp`. The WebApp port is a bare literal in this example's `AppHost/Program.cs` rather than a named variable, so this is the one file where the named-variable reader returns nothing and the literal fallback runs. **The check must assert a non-empty result for all nine examples**, or this file is silently unchecked.

`try_it_path_is_self_consistent` applies unchanged, and the `## Try it` block is *not* different for this example: `docker-compose.yml` is at the example root, so `cd with-workspace && docker compose up --build -d` is the same two commands as the other eight. Uniformity here is a fact about the repository, and Task 7 proves it.

- [ ] **Step 2: Run it and watch it fail**

- [ ] **Step 3: Write the README**

Same five sections, same rules, Task 5's pre-slice prose kept. The `## Learn more` gains the workspace-specific choice: `getting-started.md` for the single-package layout this example is the alternative to, and **not** `workspaces.md`.

Say plainly in `## What it demonstrates` that this is a complete workspace layout that installs, builds and runs standalone. That sentence is what `workspaces.md`'s own notice points readers to, and it is the reason the reader who wanted `workspaces.md` is not being left without an answer.

- [ ] **Step 4: Run the check and watch it pass**

- [ ] **Step 5: Read it back against both `package.json` files**

The root and the app. Every script named in the file must exist in the one the file says to run it from.

- [ ] **Step 6: Record the rulings and commit**

```bash
git add examples/with-workspace/README.md
git commit -m "docs(examples): give the workspace README the uniform shape"
```

---

### Task 7: Execute the `## Try it` path for `with-workspace`

**Files:**
- Create: `/tmp/opencode/ph7-task7-verify.sh`
- Modify: none. A red result sends work back to Task 5, Task 6 or both.

**Interfaces:**
- Consumes: the `## Try it` block written in Task 6.
- Produces: the executed proof that the uniform Docker claim holds for the one example whose layout differs. This is the finding class spec:208 names — sixteen rewrites are individually small and collectively easy to get wrong.

- [ ] **Step 1: State why this task exists, in the ledger, before running it**

The spec's verification table (spec:179) puts example READMEs at read-through, which was correct when they contained no Docker claim. This slice adds one to nine files. Reading cannot catch a command that looks correct and is not — that is the failure mode the whole slice exists to fix, and Plan A Task 8 found it by executing rather than reading. `with-workspace` is the one example whose `Dockerfile` is at depth 3 and whose layout differs, so it is the one where "all nine are the same" is most likely to be false.

- [ ] **Step 2: Write the check as a transcript, not an assertion**

The suite runs the path and records each step's result. It is not a grep over a file; there is nothing to grep. The steps:

| step | command | expected |
| --- | --- | --- |
| fetch | `pnpx giget@latest gh:CMeeg/phoria/examples/with-workspace#canary` | clones into `with-workspace` |
| layout | `ls docker-compose.yml apps/WebApp/Dockerfile apps/WebApp/.dockerignore` | all three present |
| build and up | `docker compose up --build -d` | exit `0` |
| ps | `docker compose ps` | a running container publishing `8080` |
| page | `curl -o /dev/null -w '%{http_code}' localhost:8080` | `200` |
| health | `curl localhost:8080/health` | `Healthy` |
| render | `curl localhost:8080 \| grep '<phoria-island'` | at least one island element |
| teardown | `docker compose down` | container and network removed, `docker compose ps` empty |

The fetch uses the `#canary` ref, because `examples/` is absent from `main` — the same reason Plan A Task 8 fetched from `#canary`. **The `#canary` ref appears in this transcript and in the ledger, and nowhere in the nine READMEs.** The two are different things: one is a verification run against a branch that exists, the other is prose a reader will use after the cut.

- [ ] **Step 3: Run it, from a directory outside the repository**

The fetch creates `with-workspace` in the working directory. Run it from `/tmp/opencode/ph7-tryit/`, which must be empty of that name beforehand, and assert the directory does not already exist rather than deleting it — a pre-existing directory would make the layout step pass against stale files.

- [ ] **Step 4: Read the transcript and report which claims it supports**

State plainly what the run proved and what it did not. Plan A Task 8's build was almost entirely `CACHED`, so the same is likely here; if so, say so in the ledger with the same precision — the wiring, the port, the health endpoint and the rendered output are proven, and a cold multi-stage build is not. **Do not carry a `CACHED` build forward silently.** The ledger line from Plan A Task 13 that says this gap is still open stays open unless this run closes it, and a cached run does not close it.

- [ ] **Step 5: If the run is red, fix the cause and return to the owning task**

A red result here is a finding about the uniform claim, not about one file. If `cd with-workspace && docker compose up --build -d` does not work, every one of the nine `## Try it` sections is wrong, and the fix belongs in the blocks, not in this task. Record which claim failed, which file owns it, and re-run the owning task's suite before recording this one as passing.

- [ ] **Step 6: Record the transcript and commit nothing**

No file in the repository changes. Append the transcript summary and the caching position to the ledger in the same call.

---

### Task 8: Cross-sweep verification, the documentation review, and close-out

**Files:**
- Modify: `docs/MEMORY.md`
- Create: `/tmp/opencode/ph7-task8-verify.sh`

**Interfaces:**
- Consumes: every check from Tasks 1 through 6, plus the pre-slice snapshot from Tasks 5 and 6.
- Produces: the slice's final state, its record, and the handoff.

- [ ] **Step 1: Write the cross-sweep suite and run it over all sixteen files**

The per-file checks run in the writing tasks. This suite runs them again across the whole set at once, because the failure this slice is most likely to produce is one that appears only *between* files: nine READMEs each correct and jointly inconsistent, or seven package READMEs where six name the recipe path in six different forms.

- `all_seventeen_uniform` — every package README has the same ordered `##` shape, and every example README has the same ordered `##` shape. Assert two distinct shapes and sixteen files; a check that accepts one shape everywhere would pass a package README that had been given the example shape.
- `all_sixteen_ports_match_source` — every example's two ports equal the derivation, across all nine, with the input count asserted at nine.
- `all_sixteen_premises_survive` — the whole-line loss check against the pre-slice snapshot, across all nine example READMEs. The package READMEs have no such snapshot, because they are being replaced rather than reshaped, and a loss check against a stub would prove nothing.
- `all_sixteen_learn_more_complete` — every `## Learn more` target in all sixteen files is a complete guide, and every entry clears thirty characters after the em dash.
- `all_sixteen_learn_more_resolve` — every relative link in all sixteen resolves, from its own directory. A package README and an example README resolve from different depths, and a link written for one depth fails in the other.
- `no_cross_document_completeness` — no file in the set contains "incomplete", "work in progress", "coming soon", or a statement about another document's state. The `## Learn more` sections point at guides without characterising them.
- `no_counts_of_examples` — no file in the set contains a numeral adjacent to the word "example" in a tally. Counts of frameworks are fine.
- `no_canary_outside_two_places` — `#canary` appears in the root `README.md` and `examples/README.md` and nowhere in the sixteen. Assert the two, not just the absence.
- `only_markdown_changed` — `git diff --name-only main...HEAD` and `git status --porcelain` list only Markdown. This slice writes no product code, and a `.ts` or `package.json` in the diff means something outside the plan ran.
- `giget_paths_exist_in_repo` — each of the nine `giget` path segments matches a directory that exists under `examples/`. **This is the most this plan can verify about those references.** It does not verify that they resolve, and the check's name says so; a check named `giget_references_resolve` that only checked local paths would be a false claim in the suite itself.

Each check carries a control. Where a control is impossible, the check says why in the ledger rather than being quietly dropped — Plan A deleted a control for a pure predicate self-test because a control would have proved the fixture rather than the function, and recorded why.

- [ ] **Step 2: Run every suite from the slice, not just this one**

Tasks 0 through 8, in order. All green. A suite that has not been run since an earlier task changed a shared helper is not a passing suite.

- [ ] **Step 3: The documentation-review task**

The trigger this slice was built on requires a named docs-review task in every plan, naming the documents it checked. This is it, and it names all sixteen plus the two it did not change:

- **Checked, changed:** the seven package READMEs and the nine example READMEs, each against the source its facts come from — `Phoria.csproj` and the NuGet version list; each package's `package.json` and the recorded registry baseline; each `src/vite/plugin.ts` and `src/plugin.ts` for the exports the snippets name; each example's `AppHost/Program.cs`, `appsettings.json` and `package.json` for its ports, scripts and commands.
- **Checked, unchanged:** the root `README.md` and `examples/README.md`, because the sixteen files point at them and must not contradict them — the Docker port `8080`, the trialist command shape, the `#canary` aside's two locations. The fourteen files in `docs/guides/` were read for link targets and completeness, and none is edited by this slice. `docs/ARCHITECTURE.md` was not touched and no file in the set references it.
- **State that no public surface changed.** The sixteen files are documentation. No export, endpoint, option or script changed, and the packages publish identically before and after.

- [ ] **Step 4: Append the close-out to `docs/MEMORY.md`**

The new entry is the last heading in the file, which is asserted rather than assumed. It records, in this order: the two registry inversions and the install rule they produced; the port derivation and the `phoria.server.port` lowercase key, because a case-sensitive probe of that key reports it absent and will be rediscovered; the `Program.cs` fallback literal that is not the answer; the fact that the spec's stub count is four-and-a-partial rather than five; the fact that the nine `giget` repairs were structural and remain unverifiable until the cut; and what Task 7 proved, including its caching position.

Keep the phrasing anchored, because the next phase's checks will match on it. `MEMORY.md` is the record a future agent reads first, and an entry that paraphrases a fact it is recording invites a second version of the same mistake.

- [ ] **Step 5: Commit**

```bash
git add docs/MEMORY.md
git commit -m "docs: record the README sweep decisions"
```

---

## What no plan covers

- **The `canary` → `main` cut.** Unchanged from Plan A and still unperformable from an agent session: `gh` is unavailable, so the version pull requests cannot be opened or merged here. Until the cut, `examples/` is absent from `main` and the nine `giget` references stay unverifiable however correct they are. It is step 6 of the runbook in `CONTRIBUTING.md`, run by the maintainer performing the cut. **Executing this plan does not finish Phase 6 either.**
- **The `@phoria/opentelemetry` dist-tag inversion.** Its `beta` tag is older than its `latest`, and its `latest` is a prerelease. Plan B writes around it by pinning the version. Re-tagging is a publishing action, it affects every consumer, and it is not a documentation change. Flag it, do not fix it here.
- **Whether the documentation-review trigger worked.** Carried from Plan A, unchanged and still unreported. Nothing in this slice observes its own efficacy, and the honest position is the one Plan A recorded: it stays unknown until the next phase has been under way for a while.

## Self-Review

**1. Spec coverage.** `## Package READMEs` (spec:119) — purpose, install, use, learn more, on all seven: Tasks 1 through 4, with `Phoria.Tests`'s deliberate absence stated in the Rulings. The package `docs` folder convention (spec:125) is unchanged, the existing recipe link is preserved and extended, and Tasks 3 and 4 add no second `docs` file. `## Example READMEs` (spec:131) — the five-section order on all nine: Tasks 5 and 6, with the opening paragraph kept byte for byte and the per-file exception in the spec not arising, because all nine are at Docker parity. The `giget` repair deferred here by Plan A (spec:174, Plan A:44) lands in Task 5's `## Try it`. The `#canary` exclusion from the nine (spec:99) is a global constraint and a check. The companion distinction (spec:165) — *does it work with X* → the example — is a check in Task 3 and a link in Task 5. The document placement rule (spec:157) is honoured by construction: the slice edits only files that belong where they are. The verification table (spec:167) is Task 1's read-through, Task 5's read-through, and Task 7's execution, which the table has no row for and Finding 4 records.

**2. Placeholder scan.** No `TBD`, no "similar to Task N", and no unresolved template. Task 5's `## Try it` fence was a `<example>` template until this review replaced it with `getting-started`'s real block and the seven names that substitute into it; the remaining angle-bracket tokens are check-name parameter notation (`example_shape <example>`), not content. Every command, version, path and port in the plan is a value derived during this planning session and recorded here. Task 5's `## Learn more` bullets are the concrete set; anything a file adds beyond them is the executor's judgement, and the checks in `learn_more_targets_are_complete` and the thirty-character rule are what constrain it.

**5. A defect this review found in the plan itself, recorded because the class recurs.** Task 1's `## Use` snippet was first written as `builder.Services.AddPhoria(builder.Configuration)`. The real signature is `AddPhoria(this IServiceCollection, Action<PhoriaOptions>? configure = null)` — it binds its own configuration section and takes no `IConfiguration`. The snippet would not have compiled, and it was written by composing a plausible .NET registration call from memory rather than by reading `ServiceCollectionExtensions.cs`, which the same step instructs the executor to read. A plan that carries a wrong code sample is worse than one that carries none, because the sample is the part the executor copies without checking. **Every code block in a plan is read against its source before the plan is saved, not after.**

**3. Type consistency.** `webapp_port` and `phoria_server_port` are defined once in Task 0 and called with one argument, an example directory name, in Tasks 0, 5, 6 and 8. `webapp_dir` is called by both port functions and by `develop_commands_exist`, always with the same argument shape. `registry_baseline` is a lookup from package name to expected resolved version, consulted by `islands_install_command`, `framework_install`, `dev_certs_install` and `otel_install`, and by nothing else. `premise_survives` reads from `/tmp/opencode/ph7-pre/`, which Tasks 5 and 6 populate before their suites run and Task 8 re-reads.

**4. Review Focus.** Each of the five lines has a control, and the controls are the hard part: a neighbouring example's port, a sibling package's version, the `Program.cs` fallback literal, a dropped sentence, an incomplete-guide target, a mismatched `giget` path, and — for the install lines — the bare-name and caret-range forms that are wrong for all six packages. The `@phoria/opentelemetry` dist-tag inversion used to be the sharpest of these, a form that was correct in five files and a downgrade in the sixth; it is now repaired by the dist-tag plan, and the control that caught it is gone with it. The replacement controls are the ones that are wrong everywhere, which is a weaker trap but the only one left.
