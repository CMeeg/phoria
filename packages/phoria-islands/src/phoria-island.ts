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

interface PhoriaIslandComponentEntry<F extends string, M extends PhoriaIslandComponentModule, T> {
	name: string
	framework: F
	loader: PhoriaIslandComponentLoader<M, T>
}

interface PhoriaIslandComponent<F extends string, T> {
	component: T
	componentName: string
	framework: F
	componentPath?: string
}

async function importComponent<F extends string, T>(
	componentEntry: PhoriaIslandComponentEntry<F, PhoriaIslandComponentModule, T>
): Promise<PhoriaIslandComponent<F, T>> {
	if (typeof componentEntry.loader === "function") {
		const defaultExportModule = await componentEntry.loader()

		if (typeof defaultExportModule.default === "undefined") {
			throw new Error(
				`"${componentEntry.name}" component must be exposed as the default export for the specified module import, or you must also specify the named export that exposes the component when registering the component.`
			)
		}

		return {
			component: defaultExportModule.default,
			componentName: componentEntry.name,
			framework: componentEntry.framework,
			componentPath: defaultExportModule.__phoriaComponentPath
		}
	}

	const { loader } = componentEntry

	const namedExportModule = await loader.module()

	return {
		component: loader.component(namedExportModule),
		componentName: componentEntry.name,
		framework: componentEntry.framework,
		componentPath: namedExportModule.__phoriaComponentPath
	}
}

export { importComponent }

export type {
	PhoriaIslandComponent,
	PhoriaIslandComponentEntry,
	PhoriaIslandComponentDefaultModule,
	PhoriaIslandComponentDefaultModuleLoader,
	PhoriaIslandComponentLoader,
	PhoriaIslandComponentModule,
	PhoriaIslandComponentModuleLoader,
	PhoriaIslandProps
}
