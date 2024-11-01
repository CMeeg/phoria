import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import dts from "vite-plugin-dts"
import pkg from "./package.json" with { type: "json" }

// https://vite.dev/config/
export default defineConfig({
	plugins: [tsconfigPaths(), dts()],
	build: {
		lib: {
			// Could also be a dictionary or array of multiple entry points
			entry: {
				main: "src/main.ts",
				react: "src/react/register.tsx",
				vue: "src/vue/register.ts",
				svelte: "src/svelte/register.ts"
			},
			name: "phoria-islands"
		},
		rollupOptions: {
			external: [
				...Object.keys(pkg.dependencies), // don't bundle dependencies
				/^node:.*/ // don't bundle built-in Node.js modules (use protocol imports!)
			],
			output: {
				// Provide global variables to use in the UMD build
				// for externalized deps
				globals: {
					react: "React",
					"react-dom": "ReactDOM",
					vue: "Vue",
					svelte: "Svelte"
				}
			}
		}
	}
})
