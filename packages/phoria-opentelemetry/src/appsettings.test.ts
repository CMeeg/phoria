import { mkdtemp, rm, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { afterEach, beforeEach, describe, expect, it } from "vitest"
import { type PhoriaObservabilityAppSettings, parsePhoriaObservabilityAppSettings } from "./appsettings"

const defaultSettings: PhoriaObservabilityAppSettings = {
	logging: false,
	tracing: {
		enabled: false,
		samplingRatio: 0.1
	},
	metrics: false
}

const environmentVariables = ["DOTNET_ENVIRONMENT", "ASPNETCORE_ENVIRONMENT", "NODE_ENV"] as const
const originalEnvironment = new Map<string, string | undefined>()

let cwd: string

beforeEach(async () => {
	for (const name of environmentVariables) {
		originalEnvironment.set(name, process.env[name])
		delete process.env[name]
	}

	cwd = await mkdtemp(join(tmpdir(), "phoria-otel-"))
})

afterEach(async () => {
	for (const name of environmentVariables) {
		const original = originalEnvironment.get(name)

		if (original === undefined) {
			delete process.env[name]
		} else {
			process.env[name] = original
		}
	}

	await rm(cwd, { recursive: true, force: true })
})

describe("parsePhoriaObservabilityAppSettings", () => {
	it("parses the base appsettings file and applies defaults for missing keys", async () => {
		await writeFile(
			join(cwd, "appsettings.json"),
			JSON.stringify({
				phoria: {
					observability: {
						logging: true,
						logHealthChecks: true
					}
				}
			})
		)

		const settings = await parsePhoriaObservabilityAppSettings({ cwd })

		expect(settings).toEqual({
			logging: true,
			logHealthChecks: true,
			tracing: {
				enabled: false,
				samplingRatio: 0.1
			},
			metrics: false
		})
	})

	it("deep-merges the environment file over the base file", async () => {
		await writeFile(
			join(cwd, "appsettings.json"),
			JSON.stringify({
				phoria: {
					observability: {
						logging: false,
						tracing: {
							enabled: false,
							samplingRatio: 0.5
						}
					}
				}
			})
		)
		await writeFile(
			join(cwd, "appsettings.Development.json"),
			JSON.stringify({
				phoria: {
					observability: {
						logging: true,
						tracing: {
							enabled: true
						}
					}
				}
			})
		)

		const settings = await parsePhoriaObservabilityAppSettings({ cwd, environment: "Development" })

		expect(settings.logging).toBe(true)
		expect(settings.tracing.enabled).toBe(true)
		expect(settings.tracing.samplingRatio).toBe(0.5)
		expect(settings.metrics).toBe(false)
	})

	it("returns defaults when no appsettings file is found", async () => {
		const settings = await parsePhoriaObservabilityAppSettings({ cwd })

		expect(settings).toEqual(defaultSettings)
	})

	it("resolves the environment from the environment variable chain when omitted", async () => {
		process.env.DOTNET_ENVIRONMENT = "Staging"

		await writeFile(
			join(cwd, "appsettings.Staging.json"),
			JSON.stringify({
				phoria: {
					observability: {
						logging: true,
						metrics: true
					}
				}
			})
		)

		const settings = await parsePhoriaObservabilityAppSettings({ cwd })

		expect(settings.logging).toBe(true)
		expect(settings.metrics).toBe(true)
	})
})
