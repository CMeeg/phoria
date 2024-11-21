import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import dts from "vite-plugin-dts"
import { externalizeDeps } from "vite-plugin-externalize-deps"

// https://vite.dev/config/
export default defineConfig({
	plugins: [tsconfigPaths(), externalizeDeps(), dts()],
	build: {
		lib: {
			entry: {
				main: "src/main.ts",
				client: "src/client/main.ts",
				server: "src/server/main.ts",
				vite: "src/vite/plugin.ts"
			},
			name: "phoria-vue"
		}
	}
})
