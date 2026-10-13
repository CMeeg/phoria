# Standalone Examples with Local-Dev Linking — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make committed phoria examples reference published package versions (fully standalone), restore `catalog:` in the JS packages, and provide a link/sync script for local development — guarded by CI and kept current by the release flow.

**Architecture:** A single Node script (`scripts/examples.js`) owns the mapping between the phoria package names and their in-repo directories, and implements four modes: `link` (switch an example to `link:` refs + `ProjectReference`), `sync` (restore the committed registry refs from HEAD + the committed lockfile), `check` (CI guard that fails on committed `link:`/`file:`/`ProjectReference`), and `bump` (rewrite refs to the current in-repo versions, used by the release flow post-publish). Committed examples use registry ranges; devs run `link`/`sync` around local work; CI runs `check`; release runs `bump` after publish.

**Tech Stack:** Node 24 (ESM), pnpm 11.17, GitHub Actions, changesets.

## Global Constraints

- **No git commits.** The user explicitly said "don't commit anything" — every task ends with verification, not a commit. Leave changes uncommitted for review.
- Root code style: Biome with tabs, as-needed semicolons, no comments. Script must pass `pnpm biome check`.
- Example `package.json` rewrites must be 2-space indented JSON (matches the examples' own `biome.jsonc`; example `pnpm lint` checks JSON).
- Build before lint/check/test (repo CI order). `pnpm build` at the repo root before exercising `examples:link` (linked packages need `dist`).
- Published baseline versions: `@phoria/phoria` 0.4.2, `@phoria/phoria-react` 0.4.2, `@phoria/vite-plugin-dotnet-dev-certs` 0.2.1, `Phoria` (NuGet) 0.4.2.
- Do not add unit tests for `scripts/examples.js` (deferred in the spec); verification is integration-based (real example round-trip + CI guard).

## Amendments (recorded during Task 2 execution)

- **Central Package Management (CPM).** Examples enable `ManagePackageVersionsCentrally`
  (`examples/getting-started/Directory.Packages.props`), so NuGet rejects a
  `PackageReference` that carries a `Version` (NU1008). The registry version lives in
  the example's `Directory.Packages.props` as
  `<PackageVersion Include="Phoria" Version="..." />`; the csproj carries an
  unversioned `<PackageReference Include="Phoria" />`. This amends the Task 1 script
  code below (adds `packageVersion()`, `packagesPropsPath()`, a
  `Directory.Packages.props` check in `check`, and props handling in `bump`/`sync`)
  and the expectations in Tasks 2 and 6. `link` never touches
  `Directory.Packages.props` (an unused `PackageVersion` is harmless), so `sync` only
  restores it when `bump` or a manual edit changed it.
- **WIP-deferred verification.** During active development an example may legitimately
  reference APIs that are not yet published. `getting-started`'s `ui/src/server.ts`
  uses the `logger` option and the `PhoriaLogger` type, which exist only in the
  working tree (`packages/phoria-islands/src/server/routing.ts:157,205`) — published
  0.4.2 only exposes `cwd` in the handler options. The committed registry state
  therefore cannot pass `pnpm check` until a release publishes those APIs. This blocks
  only **verification** (Task 2 Step 2, Task 6); goals, design, and the CI guard
  (`examples:check` checks refs only and never builds) are unaffected. Re-run the
  deferred steps after the next release that includes the example's APIs.

---

## File Structure

- Create: `scripts/examples.js` — the link/sync/check/bump CLI. Owns the package-name → dir mapping.
- Modify: `package.json` (root) — add `examples:link`, `examples:sync`, `examples:check`, `examples:bump` scripts.
- Modify: `examples/getting-started/WebApp/package.json`, `examples/getting-started/WebApp/WebApp.csproj`, `examples/getting-started/WebApp/pnpm-lock.yaml`, `examples/getting-started/Directory.Packages.props` — committed registry state.
- Modify: `packages/{phoria-islands,phoria-react,phoria-svelte,phoria-vue,vite-plugin-dotnet-dev-certs}/package.json` — runtime deps back to `catalog:`.
- Modify: `pnpm-lock.yaml` (root) — re-synced after the revert.
- Modify: `.github/workflows/ci.yml` — add `pnpm examples:check` step.
- Modify: `.github/workflows/release.yml` — add post-publish example sync step.
- Modify: `AGENTS.md` — remove literal-deps gotcha; rewrite Examples section.
- Modify: `examples/TODO.md` — update deferred items.

---

## Task 1: Create `scripts/examples.js` and wire root scripts

**Files:**
- Create: `scripts/examples.js`
- Modify: `package.json` (root, `scripts` block, lines 12-20)

**Interfaces:**
- Produces: `node scripts/examples.js <link|sync|check|bump>` — scans `examples/*/WebApp` for `package.json`, applies the mode to each, exits 1 on failure (check) or bad usage.
- Produces: root scripts `pnpm examples:link`, `pnpm examples:sync`, `pnpm examples:check`, `pnpm examples:bump`.
- Later tasks consume: `check` (Task 4 CI), `bump` (Task 2 conversion + Task 4 release), `link`/`sync` (Task 6 verification).

- [ ] **Step 1: Write `scripts/examples.js`**

```js
import { exec } from "node:child_process"
import { existsSync, readdirSync } from "node:fs"
import { readFile, writeFile } from "node:fs/promises"
import { dirname, join, relative, resolve } from "node:path"
import { fileURLToPath } from "node:url"
import { promisify } from "node:util"

const execAsync = promisify(exec)
const scriptDir = dirname(fileURLToPath(import.meta.url))
const root = resolve(scriptDir, "..")

const jsPackages = {
	"@phoria/phoria": "packages/phoria-islands",
	"@phoria/phoria-react": "packages/phoria-react",
	"@phoria/vite-plugin-dotnet-dev-certs": "packages/vite-plugin-dotnet-dev-certs",
}

const dotnetPackage = { name: "Phoria", dir: "packages/Phoria", csproj: "Phoria.csproj" }

async function run(command, cwd = root) {
	const { stdout, stderr } = await execAsync(command, { cwd })

	if (stdout) {
		console.log(stdout)
	}

	if (stderr) {
		console.error(stderr)
	}
}

async function readJson(path) {
	return JSON.parse(await readFile(path, "utf8"))
}

async function writeJson(path, value) {
	await writeFile(path, `${JSON.stringify(value, null, 2)}\n`)
}

async function getHead(path) {
	const rel = relative(root, path)
	const { stdout } = await execAsync(`git show HEAD:${rel}`, { cwd: root })

	return stdout
}

function findExamples() {
	const dir = join(root, "examples")

	if (!existsSync(dir)) {
		return []
	}

	return readdirSync(dir)
		.filter((name) => existsSync(join(dir, name, "WebApp", "package.json")))
		.map((name) => join(dir, name, "WebApp"))
}

function csprojPath(exampleDir) {
	return join(exampleDir, "WebApp.csproj")
}

function packagesPropsPath(exampleDir) {
	return resolve(exampleDir, "..", "Directory.Packages.props")
}

function projectReference() {
	return `<ProjectReference Include="../../../${dotnetPackage.dir}/${dotnetPackage.csproj}" />`
}

function packageReference() {
	return `<PackageReference Include="${dotnetPackage.name}" />`
}

function packageVersion(version) {
	return `<PackageVersion Include="${dotnetPackage.name}" Version="${version}" />`
}

async function link(exampleDir) {
	const pkgPath = join(exampleDir, "package.json")
	const pkg = await readJson(pkgPath)

	for (const [name, dir] of Object.entries(jsPackages)) {
		const section = pkg.dependencies?.[name] ? "dependencies" : pkg.devDependencies?.[name] ? "devDependencies" : null

		if (section) {
			pkg[section][name] = `link:../../../${dir}`
		}
	}

	await writeJson(pkgPath, pkg)

	const csproj = await readFile(csprojPath(exampleDir), "utf8")

	if (csproj.includes(projectReference())) {
		console.log(`${exampleDir}: already linked`)
	} else {
		const match = csproj.match(/<PackageReference Include="Phoria" \/>/)

		if (!match) {
			throw new Error(`No Phoria PackageReference found in ${csprojPath(exampleDir)}`)
		}

		await writeFile(csprojPath(exampleDir), csproj.replace(match[0], projectReference()))
	}

	await run("pnpm install", exampleDir)
	await run("dotnet restore WebApp.csproj", exampleDir)

	console.log(`Linked ${exampleDir} to local packages. Run \`pnpm build\` at the repo root first, and \`pnpm examples:sync\` before committing.`)
}

