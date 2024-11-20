import { defineConfig, type UserConfig } from "vite"
import { parsePhoriaAppSettings } from "@meeg/phoria/server"

export default defineConfig(async () => {
	const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "development"
	const appsettings = await parsePhoriaAppSettings(process.cwd(), dotnetEnv)

	// https://vite.dev/config/
	return {
		root: appsettings.Root,
		base: appsettings.Base,
		build: {
			target: "es2022",
			copyPublicDir: false
		},
		ssr: {
			// It should only be required to add the `@meeg/phoria*` packages in this workspace - when the packages are published they should be external by default
			external: ["@meeg/phoria-react/server", "@meeg/phoria-svelte/server", "@meeg/phoria-vue/server", "@meeg/phoria"]
		}
	} satisfies UserConfig
})
