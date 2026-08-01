# Architecture

Phoria is an Islands architecture framework for .NET web applications,
powered by Vite. A .NET Razor Pages or MVC application renders the host page
and communicates with a Phoria Server process for island SSR, while Vite
produces the client and server bundles used by the islands.

The supported UI integrations are React, Svelte, and Vue. The main runtime
boundaries are:

- The .NET `Phoria` package, which provides configuration, middleware,
  TagHelpers, SSR request handling, and server-process management.
- The core `@phoria/phoria` package, which provides the Vite plugin, component
  registration, island client runtime, and island server runtime.
- Framework packages (`@phoria/phoria-react`, `@phoria/phoria-svelte`, and
  `@phoria/phoria-vue`), which connect each UI framework to the core runtime.
- The Vite dev-cert plugin, which configures development HTTPS using .NET
  development certificates.

The production build uses Vite's Environment API to build the client, SSR, and
Phoria Server environments in dependency order. The client build emits the
manifest consumed by the SSR and .NET integration.

This document is intentionally a concise orientation. Detailed behavior and
configuration are documented in [`docs/guides/`](guides/), while milestone
scope and unresolved design questions are tracked in [`PROJECT.md`](PROJECT.md).
