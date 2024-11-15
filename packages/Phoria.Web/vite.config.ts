import { defineConfig, type UserConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import { svelte } from "@sveltejs/vite-plugin-svelte"
import react from "@vitejs/plugin-react"
import vue from "@vitejs/plugin-vue"
import { getPhoriaAppSettings } from "@meeg/phoria/server"

// TODO: See if there is a plugin like https://github.com/vitejs/vite-plugin-basic-ssl but for dotnet dev certs, or create one if not

export default defineConfig(async () => {
	const nodeEnv = process.env.NODE_ENV || "development"
	const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? nodeEnv
	const appsettings = await getPhoriaAppSettings(process.cwd(), dotnetEnv)

	// https://vite.dev/config/
	return {
		appType: "custom",
		root: "ui",
		base: appsettings.Base,
		publicDir: "public",
		plugins: [
			tsconfigPaths({
				root: "../" // The tsconfig is in the root of the project, not the "Vite root"
			}),
			react(),
			svelte(),
			vue()
		],
		server: {
			host: appsettings.Server?.Host,
			port: appsettings.Server?.Port
		},
		build: {
			manifest: appsettings.Manifest,
			rollupOptions: {
				input: "ui/src/entry-client.tsx"
			}
		}
	} satisfies UserConfig
})
