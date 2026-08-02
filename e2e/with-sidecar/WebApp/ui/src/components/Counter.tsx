import { useState } from "react"

interface CounterProps {
	startAt?: number
}

function Counter({ startAt = 0 }: CounterProps) {
	const [count, setCount] = useState(startAt)

	return (
		<button type="button" onClick={() => setCount((value) => value + 1)}>
			count is {count}
		</button>
	)
}

export { Counter }
