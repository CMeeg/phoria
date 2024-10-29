import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"
import appsettings from "./appsettings.json" with { type: "json" }
import appsettingsDev from "./appsettings.Development.json" with { type: "json" }

// https://vite.dev/config/
export default defineConfig({
	appType: "custom",
	root: "ui",
	publicDir: "public",
	plugins: [react()],
	optimizeDeps: {
		include: []
	},
	build: {
		manifest: appsettings.Vite.Manifest,
		emptyOutDir: true,
		outDir: "../wwwroot/ui",
		assetsDir: appsettings.Vite.Base,
		rollupOptions: {
			input: "ui/src/main.tsx"
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
