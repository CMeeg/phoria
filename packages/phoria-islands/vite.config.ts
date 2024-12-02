import { defineConfig } from "vite"
import dts from "vite-plugin-dts"
import { externalizeDeps } from "vite-plugin-externalize-deps"
import tsconfigPaths from "vite-tsconfig-paths"

// https://vite.dev/config/
export default defineConfig({
	plugins: [tsconfigPaths(), externalizeDeps(), dts()],
	build: {
		lib: {
			entry: {
				client: "src/client/main.ts",
				main: "src/main.ts",
				server: "src/server/main.ts",
				vite: "src/vite/plugin.ts"
			},
			name: "phoria"
		}
	}
})
