type PhoriaIslandProps = Record<string, unknown> | null

type PhoriaIslandComponentModule = {
	[key: string]: unknown
	__phoriaComponentPath?: string
}

type PhoriaIslandComponentDefaultModule<T> = PhoriaIslandComponentModule & {
	default?: T
}

type PhoriaIslandComponentModuleLoader<M extends PhoriaIslandComponentModule, T> = {
	module: () => Promise<M>
	component: (module: M) => T
}

type PhoriaIslandComponentDefaultModuleLoader<T> = () => Promise<PhoriaIslandComponentDefaultModule<T>>

type PhoriaIslandComponentLoader<M extends PhoriaIslandComponentModule, T> =
	| PhoriaIslandComponentModuleLoader<M, T>
	| PhoriaIslandComponentDefaultModuleLoader<T>

interface PhoriaIslandComponent<M extends PhoriaIslandComponentModule, T> {
	name: string
	framework: string
	loader: PhoriaIslandComponentLoader<M, T>
}

interface PhoriaIsland<T> {
	component: T
	componentPath?: string
}

type PhoriaIslandImport<T> = Promise<PhoriaIsland<T>>

async function createIslandImport<T>(
	component: PhoriaIslandComponent<PhoriaIslandComponentModule, T>
): PhoriaIslandImport<T> {
	if (typeof component.loader === "function") {
		const defaultExportModule = await component.loader()

		if (typeof defaultExportModule.default === "undefined") {
			throw new Error(
				`"${component.name}" component must be exposed as the default export for the specified module import, or you must also specify the named export that exposes the component when registering the component.`
			)
		}

		return {
			component: defaultExportModule.default,
			componentPath: defaultExportModule.__phoriaComponentPath
		}
	}

	const { loader } = component

	const namedExportModule = await loader.module()

	return {
		component: loader.component(namedExportModule),
		componentPath: namedExportModule.__phoriaComponentPath
	}
}

export { createIslandImport }

export type {
	PhoriaIsland,
	PhoriaIslandComponent,
	PhoriaIslandComponentDefaultModule,
	PhoriaIslandComponentDefaultModuleLoader,
	PhoriaIslandComponentLoader,
	PhoriaIslandComponentModule,
	PhoriaIslandComponentModuleLoader,
	PhoriaIslandImport,
	PhoriaIslandProps
}
