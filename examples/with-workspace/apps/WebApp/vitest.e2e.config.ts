import { defineConfig } from "vitest/config"

export default defineConfig({
	test: {
		environment: "node",
		include: ["ui/tests/e2e/**/*.test.ts"],
		testTimeout: 30_000,
		hookTimeout: 60_000,
		env: { NODE_TLS_REJECT_UNAUTHORIZED: "0" }
	}
})
