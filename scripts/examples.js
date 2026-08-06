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
	"@phoria/vite-plugin-dotnet-dev-certs": "packages/vite-plugin-dotnet-dev-certs"
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

	console.log(
		`Linked ${exampleDir} to local packages. Run \`pnpm build\` at the repo root first, and \`pnpm examples:sync\` before committing.`
	)
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
