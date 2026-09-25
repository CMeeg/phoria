import { defineConfig } from "vite"
import dts from "vite-plugin-dts"
import { externalizeDeps } from "vite-plugin-externalize-deps"

// https://vite.dev/config/
export default defineConfig({
	plugins: [externalizeDeps(), dts({ entryRoot: "src", exclude: ["tests/**/*"] })],
	resolve: {
		tsconfigPaths: true
	},
	build: {
		lib: {
			entry: {
				main: "src/main.ts"
			},
			name: "phoria-opentelemetry",
			// A single-entry lib defaults to `umd`; `cjs` is required by the package exports
			formats: ["es", "cjs"]
		}
	}
})
