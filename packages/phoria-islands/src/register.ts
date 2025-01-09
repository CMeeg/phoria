import type { PhoriaIslandComponentCsrService } from "./client/csr"
import type {
	PhoriaIslandComponentEntry,
	PhoriaIslandComponentLoader,
	PhoriaIslandComponentModule
} from "./phoria-island"
import type { PhoriaIslandComponentSsrService } from "./server/ssr"

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

// biome-ignore lint/suspicious/noExplicitAny: The registry must be able to store any type of service
const ssrServiceRegistry = new Map<string, PhoriaIslandComponentSsrService<any>>()

function registerSsrService<T>(framework: string, service: PhoriaIslandComponentSsrService<T>) {
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

// biome-ignore lint/suspicious/noExplicitAny: The registry must be able to store any type of service
const csrServiceRegistry = new Map<string, PhoriaIslandComponentCsrService<any>>()

function registerCsrService<T>(framework: string, service: PhoriaIslandComponentCsrService<T>) {
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

// biome-ignore lint/suspicious/noExplicitAny: The registry must be able to store any type of component
const componentRegistry = new Map<string, PhoriaIslandComponentEntry<PhoriaIslandComponentModule, any>>()

interface PhoriaIslandComponentOptions<M extends PhoriaIslandComponentModule, T> {
	loader: PhoriaIslandComponentLoader<M, T>
	framework: string
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
	getComponent,
	getCsrService,
	getFrameworks,
	getSsrService,
	registerComponent,
	registerComponents,
	registerCsrService,
	registerSsrService
}

export type { PhoriaIslandComponentOptions }
