# Workspaces

> [!NOTE]
> This guide is incomplete. It will cover monorepo and pnpm workspace layouts, and how Phoria resolves island components that live in a workspace package. For now, the [`with-workspace`](../../examples/with-workspace) example is a complete workspace layout that installs, builds and runs standalone.

An island component that lives in a workspace package needs that package named in the framework plugin's `workspacePackages` option, so that Phoria transforms the package's modules the way it transforms your own components. Ordinary dependencies are skipped, so they need no configuration.

## Related

- [Supported UI frameworks](./supported-ui-frameworks.md) — where the `workspacePackages` option is set for each framework plugin.
- [Configuration](./configuration.md) — the `root` option that decides where Phoria looks for your UI code.
- [Getting started](./getting-started.md) — the single-package layout that the first-run path uses.