async function sync(exampleDir) {
	const pkgPath = join(exampleDir, "package.json")
	const headPkg = JSON.parse(await getHead(pkgPath))
	const pkg = await readJson(pkgPath)

	for (const [name] of Object.entries(jsPackages)) {
		for (const section of ["dependencies", "devDependencies"]) {
			if (pkg[section]?.[name]) {
				const headSpec = headPkg[section]?.[name]

				if (!headSpec) {
					throw new Error(`HEAD has no ${name} in ${section}; cannot restore ${exampleDir}`)
				}

				pkg[section][name] = headSpec
			}
		}
	}

	await writeJson(pkgPath, pkg)

	const csproj = await readFile(csprojPath(exampleDir), "utf8")
	const headCsproj = await getHead(csprojPath(exampleDir))
	const headRef =
		headCsproj.match(/<ProjectReference Include="[^"]+" \/>/)?.[0] ??
		headCsproj.match(/<PackageReference Include="Phoria" \/>/)?.[0]
	const currentRef =
		csproj.match(/<ProjectReference Include="[^"]+" \/>/)?.[0] ??
		csproj.match(/<PackageReference Include="Phoria" \/>/)?.[0]

	if (!headRef) {
		throw new Error(`HEAD has no Phoria reference; cannot restore ${exampleDir}`)
	}

	if (currentRef && currentRef !== headRef) {
		await writeFile(csprojPath(exampleDir), csproj.replace(currentRef, headRef))
	}

	const propsPath = packagesPropsPath(exampleDir)
	const props = await readFile(propsPath, "utf8")
	const headPv = (await getHead(propsPath)).match(
		/[ \t]*<PackageVersion Include="Phoria" Version="[^"]*" \/>\r?\n?/
	)?.[0]
	const currentPv = props.match(/[ \t]*<PackageVersion Include="Phoria" Version="[^"]*" \/>\r?\n?/)?.[0]

	if (currentPv && headPv && currentPv !== headPv) {
		await writeFile(propsPath, props.replace(currentPv, headPv))
	} else if (headPv && !currentPv) {
		await writeFile(propsPath, props.replace(/<ItemGroup>/, `<ItemGroup>\n\t\t${headPv.trim()}`))
	} else if (currentPv && !headPv) {
		await writeFile(propsPath, props.replace(currentPv, ""))
	}

	await run(`git checkout -- ${relative(root, join(exampleDir, "pnpm-lock.yaml"))}`)
	await run("pnpm install --frozen-lockfile", exampleDir)

	console.log(`Restored ${exampleDir} to its committed state.`)
}

