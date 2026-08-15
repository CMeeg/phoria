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
				main: "src/main.ts",
				client: "src/client/main.ts",
				server: "src/server/main.ts",
				vite: "src/vite/plugin.ts"
			},
			name: "phoria-svelte"
		}
	}
})
