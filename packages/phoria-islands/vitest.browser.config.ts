import { playwright } from "@vitest/browser-playwright"
import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths()],
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