async function check(exampleDir) {
	const problems = []

	const pkgPath = join(exampleDir, "package.json")
	const pkg = await readJson(pkgPath)

	for (const [name] of Object.entries(jsPackages)) {
		for (const section of ["dependencies", "devDependencies"]) {
			const spec = pkg[section]?.[name]

			if (spec && (spec.startsWith("link:") || spec.startsWith("file:"))) {
				problems.push(`${pkgPath}: ${name} (${section}) is "${spec}" — use a registry range`)
			}
		}
	}

	const csproj = await readFile(csprojPath(exampleDir), "utf8")

	if (csproj.includes(projectReference())) {
		problems.push(`${csprojPath(exampleDir)}: uses a Phoria ProjectReference — use a PackageReference`)
	}

	const props = await readFile(packagesPropsPath(exampleDir), "utf8")

	if (!/<PackageVersion Include="Phoria" Version="[0-9]/.test(props)) {
		problems.push(`${packagesPropsPath(exampleDir)}: missing a registry Phoria PackageVersion`)
	}

	if (problems.length) {
		console.error(problems.join("\n"))
		process.exitCode = 1
	} else {
		console.log(`${exampleDir}: OK`)
	}
}

async function bump(exampleDir) {
	const pkgPath = join(exampleDir, "package.json")
	const pkg = await readJson(pkgPath)
	const versions = []

	for (const [name, dir] of Object.entries(jsPackages)) {
		const { version } = await readJson(join(root, dir, "package.json"))
		versions.push(`${name}@^${version}`)

		for (const section of ["dependencies", "devDependencies"]) {
			if (pkg[section]?.[name]) {
				pkg[section][name] = `^${version}`
			}
		}
	}

	await writeJson(pkgPath, pkg)

	const { version: dotnetVersion } = await readJson(join(root, dotnetPackage.dir, "package.json"))
	const csproj = await readFile(csprojPath(exampleDir), "utf8")
	const ref = csproj.includes(projectReference())
		? projectReference()
		: csproj.match(/<PackageReference Include="Phoria" \/>/)?.[0]

	if (!ref) {
		throw new Error(`No Phoria reference found in ${csprojPath(exampleDir)}`)
	}

	await writeFile(csprojPath(exampleDir), csproj.replace(ref, packageReference()))

	const propsPath = packagesPropsPath(exampleDir)
	const props = await readFile(propsPath, "utf8")
	const versionLine = packageVersion(dotnetVersion)

	if (/<PackageVersion Include="Phoria" Version="[^"]*" \/>/.test(props)) {
		await writeFile(propsPath, props.replace(/<PackageVersion Include="Phoria" Version="[^"]*" \/>/, versionLine))
	} else {
		await writeFile(propsPath, props.replace(/<ItemGroup>/, `<ItemGroup>\n\t\t${versionLine}`))
	}

	await run("pnpm install", exampleDir)

	console.log(`Bumped ${exampleDir} to ${versions.join(", ")} (registry refs).`)
}

