import { parsePhoriaAppSettings } from "@phoria/phoria/server"
import { type UserConfig, defineConfig } from "vite"

export default defineConfig(async () => {
	const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "development"
	const appsettings = await parsePhoriaAppSettings({ environment: dotnetEnv })

	// https://vite.dev/config/
	return {
		root: appsettings.Root,
		base: appsettings.Base,
		build: {
			target: "es2022",
			copyPublicDir: false
		},
		ssr: {
			// It should only be required to add the `@phoria/phoria*` packages in this workspace - when the packages are published they should be external by default
			external: [
				"@phoria/phoria-react/server",
				"@phoria/phoria-svelte/server",
				"@phoria/phoria-vue/server",
				"@phoria/phoria"
			]
		}
	} satisfies UserConfig
})
