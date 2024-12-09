import { phoriaReact } from "@phoria/phoria-react/vite"
import { phoria } from "@phoria/phoria/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"

// https://vite.dev/config/
export default defineConfig({
  publicDir: "public",
  plugins: [
    dotnetDevCerts(),
    phoria(),
    phoriaReact()
  ]
})