const modes = { link, sync, check, bump }
const mode = process.argv[2]

if (!modes[mode]) {
	console.error("Usage: node scripts/examples.js <link|sync|check|bump>")
	process.exit(1)
}

for (const exampleDir of findExamples()) {
	await modes[mode](exampleDir)
}
```

- [ ] **Step 2: Verify the script lints**

Run: `pnpm biome check scripts/examples.js`
Expected: no errors.

- [ ] **Step 3: Verify `check` fails on the current (file:-based) example**

Run: `node scripts/examples.js check`
Expected: exits 1, printing that `@phoria/phoria` (dependencies) and `@phoria/phoria-react` (dependencies) and `@phoria/vite-plugin-dotnet-dev-certs` (devDependencies) use `file:`, and that `WebApp.csproj` uses a ProjectReference.

- [ ] **Step 4: Add the root npm scripts**

Edit `package.json` (root) `scripts`:

```json
"examples:link": "node scripts/examples.js link",
"examples:sync": "node scripts/examples.js sync",
"examples:check": "node scripts/examples.js check",
"examples:bump": "node scripts/examples.js bump",
```

- [ ] **Step 5: Verify the `link` → `sync` round-trip on the current example**

Run: `pnpm examples:link`
Expected: `package.json` now has `link:../../../packages/phoria-islands`, `link:../../../packages/phoria-react`, `link:../../../packages/vite-plugin-dotnet-dev-certs`; `WebApp.csproj` has a `ProjectReference`; `pnpm install` and `dotnet restore` succeed in the example.

Run: `pnpm examples:sync`
Expected: refs restored to HEAD values (`file://...`), `pnpm-lock.yaml` restored via git checkout, frozen install succeeds.

Run: `git status` on the example directory
Expected: clean (no diff vs HEAD).

---

## Task 2: Convert `getting-started` to committed registry state

**Files:**
- Modify: `examples/getting-started/WebApp/package.json`
- Modify: `examples/getting-started/WebApp/WebApp.csproj`
- Modify: `examples/getting-started/WebApp/pnpm-lock.yaml`
- Modify: `examples/getting-started/Directory.Packages.props`

**Interfaces:**
- Consumes: `pnpm examples:bump` from Task 1.
- Produces: committed state consumed by Tasks 3, 4, 6.

- [ ] **Step 1: Run the bump to write registry refs**

Run: `pnpm examples:bump`
Expected: `WebApp/package.json` now has `"@phoria/phoria": "^0.4.2"` (dependencies), `"@phoria/phoria-react": "^0.4.2"` (dependencies), `"@phoria/vite-plugin-dotnet-dev-certs": "^0.2.1"` (devDependencies); `WebApp.csproj` has an unversioned `<PackageReference Include="Phoria" />`; `Directory.Packages.props` has `<PackageVersion Include="Phoria" Version="0.4.2" />`; example `pnpm install` regenerates the lockfile resolving those registry versions.

- [ ] **Step 2: Verify the committed state installs and builds standalone**

Run (in `examples/getting-started/WebApp`): `pnpm install --frozen-lockfile`
Expected: succeeds (verified 2026-08-06).

Run (in `examples/getting-started/WebApp`): `pnpm check && pnpm lint && pnpm build`
Expected: all green. **DEFERRED** — blocked by the unreleased `logger` API (see Amendments): `ui/src/server.ts` uses `logger`/`PhoriaLogger`, absent from published 0.4.2. `pnpm build`'s `build:webapp` step was failing on the pre-CPM-fix NU1008 and now restores cleanly.

