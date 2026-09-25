# Script Logging (emoji + colour) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the repo scripts' own log messages stand out from raw tool output with emoji prefixes, ANSI colour, and blank-line separation, applied consistently to every mode of `scripts/examples.js` and `scripts/dotnet/publish.js`.

**Architecture:** A tiny shared `scripts/log.js` helper (node built-ins only) exports styled logging functions gated on TTY + `NO_COLOR`/`FORCE_COLOR` so CI logs stay escape-free. Each script imports it, replaces its `console.log`/`console.error` messages with styled calls, and echoes each spawned tool command as a dim `$ …` line from `run()`.

**Tech Stack:** Node ESM, no new dependencies, Biome (tabs, no semicolons, no trailing commas, line width 120).

## Global Constraints

- No new dependencies — node built-ins only.
- Colour must be disabled when output is not a TTY (CI), and respect `NO_COLOR`/`FORCE_COLOR`. Emojis are always emitted.
- Emoji per mode: link `🔗`, refresh `🔄`, sync `↩️`, bump `⬆️`, check `🔎` (result `✅`/`❌`), pack `📦`, push `🚀`.
- Style per `scripts/log.js` API (single source of truth — all other tasks import from it):
  - `step(text)` — bold cyan, leading blank line (caller includes emoji)
  - `success(text)` — green with `✅` prefix, blank lines around
  - `error(text)` — red with `❌` prefix to **stderr**, blank lines around
  - `info(text)` — dim, leading blank line
  - `command(text)` — dim `$ <text>`, leading blank line
- Tool output (`run()` stdout/stderr) stays unstyled — that is the contrast the user wants.
- No Changeset: these are internal repo scripts, not published packages.
- `pnpm biome check scripts` must pass after each task.

---

### Task 1: Create `scripts/log.js`

**Files:**
- Create: `scripts/log.js`

**Interfaces:**
- Produces: named exports `step`, `success`, `error`, `info`, `command` (signatures above). Later tasks import exactly these names from `./log.js` (examples) and `../log.js` (publish).

- [ ] **Step 1: Write `scripts/log.js`**

