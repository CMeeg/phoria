import { useState } from "react"

export interface CounterProps {
  startAt?: number
}

export function Counter({ startAt = 0 }: CounterProps) {
  const [count, setCount] = useState(startAt)

  return (
    <button className="btn btn-primary rounded-pill px-3" type="button" onClick={() => setCount((value) => value + 1)}>
      Count is {count}
    </button>
  )
}
