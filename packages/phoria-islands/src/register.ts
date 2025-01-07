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

interface PhoriaIslandComponentOptions<M extends PhoriaIslandComponentModule, T> {
	loader: PhoriaIslandComponentLoader<M, T>
	framework: string
}

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

type PhoriaIslandCsrMountMode = keyof typeof csrMountMode

interface PhoriaIslandCsrOptions {
	mode: PhoriaIslandCsrMountMode
}

// TODO: Relocate these to a client entry?
interface PhoriaIslandComponentCsrService {
	mount: (
		island: HTMLElement,
		component: PhoriaIslandComponent<PhoriaIslandComponentModule, unknown>,
		props: PhoriaIslandProps,
		options?: Partial<PhoriaIslandCsrOptions>
	) => Promise<void>
}

interface PhoriaIslandSsrResult {
	framework: string
	componentPath?: string
	html: string | ReadableStream
}

// TODO: Relocate these to the server entry?
interface PhoriaIslandComponentSsrService {
	render: (
		component: PhoriaIslandComponent<PhoriaIslandComponentModule, unknown>,
		props: PhoriaIslandProps
	) => Promise<PhoriaIslandSsrResult>
}

const frameworkRegistry = new Set<string>()

function getFramework(name: string) {
	const framework = name.toLowerCase()

	return frameworkRegistry.has(framework) ? framework : undefined
}

function registerFramework(name: string) {
	let framework = getFramework(name)

	if (typeof framework !== "undefined") {
		return framework
	}

	framework = name.toLowerCase()

	frameworkRegistry.add(framework)

	return framework
}

function getFrameworks() {
	return Array.from(frameworkRegistry.values())
}

const ssrServiceRegistry = new Map<string, PhoriaIslandComponentSsrService>()

function registerSsrService(framework: string, service: PhoriaIslandComponentSsrService) {
	const frameworkName = registerFramework(framework)

	ssrServiceRegistry.set(frameworkName, service)
}

function getSsrService(framework: string) {
	const frameworkName = getFramework(framework)

	if (typeof frameworkName === "undefined") {
		throw new Error(`Framework "${framework}" has not been registered.`)
	}

	return ssrServiceRegistry.get(frameworkName)
}

const csrMountMode = {
	render: "render",
	hydrate: "hydrate"
} as const

const csrServiceRegistry = new Map<string, PhoriaIslandComponentCsrService>()

function registerCsrService(framework: string, service: PhoriaIslandComponentCsrService) {
	const frameworkName = registerFramework(framework)

	csrServiceRegistry.set(frameworkName, service)
}

function getCsrService(framework: string) {
	const frameworkName = getFramework(framework)

	if (typeof frameworkName === "undefined") {
		throw new Error(`Framework "${framework}" has not been registered.`)
	}

	return csrServiceRegistry.get(frameworkName)
}

function createIslandImport<T>(
	component: PhoriaIslandComponent<PhoriaIslandComponentModule, unknown>
): PhoriaIslandImport<T> {
	if (typeof component.loader === "function") {
		return component.loader().then((module) => {
			if (typeof module.default === "undefined") {
				throw new Error(
					`"${component.name}" component must be exposed as the default export for the specified module import, or you must also specify the named export that exposes the component when registering the component.`
				)
			}

			return {
				component: module.default as T,
				componentPath: module.__phoriaComponentPath
			}
		})
	}

	const { loader } = component

	return loader.module().then((module) => ({
		component: loader.component(module) as T,
		componentPath: module.__phoriaComponentPath
	}))
}

const componentRegistry = new Map<string, PhoriaIslandComponent<PhoriaIslandComponentModule, unknown>>()

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

	const islandComponent = {
		name,
		framework: component.framework,
		loader: component.loader as PhoriaIslandComponentLoader<PhoriaIslandComponentModule, T>
	}

	componentRegistry.set(name.toLowerCase(), islandComponent)

	return islandComponent
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
	getFrameworks,
	registerComponent,
	registerComponents,
	getComponent,
	createIslandImport,
	registerCsrService,
	getCsrService,
	csrMountMode,
	registerSsrService,
	getSsrService
}

export type {
	PhoriaIslandComponent,
	PhoriaIslandComponentModule,
	PhoriaIslandComponentOptions,
	PhoriaIslandProps,
	PhoriaIsland,
	PhoriaIslandImport,
	PhoriaIslandComponentCsrService,
	PhoriaIslandCsrOptions,
	PhoriaIslandCsrMountMode,
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult
}
