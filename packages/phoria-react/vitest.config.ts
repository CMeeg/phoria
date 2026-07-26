import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths()],
	test: {
		environment: "node",
		include: ["src/**/*.test.ts"],
		exclude: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"],
		passWithNoTests: true
	}
})
