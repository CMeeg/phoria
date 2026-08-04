import { defineConfig } from "vitest/config"

export default defineConfig({
	test: {
		environment: "node",
		include: ["tests/e2e/**/*.test.ts"],
		testTimeout: 30_000,
		hookTimeout: 60_000,
		env: {
			// The Aspire-hosted Web App redirects HTTP to its HTTPS endpoint using a dev certificate.
			NODE_TLS_REJECT_UNAUTHORIZED: "0"
		}
	}
})
