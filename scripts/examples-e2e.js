import { spawn } from "node:child_process"
import { existsSync, readdirSync } from "node:fs"
import { join, resolve } from "node:path"
import { fileURLToPath } from "node:url"

const root = resolve(fileURLToPath(new URL("..", import.meta.url)))

const exampleConfigs = {
	"framework-react": { webAppPort: 5173 },
	"framework-vue": { webAppPort: 5273 },
	"getting-started": { webAppPort: 5373 },
	"framework-svelte": { webAppPort: 5473 },
	"framework-multiple": { webAppPort: 5573 },
	"with-workspace": { webAppPort: 5673 },
	"with-tailwind": { webAppPort: 5773 },
	"with-styled-components": { webAppPort: 5873 },
	"with-storybook": { webAppPort: 5973 }
}

function findExamples() {
	return readdirSync(join(root, "examples")).flatMap((name) => {
		const exampleDir = join(root, "examples", name)
		const webAppDir = existsSync(join(exampleDir, "WebApp", "package.json"))
			? join(exampleDir, "WebApp")
			: existsSync(join(exampleDir, "apps", "WebApp", "package.json"))
				? join(exampleDir, "apps", "WebApp")
				: null

		return webAppDir ? [{ name, exampleDir, webAppDir }] : []
	})
}

export function parseExamplesE2E(value, discoveredNames) {
	if (!value?.trim()) {
		return discoveredNames
	}

	const selectedNames = [
		...new Set(
			value
				.split(",")
				.map((name) => name.trim())
				.filter(Boolean)
		)
	]
	const unknownNames = selectedNames.filter((name) => !discoveredNames.includes(name))

	if (unknownNames.length) {
		throw new Error(`Unknown example name(s): ${unknownNames.join(", ")}. Valid names: ${discoveredNames.join(", ")}`)
	}

	return selectedNames
}

function run(command, args, cwd, env = {}) {
	return new Promise((resolvePromise, reject) => {
		const child = spawn(command, args, {
			cwd,
			env: { ...process.env, ...env },
			stdio: "inherit"
		})

		child.once("error", reject)
		child.once("exit", (code, signal) => {
			if (code === 0) {
				resolvePromise()
				return
			}

			reject(new Error(`${command} exited with ${signal ?? `code ${code}`}`))
		})
	})
}

function start(command, args, cwd, env = {}) {
	const child = spawn(command, args, {
		cwd,
		env: { ...process.env, ...env },
		stdio: "inherit"
	})

	const error = new Promise((_, reject) => child.once("error", reject))
	const exit = new Promise((_, reject) =>
		child.once("exit", (code, signal) => reject(new Error(`${command} exited with ${signal ?? `code ${code}`}`)))
	)

	return { child, error, exit }
}

async function waitForHealthy(url, processFailure, timeoutMs = 90_000) {
	const deadline = Date.now() + timeoutMs

	while (Date.now() < deadline) {
		try {
			const response = await fetch(url)

			if (response.ok) {
				return
			}
		} catch {
			// The process is still starting.
		}

		await Promise.race([new Promise((resolvePromise) => setTimeout(resolvePromise, 500)), processFailure])
	}

	throw new Error(`Timed out waiting for ${url}`)
}

async function testExample({ name, exampleDir, webAppDir }) {
	const { webAppPort } = exampleConfigs[name]
	const installDir = existsSync(join(exampleDir, "pnpm-workspace.yaml")) ? exampleDir : webAppDir
	const environment = {
		ASPNETCORE_ENVIRONMENT: "Preview",
		DOTNET_ENVIRONMENT: "Preview"
	}

	console.log(`\n=== ${name} ===`)
	await run("pnpm", ["install", "--frozen-lockfile"], installDir)
	await run("pnpm", ["build"], webAppDir)

	const phoriaServer = start("node", ["ui/dist/server/server.js"], webAppDir, {
		...environment,
		NODE_ENV: "production"
	})
	const webApp = start(
		"dotnet",
		[
			"run",
			"--no-build",
			"--configuration",
			"Release",
			"--no-launch-profile",
			"--urls",
			`http://localhost:${webAppPort}`
		],
		webAppDir,
		environment
	)

	try {
		await waitForHealthy(
			`http://localhost:${webAppPort}/health`,
			Promise.race([phoriaServer.error, phoriaServer.exit, webApp.error, webApp.exit])
		)
		await run("pnpm", ["test:e2e"], webAppDir, { PHORIA_WEBAPP_URL: `http://localhost:${webAppPort}` })
	} finally {
		webApp.child.kill("SIGTERM")
		phoriaServer.child.kill("SIGTERM")
	}
}

async function main() {
	const examples = findExamples()
	const selectedNames = parseExamplesE2E(
		// biome-ignore lint/suspicious/noUndeclaredEnvVars: This variable controls explicit local and CI e2e selection.
		process.env.EXAMPLES_E2E,
		examples.map(({ name }) => name)
	)

	for (const name of selectedNames) {
		await testExample(examples.find((example) => example.name === name))
	}
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
	await main()
}
