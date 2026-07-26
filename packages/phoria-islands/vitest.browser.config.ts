import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths()],
	test: {
		include: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"],
		browser: {
			enabled: true,
			provider: "playwright",
			headless: true,
			instances: [{ browser: "chromium" }]
		}
	}
})
