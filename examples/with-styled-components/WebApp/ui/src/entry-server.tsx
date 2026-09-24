import "@phoria/phoria-react/server"
import "./components/register"
import type { PhoriaIsland } from "@phoria/phoria/server"
import { isReactIsland } from "@phoria/phoria-react/server"
import { renderWithStyledComponents } from "./server/ssr"

async function renderPhoriaIsland(island: PhoriaIsland) {
  if (isReactIsland(island)) {
    return await island.render({
      renderComponent: renderWithStyledComponents,
    })
  }

  return await island.render()
}

export { renderPhoriaIsland }
