import { mkdir, mkdtemp, rm, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { x } from "tinyexec"
import type { Plugin, UserConfig } from "vite"
import { afterEach, describe, expect, it, vi } from "vitest"
import { dotnetDevCerts } from "./plugin"

vi.mock("tinyexec", () => ({ x: vi.fn() }))

const directories: string[] = []

type TestPlugin = Omit<Plugin, "config"> & {
	config?: (config: UserConfig, env: { mode: string; command: "build" | "serve" }) => Promise<void>
}

const asTestPlugin = (plugin: Plugin) => plugin as unknown as TestPlugin

afterEach(async () => {
	vi.clearAllMocks()
	vi.unstubAllEnvs()
	for (const directory of directories.splice(0)) await rm(directory, { recursive: true, force: true })
})

describe("dotnetDevCerts plugin", () => {
	it("returns a vite plugin with the expected name", () => {
		const plugin = dotnetDevCerts() as Plugin
		expect(plugin).toHaveProperty("name")
		expect(plugin.name).toBe("dotnet-dev-certs")
	})

	it("does nothing outside development mode", async () => {
		const plugin = asTestPlugin(dotnetDevCerts({ basePath: "/missing", certificateName: "test" }) as Plugin)
		const config: UserConfig = { server: { port: 1234 } }

		await plugin.config?.(config, { mode: "production", command: "build" })

		expect(config.server).toEqual({ port: 1234 })
		expect(x).not.toHaveBeenCalled()
	})

	it("reuses existing certificates and preserves server configuration", async () => {
		const basePath = await mkdtemp(join(tmpdir(), "dotnet-certs-"))
		directories.push(basePath)
		await writeFile(join(basePath, "app.pem"), "cert")
		await writeFile(join(basePath, "app.key"), "key")
		const plugin = asTestPlugin(dotnetDevCerts({ basePath, certificateName: "app" }) as Plugin)
		const config: UserConfig = { server: { port: 1234, strictPort: true } }

		await plugin.config?.(config, { mode: "development", command: "serve" })

		expect(config.server).toMatchObject({
			port: 1234,
			strictPort: true,
			https: { cert: join(basePath, "app.pem"), key: join(basePath, "app.key") }
		})
		expect(x).not.toHaveBeenCalled()
	})

	it("exports missing certificates with the expected dotnet arguments", async () => {
		const basePath = await mkdtemp(join(tmpdir(), "dotnet-certs-"))
		directories.push(basePath)
		vi.mocked(x).mockResolvedValue({ exitCode: 0, stderr: "" } as never)
		const plugin = asTestPlugin(dotnetDevCerts({ basePath, certificateName: "app" }) as Plugin)
		const config: UserConfig = {}

		await plugin.config?.(config, { mode: "development", command: "serve" })

		expect(x).toHaveBeenCalledWith("dotnet", [
			"dev-certs",
			"https",
			"--export-path",
			join(basePath, "app.pem"),
			"--format",
			"Pem",
			"--no-password"
		])
		expect(config.server?.https).toEqual({ cert: join(basePath, "app.pem"), key: join(basePath, "app.key") })
	})

	it("throws when dotnet certificate export fails", async () => {
		const basePath = await mkdtemp(join(tmpdir(), "dotnet-certs-"))
		directories.push(basePath)
		vi.mocked(x).mockResolvedValue({ exitCode: 1, stderr: "failed" } as never)

		await expect(
			asTestPlugin(dotnetDevCerts({ basePath, certificateName: "app" }) as Plugin).config?.(
				{},
				{ mode: "development", command: "serve" }
			)
		).rejects.toThrow("Failed to generate dotnet dev certs")
	})

	it("throws when the configured certificate base path is missing", async () => {
		await expect(
			asTestPlugin(dotnetDevCerts({ basePath: "/missing", certificateName: "app" }) as Plugin).config?.(
				{},
				{ mode: "development", command: "serve" }
			)
		).rejects.toThrow("base path for the dotnet dev certs does not exist")
	})

	it("normalizes a scoped package name", async () => {
		const cwd = await mkdtemp(join(tmpdir(), "dotnet-package-"))
		const basePath = await mkdtemp(join(tmpdir(), "dotnet-certs-"))
		directories.push(cwd, basePath)
		await writeFile(join(cwd, "package.json"), JSON.stringify({ name: "@x/y" }))
		vi.mocked(x).mockResolvedValue({ exitCode: 0, stderr: "" } as never)

		await asTestPlugin(dotnetDevCerts({ cwd, basePath }) as Plugin).config?.(
			{},
			{ mode: "development", command: "serve" }
		)

		expect(x).toHaveBeenCalledWith("dotnet", expect.arrayContaining([join(basePath, "x_y.pem")]))
	})

	it("throws when package metadata is missing or invalid", async () => {
		const cwd = await mkdtemp(join(tmpdir(), "dotnet-package-"))
		const basePath = await mkdtemp(join(tmpdir(), "dotnet-certs-"))
		directories.push(cwd, basePath)
		const plugin = asTestPlugin(dotnetDevCerts({ cwd, basePath }) as Plugin)

		await expect(plugin.config?.({}, { mode: "development", command: "serve" })).rejects.toThrow(
			"Could not find a package.json"
		)
		await writeFile(join(cwd, "package.json"), JSON.stringify({ private: true }))
		await expect(plugin.config?.({}, { mode: "development", command: "serve" })).rejects.toThrow(
			"does not contain a name"
		)
	})

	it("uses the APPDATA certificate location when present", async () => {
		const appData = await mkdtemp(join(tmpdir(), "dotnet-appdata-"))
		const basePath = join(appData, "ASP.NET", "https")
		directories.push(appData)
		await mkdir(basePath, { recursive: true })
		await writeFile(join(basePath, "app.pem"), "cert")
		await writeFile(join(basePath, "app.key"), "key")
		vi.stubEnv("APPDATA", appData)

		const config: UserConfig = {}
		await asTestPlugin(dotnetDevCerts({ certificateName: "app" }) as Plugin).config?.(config, {
			mode: "development",
			command: "serve"
		})

		expect(config.server?.https).toEqual({ cert: join(basePath, "app.pem"), key: join(basePath, "app.key") })
	})

	it("uses the non-Linux certificate location when APPDATA is absent", async () => {
		vi.resetModules()
		vi.doMock("std-env", () => ({ isLinux: false }))
		const { dotnetDevCerts: dotnetDevCertsWithoutLinux } = await import("./plugin")
		const home = await mkdtemp(join(tmpdir(), "dotnet-home-"))
		const basePath = join(home, ".aspnet", "dev-certs", "https")
		directories.push(home)
		await mkdir(basePath, { recursive: true })
		await writeFile(join(basePath, "app.pem"), "cert")
		await writeFile(join(basePath, "app.key"), "key")
		vi.stubEnv("APPDATA", "")
		vi.stubEnv("HOME", home)

		const config: UserConfig = {}
		await asTestPlugin(dotnetDevCertsWithoutLinux({ certificateName: "app" }) as Plugin).config?.(config, {
			mode: "development",
			command: "serve"
		})

		expect(config.server?.https).toEqual({ cert: join(basePath, "app.pem"), key: join(basePath, "app.key") })
	})
})
