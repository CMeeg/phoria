import type { ViteDevServer } from "vite"

type ViteModule = {
	createServer: (...args: never[]) => unknown
	isRunnableDevEnvironment: (...args: never[]) => boolean
}

interface PhoriaViteDevServer extends ViteDevServer {
	_vite: ViteModule
}

async function createPhoriaViteDevServer<T extends ViteModule>(vitePromise: Promise<T>): Promise<PhoriaViteDevServer> {
	const vite = await vitePromise
	const createServer = vite.createServer as (config: {
		appType: "custom"
		server: { middlewareMode: true }
	}) => Promise<ViteDevServer>
	const devServer = await createServer({
		appType: "custom",
		server: { middlewareMode: true }
	})

	return Object.assign(devServer, { _vite: vite })
}

export type { PhoriaViteDevServer }
export { createPhoriaViteDevServer }
