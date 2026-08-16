import { exec } from "node:child_process"
import { existsSync, readdirSync } from "node:fs"
import { readFile, writeFile } from "node:fs/promises"
import { dirname, join, relative, resolve } from "node:path"
import { fileURLToPath } from "node:url"
import { promisify } from "node:util"
import { command, error, info, step, success } from "./log.js"

const execAsync = promisify(exec)
const scriptDir = dirname(fileURLToPath(import.meta.url))
const root = resolve(scriptDir, "..")

const jsPackages = {
	"@phoria/phoria": "packages/phoria-islands",
	"@phoria/phoria-react": "packages/phoria-react",
	"@phoria/phoria-svelte": "packages/phoria-svelte",
	"@phoria/phoria-vue": "packages/phoria-vue",
	"@phoria/opentelemetry": "packages/phoria-opentelemetry",
	"@phoria/vite-plugin-dotnet-dev-certs": "packages/vite-plugin-dotnet-dev-certs"
}

const dotnetPackage = { name: "Phoria", dir: "packages/Phoria", csproj: "Phoria.csproj" }

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

async function readCatalog() {
	const yaml = await readFile(join(root, "pnpm-workspace.yaml"), "utf8")
	const lines = yaml.split("\n")
	const catalog = {}

	for (let i = 0; i < lines.length; i++) {
		if (!/^catalog:\s*$/.test(lines[i])) {
			continue
		}

		for (let j = i + 1; j < lines.length; j++) {
			const line = lines[j]

			if (line.trim() === "") {
				continue
			}

			if (!/^\s/.test(line)) {
				break
			}

			const match = line.match(/^\s{2}(?:"([^"]+)"|([^:]+)):\s*(?:"([^"]*)"|(\S+))\s*$/)

			if (!match) {
				throw new Error(`Unsupported catalog entry in pnpm-workspace.yaml: "${line}"`)
			}

			catalog[match[1] ?? match[2]] = match[3] ?? match[4]
		}

		break
	}

	return catalog
}

async function literalizeCatalogDeps(pkgPath, catalog) {
	const text = await readFile(pkgPath, "utf8")
	let literalized = text

	for (const section of ["dependencies", "devDependencies"]) {
		const json = JSON.parse(text)[section] ?? {}

		for (const name of Object.keys(json)) {
			if (json[name] !== "catalog:") {
				continue
			}

			const spec = catalog[name]

			if (spec === undefined) {
				throw new Error(`No catalog entry for "${name}" (${pkgPath}, ${section})`)
			}

			const needle = `"${name}": "catalog:"`

			if (!literalized.includes(needle)) {
				throw new Error(`Expected "${needle}" in ${pkgPath}`)
			}

			literalized = literalized.split(needle).join(`"${name}": "${spec}"`)
		}
	}

	if (literalized !== text) {
		await writeFile(pkgPath, literalized)
	}
}

async function restorePackagesFromHead() {
	for (const dir of Object.values(jsPackages)) {
		const pkgPath = join(root, dir, "package.json")
		const rel = relative(root, pkgPath)

		if ((await readFile(pkgPath, "utf8")) !== (await getHead(pkgPath))) {
			await run(`git checkout -- ${rel}`)
		}
	}
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
	step(`🔗 Linking ${exampleDir}…`)

	const pkgPath = join(exampleDir, "package.json")
	const pkg = await readJson(pkgPath)

	for (const [name, dir] of Object.entries(jsPackages)) {
		const section = pkg.dependencies?.[name] ? "dependencies" : pkg.devDependencies?.[name] ? "devDependencies" : null

		if (section) {
			pkg[section][name] = `file:../../../${dir}`
		}
	}

	await writeJson(pkgPath, pkg)

	const catalog = await readCatalog()

	for (const dir of Object.values(jsPackages)) {
		await literalizeCatalogDeps(join(root, dir, "package.json"), catalog)
	}

	const csproj = await readFile(csprojPath(exampleDir), "utf8")

	if (csproj.includes(projectReference())) {
		info(`🔗 ${exampleDir}: already linked`)
	} else {
		const match = csproj.match(/<PackageReference Include="Phoria" \/>/)

		if (!match) {
			throw new Error(`No Phoria PackageReference found in ${csprojPath(exampleDir)}`)
		}

		await writeFile(csprojPath(exampleDir), csproj.replace(match[0], projectReference()))
	}

	await run("pnpm install", exampleDir)
	await run("dotnet restore WebApp.csproj", exampleDir)

	success(
		`Linked ${exampleDir} to local packages (file: refs, catalog literalized).\n` +
			`Run \`pnpm build\` at the repo root first. After every rebuild, run \`pnpm examples:refresh\` to refresh the hard links (a plain \`pnpm install\` does not).\n` +
			`Run \`pnpm examples:sync\` before committing.`
	)
}

