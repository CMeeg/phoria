import { svelte } from "@sveltejs/vite-plugin-svelte"
import react from "@vitejs/plugin-react"
import vue from "@vitejs/plugin-vue"
import { defineConfig } from "vite"
import tsconfigPaths from "vite-tsconfig-paths"
import appsettingsDev from "./appsettings.Development.json" with { type: "json" }
import appsettings from "./appsettings.json" with { type: "json" }

// https://vite.dev/config/
export default defineConfig({
	appType: "custom",
	root: "ui",
	base: `/${appsettings.Vite.Base}`,
	publicDir: "public",
	plugins: [
		tsconfigPaths({
			root: "../"
		}),
		react(),
		svelte(),
		vue()
	],
	resolve: {
		dedupe: ["react", "react-dom", "vue", "svelte"]
	},
	build: {
		manifest: appsettings.Vite.Manifest,
		rollupOptions: {
			input: "ui/src/entry-client.tsx"
		}
	},
	server: {
		port: appsettingsDev.Vite.Server.Port,
		strictPort: true,
		hmr: {
			host: appsettingsDev.Vite.Server.Host,
			clientPort: appsettingsDev.Vite.Server.Port
		}
	}
})