- [ ] **Step 3: Verify the CI guard passes on the committed state**

Run: `pnpm examples:check`
Expected: prints `...WebApp: OK` for getting-started, exits 0.

- [ ] **Step 4: Confirm the example is self-contained**

Run: `grep -c "file:" examples/getting-started/WebApp/pnpm-lock.yaml`
Expected: 0 matches.

---

## Task 3: Revert packages to `catalog:` and remove the gotcha

**Files:**
- Modify: `packages/phoria-islands/package.json` (dependencies block, lines 65-71)
- Modify: `packages/phoria-react/package.json` (dependencies block, lines 64-67)
- Modify: `packages/phoria-svelte/package.json` (dependencies block, lines 63-66)
- Modify: `packages/phoria-vue/package.json` (dependencies block, lines 63-66)
- Modify: `packages/vite-plugin-dotnet-dev-certs/package.json` (dependencies block, lines 47-51)
- Modify: `AGENTS.md` (remove the literal-deps gotcha bullet, line 123)
- Modify: `pnpm-lock.yaml` (root, regenerated)

**Interfaces:**
- Consumes: Task 2 (the example no longer uses `file:`, so reverting is safe).
- Produces: `catalog:` runtime deps consumed by Task 6 verification (link against catalog-based packages).

- [ ] **Step 1: Revert the five dependency blocks to `catalog:`**

`packages/phoria-islands/package.json`:
```json
	"dependencies": {
		"defu": "^6.1.7",
		"destr": "catalog:",
		"empathic": "catalog:",
		"h3": "catalog:",
		"mime": "^4.1.0"
	},
```

`packages/phoria-react/package.json`, `packages/phoria-svelte/package.json`, `packages/phoria-vue/package.json`:
```json
	"dependencies": {
		"@rollup/pluginutils": "catalog:",
		"magic-string": "catalog:"
	},
```

`packages/vite-plugin-dotnet-dev-certs/package.json`:
```json
	"dependencies": {
		"destr": "catalog:",
		"empathic": "catalog:",
		"tinyexec": "^1.2.4"
	},
```

- [ ] **Step 2: Remove the AGENTS.md gotcha bullet**

Delete the line:

```markdown
- **JS package runtime `dependencies` must use literal semver ranges, never `catalog:`.** `examples/` installs the packages via `file:`, and pnpm errors on `catalog:` specs in packages outside the workspace (it resolves the raw manifest). devDependencies may keep using `catalog:`.
```

- [ ] **Step 3: Re-sync the root lockfile**

Run: `pnpm install --no-frozen-lockfile`
Expected: succeeds; specifiers in `pnpm-lock.yaml` importers revert to `catalog:`.

Run: `pnpm install --frozen-lockfile`
Expected: "Already up to date".

- [ ] **Step 4: Verify the packages still lint and check**

Run: `pnpm lint && pnpm check`
Expected: all packages pass.

---

## Task 4: CI guard and release flow

**Files:**
- Modify: `.github/workflows/ci.yml` (build-and-test job, after "Install dependencies", line 46)
- Modify: `.github/workflows/release.yml` (after the changesets step, before "Push release tags")

**Interfaces:**
- Consumes: `pnpm examples:check` and `pnpm examples:bump` from Task 1.

- [ ] **Step 1: Add the CI guard step**

Insert into `ci.yml` `build-and-test` after `Install dependencies`:

```yaml
      - name: Check examples reference published versions
        run: pnpm examples:check
```

- [ ] **Step 2: Add the post-publish release step**

Insert into `release.yml` between the `Create Release Pull Request or Publish` step and the `Push release tags` step:

```yaml
      - name: Sync examples to released versions
        if: ${{ steps.changesets.outputs.published == 'true' }}
        run: |
          pnpm examples:bump
          git add examples
          git commit -m "chore(examples): sync to latest phoria packages"
```

- [ ] **Step 3: Verify the workflow YAML is well-formed**

Run: `pnpm biome check .github/workflows/ci.yml .github/workflows/release.yml`
Expected: no errors (biome parses the YAML). If biome reports the files as ignored/unsupported, fall back to `python3 -c "import yaml,sys; [yaml.safe_load(open(f)) for f in sys.argv[1:]]" .github/workflows/ci.yml .github/workflows/release.yml`.

---

