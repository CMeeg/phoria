import { readFile, mkdir } from "node:fs/promises"
import { join, resolve, isAbsolute, dirname } from "node:path"
import util from "node:util"
import child_process from "node:child_process"

const exec = util.promisify(child_process.exec)

async function getPackageJson(path) {
	try {
		const pkgContent = await readFile(path, { encoding: "utf8" })

		return JSON.parse(pkgContent)
	} catch (error) {
		throw new Error(`Failed to parse package.json: ${path}`, { cause: error })
	}
}

async function run(command) {
	const { stdout, stderr } = await exec(command)

	if (stdout) {
		console.log(stdout)
	}

	if (stderr) {
		throw new Error(stderr)
	}
}

async function getNugetPackageSources(path) {
	// Read the nuget.config file

	const nugetConfigContent = await readFile(path, "utf-8")

	// Extract the <packageSources> block

	const packageSourcesMatch = nugetConfigContent.match(/<packageSources>([\s\S]*?)<\/packageSources>/)

	if (!packageSourcesMatch) {
		throw new Error(`No packageSources block found in nuget.config: ${path}`)
	}

	const packageSourcesContent = packageSourcesMatch[1]

	// Extract package sources

	const packageSources = new Map()

	const matches = packageSourcesContent.matchAll(/<add\s+key="([^"]+)"\s+value="([^"]+)"\s*\/>/g)

	for (const match of matches) {
		const key = match[1]
		const value = match[2]

		packageSources.set(key, value)
	}

	return packageSources
}

async function getNugetPackageSource(path, name) {
	const packageSources = await getNugetPackageSources(path)

	const packageSource = packageSources.get(name)

	if (!packageSource) {
		throw new Error(`Package source not found: ${name}`)
	}

	if (packageSource.startsWith("http")) {
		return packageSource
	}

	// If this is a local package source, ensure the directory exists

	const localSourcePath = isAbsolute(packageSource) ? packageSource : join(dirname(path), packageSource)

	await mkdir(localSourcePath, { recursive: true })

	return localSourcePath
}

// Get required args

const nameArgIndex = process.argv.indexOf("--name")
const packageSourceName = nameArgIndex > -1 ? process.argv[nameArgIndex + 1] : null

if (!packageSourceName) {
	throw new Error("--name argument is required")
}

const apiKey = process.env.NUGET_API_KEY ?? "no_api_key"

// Get the version from the package.json file

const cwd = process.cwd()
const pkg = await getPackageJson(join(cwd, "package.json"))
const { version } = pkg

// Pack the project

const distPath = join(cwd, "dist")

await run(`dotnet pack "${cwd}" --include-symbols --nologo -p:Version=${version} --output "${distPath}"`)

// Push the package to the NuGet feed

const packageSource = await getNugetPackageSource(resolve("../../nuget.config"), packageSourceName)

await run(`dotnet nuget push \"${join(distPath, "*.nupkg")}\" --source \"${packageSource}\" --api-key ${apiKey} --skip-duplicate`)
