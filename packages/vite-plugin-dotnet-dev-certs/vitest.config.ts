import { defineConfig } from "vitest/config"

export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	test: {
		environment: "node",
		include: ["src/**/*.test.ts"],
		coverage: {
			provider: "v8",
			all: true,
			include: ["src/**/*.{ts,tsx}"],
			exclude: ["**/*.test.ts", "**/*.test.tsx", "**/*.browser.test.ts", "**/*.browser.test.tsx"],
			reporter: ["text", "json"]
		}
	}
})
