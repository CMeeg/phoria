import { type Span, trace } from "@opentelemetry/api"
import { getResponseHeader, type H3Event } from "h3"

interface PhoriaRequestSpanHookOptions {
	base: string
	ssrBase: string
}

function createPhoriaRequestSpanHook({ base, ssrBase }: PhoriaRequestSpanHookOptions) {
	function onRequest(event: H3Event): void {
		event.context.phoriaSpan = trace.getActiveSpan()
	}

	function onBeforeResponse(event: H3Event): void {
		const span = event.context.phoriaSpan as Span | undefined

		if (!span) {
			return
		}

		if (event.method === "POST" && event.path.startsWith(`${ssrBase}/render/`)) {
			const component = event.path.slice(`${ssrBase}/render/`.length)

			span.updateName("phoria-server.ssr.render")
			span.setAttribute("phoria.component", component)

			const framework = getResponseHeader(event, "x-phoria-island-framework")
			if (framework !== undefined) {
				span.setAttribute("phoria.framework", framework)
			}

			return
		}

		if (event.method === "GET" && event.path.startsWith(base)) {
			span.updateName("phoria-server.csr.asset")
		}
	}

	return { onRequest, onBeforeResponse }
}

export { createPhoriaRequestSpanHook }
