# With Tailwind Example

This example demonstrates Tailwind CSS v4 with a React Phoria island in a standalone .NET web application. Tailwind is loaded through `@tailwindcss/vite`; the stylesheet uses `@source` directives to scan Razor and UI sources without a legacy Tailwind configuration file.

Run these commands from `examples/with-tailwind/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

The WebApp is at `http://localhost:5773`; the Phoria Server is at `http://localhost:5673`. Use `pnpm preview` after the build and `pnpm stop` to stop the AppHost. With the app running, `pnpm test:e2e` checks the React island, Tailwind-rendered page, modulepreload output, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5773`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-tailwind with-tailwind
```
