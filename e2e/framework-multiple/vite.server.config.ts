import { join } from "node:path"
import { parsePhoriaAppSettings } from "@phoria/phoria/server"
import { type UserConfig, defineConfig } from "vite"

export default defineConfig(async () => {
	const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? "development"
	const appsettings = await parsePhoriaAppSettings({
		environment: dotnetEnv,
		cwd: join(process.cwd(), "WebApp")
	})

	// https://vite.dev/config/
	return {
		root: appsettings.root,
		base: appsettings.base,
		build: {
			ssr: true,
			target: "es2022",
			copyPublicDir: false,
			emptyOutDir: true,
			outDir: `${appsettings.build.outDir}/server`,
			rollupOptions: {
				input: `${appsettings.root}/src/server.ts`
			}
		},
		environments: {
			ssr: {
				resolve: {
					// It should only be required to add the `@phoria/phoria*` packages in this workspace - when the packages are published they should be external by default
					external: ["@phoria/phoria"]
				}
			}
		}
	} satisfies UserConfig
})
