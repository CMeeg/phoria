import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"
import dts from "vite-plugin-dts"

export default defineConfig({
	plugins: [react(), dts({ entryRoot: "src" })],
	build: {
		lib: {
			entry: "src/index.ts",
			formats: ["es"],
			fileName: "index"
		},
		rolldownOptions: {
			external: ["react", "react-dom", "react/jsx-runtime"]
		}
	}
})
