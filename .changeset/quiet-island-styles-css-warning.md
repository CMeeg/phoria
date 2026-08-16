---
phoria-dotnet: patch
---

Suppress the "doesn't have CSS chunks" warning for the `<phoria-island-styles/>` element. Rendering nothing when the entry has no CSS is expected for this optional element, so the warning is now only logged when an explicit `<link rel="stylesheet" phoria-href="...">` references an entry without CSS.
