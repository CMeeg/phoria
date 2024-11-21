import { defineConfig, type UserConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import { svelte } from "@sveltejs/vite-plugin-svelte"
import react from "@vitejs/plugin-react"
import vue from "@vitejs/plugin-vue"
import Inspect from "vite-plugin-inspect"
import { parsePhoriaAppSettings } from "@meeg/phoria/server"
import { phoriaReact } from "@meeg/phoria-react/vite"
import { phoriaSvelte } from "@meeg/phoria-svelte/vite"
import { phoriaVue } from "@meeg/phoria-vue/vite"

// TODO: See if there is a plugin like https://github.com/vitejs/vite-plugin-basic-ssl but for dotnet dev certs, or create one if not

export default defineConfig(async () => {
	const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "development"
	const appsettings = await parsePhoriaAppSettings(process.cwd(), dotnetEnv)

	// https://vite.dev/config/
	return {
		root: appsettings.Root,
		base: appsettings.Base,
		publicDir: "public",
		plugins: [
			tsconfigPaths({
				root: "../" // The tsconfig is in the root of the project, not the "Vite root"
			}),
			react(),
			svelte(),
			vue(),
			phoriaReact(),
			phoriaSvelte(),
			phoriaVue(),
			Inspect({
				build: true,
				outputDir: ".vite-inspect"
			})
		],
		build: {
			rollupOptions: {
				input: appsettings.Entry
			}
		},
		ssr: {
			// It should only be required to add the `@meeg/phoria*` packages in this workspace - when the packages are published they should be external by default
			external: ["@meeg/phoria-react/server", "@meeg/phoria-svelte/server", "@meeg/phoria-vue/server", "@meeg/phoria"]
		}
	} satisfies UserConfig
})
