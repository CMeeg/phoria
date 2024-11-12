// TODO: Not sure on the structure of the registries - will need to feel it out as we go

interface PhoriaIslandFramework<T> {
	createComponent: (component: PhoriaIslandComponentOptions<PhoriaIslandComponentModule, T>) => PhoriaIslandComponent
}

// TODO: Can I get rid of the "unknown" types here?
const frameworkRegistry = new Map<string, PhoriaIslandFramework<unknown>>()

function registerFramework<T>(name: string, framework: PhoriaIslandFramework<T>) {
	frameworkRegistry.set(name.toLowerCase(), framework as PhoriaIslandFramework<unknown>)
}

function getFramework(name: string) {
	return frameworkRegistry.get(name.toLowerCase())
}

function getFrameworks() {
	return Array.from(frameworkRegistry.keys())
}

type PhoriaIslandComponentModule = {
	componentPath?: string
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

interface PhoriaIslandComponentOptions<M extends PhoriaIslandComponentModule, T> {
	loader: PhoriaIslandComponentLoader<M, T>
	framework: string
}

const componentRegistry = new Map<string, PhoriaIslandComponent>()

interface PhoriaIsland<T> {
	component: T
	componentPath?: string
}

type PhoriaIslandImport<T> = Promise<PhoriaIsland<T>>

function getIslandImport<T>(
	component: PhoriaIslandComponentOptions<PhoriaIslandComponentModule, T>
): PhoriaIslandImport<T> {
	if (typeof component.loader === "function") {
		return component.loader().then((module) => {
			if (typeof module.default === "undefined") {
				// TODO: Can we make the component name available here so this error is more useful?
				throw new Error("Component module must have a default export.")
			}

			return {
				component: module.default,
				componentPath: module.componentPath
			}
		})
	}

	const { loader } = component
	return loader.module().then((module) => ({
		component: loader.component(module),
		componentPath: module.componentPath
	}))
}

interface PhoriaIslandRenderOptions {
	preferStream?: boolean
}

type PhoriaIslandProps = Record<string, unknown> | null

interface PhoriaIslandRenderResult {
	framework: string
	componentPath?: string
	html: string | ReadableStream
}

interface PhoriaIslandComponent {
	framework: string
	mount: <P extends PhoriaIslandProps>(
		container: HTMLElement,
		props?: P,
		hydrate?: boolean
	) => Promise<void>
	render: <P extends PhoriaIslandProps>(
		props: P,
		options?: PhoriaIslandRenderOptions
	) => Promise<PhoriaIslandRenderResult>
}

function registerComponent<M extends PhoriaIslandComponentModule, T>(
	name: string,
	component: PhoriaIslandComponentOptions<M, T>
) {
	const framework = getFramework(component.framework)

	if (!framework) {
		throw new Error(
			`Cannot register component "${name}" because the "${component.framework}" framework has not been registered.`
		)
	}

	// TODO: Can I get rid of the "unknown" type conversion here?
	const frameworkComponent = framework.createComponent(
		component as unknown as PhoriaIslandComponentOptions<PhoriaIslandComponentModule, T>
	)

	componentRegistry.set(name.toLowerCase(), frameworkComponent)
}

function registerComponents<M extends PhoriaIslandComponentModule>(
	components: Record<string, PhoriaIslandComponentOptions<M, unknown>>
) {
	for (const [name, component] of Object.entries(components)) {
		registerComponent(name, component)
	}
}

function getComponent(name: string) {
	return componentRegistry.get(name.toLowerCase())
}

export {
	registerFramework,
	getFramework,
	getFrameworks,
	registerComponent,
	registerComponents,
	getComponent,
	getIslandImport
}

export type {
	PhoriaIslandFramework,
	PhoriaIslandComponent,
	PhoriaIslandProps,
	PhoriaIslandImport,
	PhoriaIsland,
	PhoriaIslandRenderOptions,
	PhoriaIslandRenderResult
}
