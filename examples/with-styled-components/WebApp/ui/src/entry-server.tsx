import "@phoria/phoria-react/server"
import "./components/register"
import type { PhoriaIsland } from "@phoria/phoria/server"
import { isReactIsland } from "@phoria/phoria-react/server"
import { renderToString } from "react-dom/server"
import { ServerStyleSheet, StyleSheetManager } from "styled-components"

async function renderPhoriaIsland(island: PhoriaIsland) {
  if (!isReactIsland(island)) {
    return await island.render()
  }

  const sheet = new ServerStyleSheet()

  try {
    const result = await island.render({
      renderComponent: (reactIsland, props) =>
        renderToString(
          <StyleSheetManager sheet={sheet.instance}>
            <reactIsland.component {...props} />
          </StyleSheetManager>,
        ),
    })

    return { ...result, html: sheet.getStyleTags() + result.html }
  } finally {
    sheet.seal()
  }
}

export { renderPhoriaIsland }
