import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import Inspect from "vite-plugin-inspect"
import { inspectConfig } from "@meeg/vite-plugin-inspect-config"
import { dotnetDevCerts } from "@meeg/vite-plugin-dotnet-dev-certs"
import { phoria } from "@meeg/phoria/vite"
import { phoriaReact } from "@meeg/phoria-react/vite"
import { phoriaSvelte } from "@meeg/phoria-svelte/vite"
import { phoriaVue } from "@meeg/phoria-vue/vite"

// https://vite.dev/config/
export default defineConfig({
	publicDir: "public",
	plugins: [
		tsconfigPaths({
			root: "../" // The tsconfig is in the root of the project, not the "Vite root"
		}),
		dotnetDevCerts(),
		phoria(),
		phoriaReact(),
		phoriaSvelte(),
		phoriaVue(),
		Inspect({
			build: true,
			outputDir: ".vite-inspect"
		}),
		inspectConfig()
	]
})