## Task 5: Docs

**Files:**
- Modify: `AGENTS.md` (Examples section, lines 158-167)
- Modify: `examples/TODO.md` (deferred items section)

- [ ] **Step 1: Rewrite the AGENTS.md Examples section**

Replace the current Examples section with:

```markdown
## Examples

`examples/` contains standalone example apps (user-facing), distinct from the `e2e/` workspace integration tests. Examples are **deliberately outside the pnpm workspace**: the root `pnpm-workspace.yaml` globs do not match them, and each example ships its own `pnpm-workspace.yaml` + committed `pnpm-lock.yaml`.

- Committed examples reference the **published** phoria packages (registry ranges), so a fresh clone or giget fetch can `pnpm install && pnpm build` them standalone. CI enforces this via `pnpm examples:check` — it fails if an example commits a `link:`/`file:` phoria ref or a Phoria `ProjectReference`.
- Local development against in-repo packages: run `pnpm examples:link` to switch the example to `link:` refs + a `ProjectReference` (run `pnpm build` at the repo root first so linked `dist` exists), and `pnpm examples:sync` when done to restore the committed registry state. Never commit a linked example — `pnpm examples:check` rejects it.
- The release flow keeps examples current: after `changeset publish`, `pnpm examples:bump` rewrites each example's refs to the just-released versions and regenerates the lockfiles.
- pnpm uses the **nearest** `pnpm-workspace.yaml`, so running commands inside an example's directory shadows the repo-root workspace — no `--ignore-workspace` flag needed. pnpm 11 reads build-script settings from `pnpm-workspace.yaml` (the `pnpm` field in `package.json` is ignored), so each example carries its own `allowBuilds` (esbuild, Biome, etc.).
- `getting-started` keeps `package.json` and `vite.config.ts` in `WebApp/` (the Vite dev server only discovers config in the spawned working directory); `aspire start`/`aspire run` locate the example-root `aspire.config.json`, while `aspire stop` runs from the example root with `--all`.
```

- [ ] **Step 2: Update `examples/TODO.md`**

Replace the `## Deferred (e2e → standalone examples re-org)` section with:

```markdown
## Deferred

* [ ] Convert `e2e/framework-multiple`, `e2e/with-sidecar`, `e2e/with-workspace` into standalone examples, and decide how CI runs their e2e tests.
* [ ] Add examples to CI (install + build against published packages) — the `examples:check` guard exists; a full example smoke test is still open.
* [ ] Verify giget-fetch of a committed example end-to-end (standalone install + build).
```

- [ ] **Step 3: Verify no leftover references to the old workflow**

Run: `grep -rn "literal semver\|file://\." AGENTS.md docs/`
Expected: only hits for the historical rationale, if any; no leftover instructions to use `file:` in examples.

---

## Task 6: End-to-end verification round-trip

**Files:** none (verification only).

**Interfaces:**
- Consumes: all of the above.

- [ ] **Step 1: Root sanity**

Run: `pnpm build && pnpm lint && pnpm check`
Expected: green (builds phoria packages with `catalog:` deps; note the pre-existing Phoria.csproj concurrent-build race in the e2e apps is unrelated and may fail).

- [ ] **Step 2: Committed state passes the guard**

Run: `pnpm examples:check`
Expected: exit 0, `...WebApp: OK`.

- [ ] **Step 3: Link round-trip**

Run: `pnpm examples:link`
Expected: example `package.json` has `link:` refs, csproj has `ProjectReference`.

Run (in `examples/getting-started/WebApp`): `pnpm install && pnpm check && pnpm lint && pnpm build`
Expected: all green against the linked local packages (root `dist` present from Step 1).

- [ ] **Step 4: Sync restores byte-identical committed state**

Run: `pnpm examples:sync`
Expected: frozen install passes.

Run: `git status`
Expected: clean in `examples/getting-started/WebApp` (no diff vs HEAD).

Run: `pnpm examples:check`
Expected: exit 0.

- [ ] **Step 5: Release bump produces correct refs (simulated)**

Run: `pnpm examples:bump`
Expected: refs are `^0.4.2` / `^0.4.2` / `^0.2.1`, csproj has an unversioned `<PackageReference Include="Phoria" />`, and `Directory.Packages.props` has `<PackageVersion Include="Phoria" Version="0.4.2" />`; example frozen install passes.

Run: `pnpm examples:sync` (restores committed state), then `git status`
Expected: clean again.
