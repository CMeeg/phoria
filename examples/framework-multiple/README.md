# Framework Multiple Example

This example demonstrates React, Svelte, and Vue Phoria islands together in a single standalone .NET web application, with OpenTelemetry wiring and the observable Phoria Server. The page renders three island counters with different interaction patterns: a React counter mounting with `Client.Load`, a Vue counter mounting with `Client.Idle`, and a Svelte counter mounting with `Client.Visible`. Each framework is registered in `ui/src/components/register.ts` and rendered through the `phoria-island` tag helper.

Run these commands from `examples/framework-multiple/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp is at `http://localhost:5573`; the Phoria Server is at `http://localhost:5473`.

After a build, exercise the production-shaped flow with `pnpm preview`. Stop it with `pnpm stop`. With the app running, run `pnpm test:e2e`; the suite checks rendered island markup for each framework, modulepreload output, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5573`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/framework-multiple framework-multiple
```