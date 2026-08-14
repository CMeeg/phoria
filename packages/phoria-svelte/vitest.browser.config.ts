import { svelte } from "@sveltejs/vite-plugin-svelte"
import { playwright } from "@vitest/browser-playwright"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [svelte()],
	resolve: {
		tsconfigPaths: true
	},
	optimizeDeps: {
		include: ["svelte"]
	},
	test: {
		include: ["src/**/*.browser.test.ts"],
		browser: {
			enabled: true,
			headless: true,
			provider: playwright(),
			instances: [{ browser: "chromium" }]
		}
	}
})