```js
const streamEnabled = (stream) =>
	process.env.FORCE_COLOR
		? process.env.FORCE_COLOR !== "0"
		: !process.env.NO_COLOR && Boolean(stream.isTTY)

function paint(stream, code, text) {
	return streamEnabled(stream) ? `\x1b[${code}m${text}\x1b[0m` : text
}

export function step(text) {
	console.log(`\n${paint(process.stdout, "1;36", text)}`)
}

export function success(text) {
	console.log(`\n${paint(process.stdout, "32", `✅ ${text}`)}\n`)
}

export function error(text) {
	console.error(`\n${paint(process.stderr, "31", `❌ ${text}`)}\n`)
}

export function info(text) {
	console.log(`\n${paint(process.stdout, "2", text)}`)
}

export function command(text) {
	console.log(`\n${paint(process.stdout, "2", `$ ${text}`)}`)
}
```

- [ ] **Step 2: Smoke-test the module**

Run: `node -e "import('./scripts/log.js').then(m => { m.step('🔗 Test'); m.success('done'); m.error('boom') })"`
Expected: three messages, separated by blank lines, `✅ done` on stdout and `❌ boom` on stderr (no escape codes visible when piped).

- [ ] **Step 3: Lint**

Run: `pnpm biome check scripts/log.js`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/log.js
git commit -m "feat(scripts): add shared emoji/colour log helpers"
```

---

### Task 2: Restyle `scripts/examples.js`

**Files:**
- Modify: `scripts/examples.js`

**Interfaces:**
- Consumes: `step`, `success`, `error`, `info`, `command` from `./log.js`.
- Produces: styled output for all five modes (`link`, `refresh`, `sync`, `check`, `bump`) plus the usage message.

- [ ] **Step 1: Add the import**

Add after the `import { promisify } from "node:util"` line:

```js
import { command, error, info, step, success } from "./log.js"
```

- [ ] **Step 2: Echo commands in `run()`**

Replace the `run` function (currently lines ~22-32) with:

```js
async function run(cmd, cwd = root) {
	command(cmd)

	const { stdout, stderr } = await execAsync(cmd, { cwd })

	if (stdout) {
		console.log(stdout)
	}

	if (stderr) {
		console.error(stderr)
	}
}
```

- [ ] **Step 3: Style the mode messages**

Replace each `console.log`/`console.error` call per the table (emoji and wording verbatim):

| Location | Current | New |
|---|---|---|
| `link` top | — | `step(\`🔗 Linking ${exampleDir}…\`)` |
| `link` csproj already linked | `console.log(\`${exampleDir}: already linked\`)` | `info(\`🔗 ${exampleDir}: already linked\`)` |
| `link` footer (multi-line string, unchanged text) | `console.log(...)` | see code block below |
| `refresh` top | — | `step(\`🔄 Refreshing hard links for ${exampleDir}…\`)` |
| `refresh` footer | `console.log(\`Refreshed hard links for ${exampleDir}.\`)` | `success(\`Refreshed hard links for ${exampleDir}.\`)` |
| `sync` top | — | `step(\`↩️ Restoring ${exampleDir} to its committed state…\`)` |
| `sync` lockfile note | `console.log("Restored the root pnpm-lock.yaml (…)")` | `info("Restored the root pnpm-lock.yaml (it was modified by link-induced catalog: churn).")` |
| `sync` footer | `console.log(\`Restored ${exampleDir} to its committed state.\`)` | `success(\`Restored ${exampleDir} to its committed state.\`)` |
| `check` top | — | `step(\`🔎 Checking ${exampleDir}…\`)` |
| `check` problems | `console.error(problems.join("\n"))` | see code block below |
| `check` OK | `console.log(\`${exampleDir}: OK\`)` | `success(\`${exampleDir}: OK\`)` |
| `bump` top | — | `step(\`⬆️ Bumping ${exampleDir}…\`)` |
| `bump` footer | `console.log(\`Bumped ${exampleDir} to ${versions.join(", ")} (registry refs).\`)` | `success(\`Bumped ${exampleDir} to ${versions.join(", ")} (registry refs).\`)` |
| usage error | `console.error("Usage: …")` | `error("Usage: node scripts/examples.js <link|sync|check|bump|refresh>")` |

The `link` footer keeps its existing instructional text (line ~198-202) but wraps it in `success`:

```js
success(
	`Linked ${exampleDir} to local packages (file: refs, catalog literalized).\n` +
		`Run \`pnpm build\` at the repo root first. After every rebuild, run \`pnpm examples:refresh\` to refresh the hard links (a plain \`pnpm install\` does not).\n` +
		`Run \`pnpm examples:sync\` before committing.`
)
```

The `check` problems branch (currently `console.error(problems.join("\n"))`) becomes:

```js
error(`${exampleDir}: found ${problems.length} problem(s):\n${problems.join("\n")}`)
```

- [ ] **Step 4: Lint**

Run: `pnpm biome check scripts/examples.js scripts/log.js`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add scripts/examples.js
git commit -m "feat(scripts): restyle examples.js output with emoji and colour"
```

---

### Task 3: Restyle `scripts/dotnet/publish.js`

**Files:**
- Modify: `scripts/dotnet/publish.js`

**Interfaces:**
- Consumes: `command`, `error`, `step` from `../log.js`.
- Produces: styled pack/push steps and a clean styled failure instead of a thrown `Error`.

- [ ] **Step 1: Add the import**

Add after the `const exec = util.promisify(child_process.exec)` line:

```js
import { command, error, step } from "../log.js"
```

- [ ] **Step 2: Echo commands and style failures in `run()`**

Replace the `run` function (currently lines ~18-28) with:

```js
async function run(cmd) {
	command(cmd)

	const { stdout, stderr } = await exec(cmd)

	if (stdout) {
		console.log(stdout)
	}

	if (stderr) {
		error(stderr.trim())
		process.exit(1)
	}
}
```

- [ ] **Step 3: Add step announcements around the two tool invocations**

Before the `dotnet pack` call (line ~104):

```js
step(`📦 Packing ${cwd} (v${version})…`)
```

Before the `dotnet nuget push` call (line ~110):

```js
step(`🚀 Pushing Phoria v${version} to ${packageSourceName}…`)
```

- [ ] **Step 4: Lint**

Run: `pnpm biome check scripts/dotnet/publish.js scripts/log.js`
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add scripts/dotnet/publish.js
git commit -m "feat(scripts): restyle publish.js output with emoji and colour"
```

---

### Task 4: Verify end-to-end output

**Files:**
- None (read-only verification; `examples:link` mutates then `examples:sync` restores).

- [ ] **Step 1: Check mode (non-mutating)**

Run: `pnpm examples:check`
Expected: each example announced as `🔎 Checking …`, then either `✅ …: OK` or a `❌ …: found N problem(s):` block on stderr with exit code 1 if the example is currently linked.

- [ ] **Step 2: Eyeball link output, then restore**

Run: `pnpm examples:link`
Expected: for each example — a `🔗 Linking …` heading, dim `$ pnpm install` / `$ dotnet restore` lines interleaved with plain tool output, then a `✅ Linked …` summary block.

Then run: `pnpm examples:sync`
Expected: `↩️ Restoring …` headings and `✅ Restored …` blocks, with the examples back on committed registry refs.

- [ ] **Step 3: Confirm clean state**

Run: `git status --porcelain examples/`
Expected: empty (the link/sync round-trip left no drift).

- [ ] **Step 4: Full script lint**

Run: `pnpm biome check scripts`
Expected: no errors.

- [ ] **Step 5: Commit any final formatting drift**

```bash
git add -A
git commit -m "chore(scripts): finalise logging restyle"
```
