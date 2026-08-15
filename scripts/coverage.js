// Normalizes coverage reports for Codecov upload.
// - lcov: prefixes SF: paths with the package directory so monorepo paths are unique
// - cobertura: drops NuGet dependency sources and strips the machine-specific absolute prefix
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs"
import { join } from "node:path"

const jsPackages = [
	"phoria-islands",
	"phoria-react",
	"phoria-svelte",
	"phoria-vue",
	"phoria-opentelemetry",
	"vite-plugin-dotnet-dev-certs"
]

for (const pkg of jsPackages) {
	const file = join("packages", pkg, "coverage", "lcov.info")
	if (!existsSync(file)) {
		continue
	}
	const fixed = readFileSync(file, "utf8")
		.split("\n")
		.map((line) => {
			if (!line.startsWith("SF:")) {
				return line
			}
			const path = line.replace(/^SF:/, "").replace(new RegExp(`^(packages/${pkg}/)+`), "")
			return `SF:packages/${pkg}/${path}`
		})
		.join("\n")
	writeFileSync(file, fixed)
}

mkdirSync("TestResults", { recursive: true })

for (const tfm of ["net8.0", "net10.0"]) {
	const dir = join("packages", "Phoria.Tests", "bin", "Release", tfm, "TestResults")
	if (!existsSync(dir)) {
		continue
	}
	const latest = readdirSync(dir)
		.filter((f) => f.startsWith("coverage.cobertura.") && f.endsWith(".xml"))
		.sort()
		.at(-1)
	if (!latest) {
		continue
	}
	const xml = readFileSync(join(dir, latest), "utf8")
		.replace(/<class\b[\s\S]*?filename="_\/[^"]*"[\s\S]*?<\/class>\s*/g, "")
		.replace(/filename="[^"]*?\/(packages\/Phoria\/[^"]*)"/g, 'filename="$1"')
	writeFileSync(join("TestResults", `${tfm}.cobertura.xml`), xml)
}
