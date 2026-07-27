import type { EnvironmentOptions } from "vite"
import { describe, expect, it } from "vitest"
import { phoria } from "./plugin"

describe("phoria plugin", () => {
	it("preserves user-supplied rolldownOptions when setting the entry input", async () => {
		const plugin = phoria({ appsettings: { entry: "entry.ts", ssrEntry: "ssr.ts" } }) as {
			config: (config: object, env: object) => Promise<void> | void
			configEnvironment: (name: string, options: EnvironmentOptions, env: object) => void
		}
		const options: EnvironmentOptions = {
			build: {
				rolldownOptions: {
					output: { entryFileNames: "custom-[name].js" }
				}
			}
		}

		await plugin.config({}, { command: "build", mode: "production" })
		plugin.configEnvironment("client", options, { command: "build", mode: "production" })

		expect(options.build?.rolldownOptions?.output).toEqual({ entryFileNames: "custom-[name].js" })
		expect(options.build?.rolldownOptions?.input).toBeDefined()
	})
})
