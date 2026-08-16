import { playwright } from "@vitest/browser-playwright"
import { defineConfig } from "vitest/config"

export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	test: {
		include: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"],
		browser: {
			enabled: true,
			headless: true,
			provider: playwright(),
			instances: [{ browser: "chromium" }]
		}
	}
})
