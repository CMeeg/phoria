import { defineConfig } from "vitest/config"

export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	test: {
		environment: "node",
		include: ["src/**/*.test.ts"],
		exclude: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"]
	}
})
