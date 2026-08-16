function streamEnabled(stream) {
	if (process.env.FORCE_COLOR) {
		return process.env.FORCE_COLOR !== "0"
	}

	return !process.env.NO_COLOR && Boolean(stream.isTTY)
}

function paint(stream, code, text) {
	return streamEnabled(stream) ? `\x1b[${code}m${text}\x1b[0m` : text
}

export function step(text) {
	console.log(`\n${paint(process.stdout, "1;36", text)}`)
}

export function success(text) {
	console.log(`\n${paint(process.stdout, "32", `✅ ${text}`)}\n`)
}

export function error(text) {
	console.error(`\n${paint(process.stderr, "31", `❌ ${text}`)}\n`)
}

export function info(text) {
	console.log(`\n${paint(process.stdout, "2", text)}`)
}

export function command(text) {
	console.log(`\n${paint(process.stdout, "2", `$ ${text}`)}`)
}
