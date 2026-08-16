# With Tailwind Example

This example demonstrates Tailwind CSS v4 with a React Phoria island in a standalone .NET web application.

## Tailwind

Tailwind is added through the official `@tailwindcss/vite` plugin. The application stylesheet imports Tailwind and scans both Razor Pages and UI sources with `@source` directives, so utilities used in Razor markup are included in the build without a legacy Tailwind configuration file.

## Usage

Install the published dependencies from the `WebApp` directory:

```bash
pnpm install
```

Run the example in development mode:

```bash
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp listens on port `5773` and the Phoria Server listens on port `5673`.

Build and preview the production output with:

```bash
pnpm build
pnpm preview
```

Run the smoke test against the Aspire-hosted app with:

```bash
pnpm test:e2e
```

The example can also be fetched directly with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-tailwind with-tailwind
```
