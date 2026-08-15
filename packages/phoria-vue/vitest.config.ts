import { defineConfig } from "vitest/config"

export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	test: {
		environment: "node",
		include: ["src/**/*.test.ts"],
		exclude: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"],
		passWithNoTests: true,
		coverage: {
			provider: "v8",
			all: true,
			include: ["src/**/*.{ts,tsx}"],
			exclude: [
				"**/*.test.ts",
				"**/*.test.tsx",
				"**/*.browser.test.ts",
				"**/*.browser.test.tsx",
				"src/main.ts",
				"src/client/main.ts",
				"src/server/main.ts"
			],
			reporter: ["text", "json", "lcov"]
		}
	}
})
