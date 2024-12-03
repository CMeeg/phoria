import { inspectConfig } from "@meeg/vite-plugin-inspect-config"
import { phoriaReact } from "@phoria/phoria-react/vite"
import { phoriaSvelte } from "@phoria/phoria-svelte/vite"
import { phoriaVue } from "@phoria/phoria-vue/vite"
import { phoria } from "@phoria/phoria/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"
import Inspect from "vite-plugin-inspect"
import tsconfigPaths from "vite-tsconfig-paths"

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
