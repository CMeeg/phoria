import react from "@vitejs/plugin-react"
import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths(), react()],
	optimizeDeps: {
		include: ["react", "react-dom/client"]
	},
	test: {
		include: ["src/**/*.browser.test.tsx"],
		browser: {
			enabled: true,
			provider: "playwright",
			headless: true,
			instances: [{ browser: "chromium" }]
		}
	}
})
