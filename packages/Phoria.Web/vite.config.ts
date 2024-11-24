import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import Inspect from "vite-plugin-inspect"
import { phoria } from "@meeg/phoria/vite"
import { phoriaReact } from "@meeg/phoria-react/vite"
import { phoriaSvelte } from "@meeg/phoria-svelte/vite"
import { phoriaVue } from "@meeg/phoria-vue/vite"

// TODO: See if there is a plugin like https://github.com/vitejs/vite-plugin-basic-ssl but for dotnet dev certs, or create one if not

// https://vite.dev/config/
export default defineConfig({
	publicDir: "public",
	plugins: [
		tsconfigPaths({
			root: "../" // The tsconfig is in the root of the project, not the "Vite root"
		}),
		phoria(),
		phoriaReact(),
		phoriaSvelte(),
		phoriaVue(),
		Inspect({
			build: true,
			outputDir: ".vite-inspect"
		})
	]
})
