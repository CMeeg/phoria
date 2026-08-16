import { useState } from "react"
import reactLogo from "/react.svg"

export interface CounterProps {
  startAt?: number
}

export function Counter({ startAt }: CounterProps) {
  const [count, setCount] = useState(startAt ?? 0)

  return (
    <div className="react-counter">
      <a href="https://react.dev" target="_blank" rel="noreferrer" className="hover:drop-shadow-[0_0_2em_#61dafbaa]">
        <img
          src={reactLogo}
          className="motion-safe:animate-spin-slow duration-20000 inline h-24 max-w-none"
          alt="React logo"
        />
      </a>
      <button type="button" className="block mx-auto my-6" onClick={() => setCount((count) => count + 1)}>
        count is {count}
      </button>
      <p>
        Edit <code>ui/src/components/counter/counter.tsx</code> to test HMR
      </p>
    </div>
  )
}
