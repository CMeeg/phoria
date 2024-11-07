import { svelte } from "@sveltejs/vite-plugin-svelte"
import react from "@vitejs/plugin-react"
import vue from "@vitejs/plugin-vue"
import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import appsettings from "./appsettings.json" with { type: "json" }

// TODO: See if there is a plugin like https://github.com/vitejs/vite-plugin-basic-ssl but for dotnet dev certs, or create one if not

// https://vite.dev/config/
export default defineConfig({
	appType: "custom",
	root: "ui",
	base: appsettings.Phoria.Base,
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
		host: appsettings.Phoria.Server.Host,
		port: appsettings.Phoria.Server.Port
	},
	build: {
		manifest: appsettings.Phoria.Manifest,
		rollupOptions: {
			input: "ui/src/entry-client.tsx"
		}
	}
})
