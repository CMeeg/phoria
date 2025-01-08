import "@phoria/phoria-react/server"
import "@phoria/phoria-svelte/server"
import "@phoria/phoria-vue/server"
import "./components/register"
import type { PhoriaIsland } from "@phoria/phoria/server"

async function renderPhoriaIsland(phoriaIsland: PhoriaIsland) {
	return phoriaIsland.render()
}

export { renderPhoriaIsland }