let workspaceBuilt = false

async function refresh(exampleDir) {
	if (!workspaceBuilt) {
		step("🔨 Building workspace packages…")
		await run("pnpm build")
		workspaceBuilt = true
	}

	step(`🔄 Refreshing hard links for ${exampleDir}…`)

	await run("pnpm install --force", exampleDir)
	success(`Refreshed hard links for ${exampleDir}.`)
}

async function sync(exampleDir) {
	step(`↩️ Restoring ${exampleDir} to its committed state…`)

	await restorePackagesFromHead()

	const pkgPath = join(exampleDir, "package.json")
	const headPkg = JSON.parse(await getHead(pkgPath))
	const pkg = await readJson(pkgPath)
	let usedRegistryFallback = false

	for (const [name, dir] of Object.entries(jsPackages)) {
		for (const section of ["dependencies", "devDependencies"]) {
			if (pkg[section]?.[name]) {
				const headSpec = headPkg[section]?.[name]
				const { version } = await readJson(join(root, dir, "package.json"))

				pkg[section][name] = headSpec ?? `^${version}`
				usedRegistryFallback ||= !headSpec
			}
		}
	}

	await writeJson(pkgPath, pkg)

	const csproj = await readFile(csprojPath(exampleDir), "utf8")
	const headCsproj = await getHead(csprojPath(exampleDir))
	const headRef =
		headCsproj.match(/<ProjectReference Include="[^"]+" \/>/)?.[0] ??
		headCsproj.match(/<PackageReference Include="Phoria"(?: Version="[^"]+")? \/>/)?.[0]
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

	const lockPath = join(exampleDir, "pnpm-lock.yaml")
	await run(`git checkout -- ${relative(root, lockPath)}`)
	const lockfile = await readFile(lockPath, "utf8")
	const lockNeedsUpdate = Object.entries(jsPackages).some(([name]) =>
		["dependencies", "devDependencies"].some((section) => {
			const spec = pkg[section]?.[name]
			return spec && (!lockfile.includes(name) || !lockfile.includes(`specifier: ${spec}`))
		})
	)

	if (usedRegistryFallback || lockNeedsUpdate) {
		info("Skipped frozen install because the restored package dependency is newer than the committed example lockfile.")
	} else {
		await run("pnpm install --frozen-lockfile", exampleDir)
	}

	// Running root-level pnpm commands (e.g. `pnpm build`) while the packages'
	// `catalog:` specifiers are literalized rewrites the root lockfile. Restore it
	// since it can only be dirty from link-induced churn at this point.
	const { stdout: rootLockStatus } = await execAsync("git status --porcelain pnpm-lock.yaml", { cwd: root })

	if (rootLockStatus.trim()) {
		await run("git checkout -- pnpm-lock.yaml")
		info("Restored the root pnpm-lock.yaml (it was modified by link-induced catalog: churn).")
	}

	success(`Restored ${exampleDir} to its committed state.`)
}

async function check(exampleDir) {
	step(`🔎 Checking ${exampleDir}…`)

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
		error(`${exampleDir}: found ${problems.length} problem(s):\n${problems.join("\n")}`)
		process.exitCode = 1
	} else {
		success(`${exampleDir}: OK`)
	}
}

async function bump(exampleDir) {
	step(`⬆️ Bumping ${exampleDir}…`)

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

	await run("pnpm install --no-frozen-lockfile", exampleDir)

	success(`Bumped ${exampleDir} to ${versions.join(", ")} (registry refs).`)
}

const modes = { link, sync, check, bump, refresh }
const mode = process.argv[2]

if (!modes[mode]) {
	error("Usage: node scripts/examples.js <link|sync|check|bump|refresh>")
	process.exit(1)
}

for (const exampleDir of findExamples()) {
	await modes[mode](exampleDir)
}
