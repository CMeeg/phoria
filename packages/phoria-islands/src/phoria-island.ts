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

interface PhoriaIslandComponentEntry<M extends PhoriaIslandComponentModule, T> {
	name: string
	framework: string
	loader: PhoriaIslandComponentLoader<M, T>
}

interface PhoriaIslandComponent<T> {
	component: T
	componentName: string
	framework: string
	componentPath?: string
}

async function importComponent<T>(
	componentEntry: PhoriaIslandComponentEntry<PhoriaIslandComponentModule, T>
): Promise<PhoriaIslandComponent<T>> {
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
