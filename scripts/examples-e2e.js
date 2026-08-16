import { spawn } from "node:child_process"
import { existsSync, readdirSync } from "node:fs"
import { join, resolve } from "node:path"
import { fileURLToPath } from "node:url"

const root = resolve(fileURLToPath(new URL("..", import.meta.url)))

const exampleConfigs = {
	"getting-started": { webAppPort: 5373 },
	"framework-multiple": { webAppPort: 5573 }
}

function findExamples() {
	return readdirSync(join(root, "examples")).filter((name) =>
		existsSync(join(root, "examples", name, "WebApp", "package.json"))
	)
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

async function testExample(name) {
	const exampleDir = join(root, "examples", name, "WebApp")
	const { webAppPort } = exampleConfigs[name]
	const environment = {
		ASPNETCORE_ENVIRONMENT: "Preview",
		DOTNET_ENVIRONMENT: "Preview"
	}

	console.log(`\n=== ${name} ===`)
	await run("pnpm", ["install", "--frozen-lockfile"], exampleDir)
	await run("pnpm", ["build"], exampleDir)

	const phoriaServer = start("node", ["ui/dist/server/server.js"], exampleDir, {
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
		exampleDir,
		environment
	)

	try {
		await waitForHealthy(
			`http://localhost:${webAppPort}/health`,
			Promise.race([phoriaServer.error, phoriaServer.exit, webApp.error, webApp.exit])
		)
		await run("pnpm", ["test:e2e"], exampleDir, { PHORIA_WEBAPP_URL: `http://localhost:${webAppPort}` })
	} finally {
		webApp.child.kill("SIGTERM")
		phoriaServer.child.kill("SIGTERM")
	}
}

async function main() {
	for (const name of findExamples()) {
		await testExample(name)
	}
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
	await main()
}
