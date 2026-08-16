import type { RenderReactPhoriaIslandComponent } from "@phoria/phoria-react/server"
import { renderToString } from "react-dom/server"
import { ServerStyleSheet, StyleSheetManager } from "styled-components"

const renderWithStyledComponents: RenderReactPhoriaIslandComponent = async (island, props) => {
  const sheet = new ServerStyleSheet()

  try {
    const html = renderToString(
      <StyleSheetManager sheet={sheet.instance}>
        <island.component {...props} />
      </StyleSheetManager>,
    )

    return html + sheet.getStyleTags()
  } finally {
    sheet.seal()
  }
}

export { renderWithStyledComponents }
