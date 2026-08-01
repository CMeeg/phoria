import { defineConfig } from "vite"
import dts from "vite-plugin-dts"
import { externalizeDeps } from "vite-plugin-externalize-deps"

// https://vite.dev/config/
export default defineConfig({
	plugins: [externalizeDeps(), dts()],
	resolve: {
		tsconfigPaths: true
	},
	build: {
		lib: {
			entry: {
				plugin: "src/plugin.ts"
			},
			formats: ["es", "cjs"],
			name: "dotnet-dev-certs"
		}
	}
})
