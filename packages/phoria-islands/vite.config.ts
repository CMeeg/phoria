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
				react: "src/react/register.tsx",
				server: "src/server/main.ts",
				svelte: "src/svelte/register.ts",
				vue: "src/vue/register.ts",
				web: "src/web.ts"
			},
			name: "phoria-islands"
		}
	}
})
