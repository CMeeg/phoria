# Central Event ID Registry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace composite `EventFeature + offset` logging IDs with a central nested registry of full numeric constants while preserving every emitted event ID.

**Architecture:** `Phoria.Logging.EventId` becomes the single registry. Its nested `Server`, `Islands`, and `Vite` classes contain named `const int` values for the complete IDs currently emitted in the 1200, 1100, and 1300 feature ranges. Every in-scope `LoggerMessage` declaration and `LoggerMessage.Define` call references those constants directly; message text and runtime behavior do not change.

**Tech Stack:** C# 13, .NET 8/.NET 10, `LoggerMessage` source generation, xUnit v3.

## Global Constraints

- Preserve every currently emitted numeric event ID, including feature grouping and documented gaps.
- `packages/Phoria/Logging/EventId.cs` owns the central registry and nested feature groups.
- In-scope logger declarations use direct `EventId.*` constants, never composite arithmetic expressions.
- Do not change log levels, message templates, method names, or runtime logging behavior.
- Do not introduce a new logging abstraction or change unrelated logging code.
- Build `packages/Phoria/Phoria.csproj` in Release with zero warnings.
- Run the full .NET test solution on both target frameworks.

---

### Task 1: Centralize Full Event IDs

**Files:**
- Modify: `packages/Phoria/Logging/EventId.cs`
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs`
- Modify: `packages/Phoria/Server/PhoriaServerMonitor.cs`
- Modify: `packages/Phoria/Server/PhoriaServerMiddleware.cs`
- Modify: `packages/Phoria/Server/ViteDevServerHmrProxy.cs`
- Modify: `packages/Phoria/Islands/PhoriaIslandEntryTagHelper.cs`
- Modify: `packages/Phoria/Islands/PhoriaIslandComponentFactory.cs`
- Modify: `packages/Phoria/Vite/ViteManifestReader.cs`
- Modify: `packages/Phoria/Vite/ViteSsrManifestReader.cs`
- Test: `packages/Phoria.Tests/` existing logging/process/island tests as needed

**Interfaces:**
- Consumes: The existing event names and numeric values from the current `EventFeature` plus per-file offsets.
- Produces: `Phoria.Logging.EventId.Server`, `Phoria.Logging.EventId.Islands`, and `Phoria.Logging.EventId.Vite` nested classes with direct full-ID constants.

- [ ] **Step 1: Record the existing numeric contract**

  Use the current feature bases and offsets to preserve these exact values:

  ```text
  EventId.Islands:
    EntryAttributeMissing = 1101
    ViteManifestKeyNotFound = 1102
    ManifestEntryDoesntHaveCssChunks = 1103
    ServerUnhealthyDegradingToClient = 1104

  EventId.Server:
    MiddlewareProxyViaHttpError = 1201
    ServerIsHealthy = 1202
    ServerIsUnhealthy = 1203
    EstablishingWebSocketProxy = 1204
    FailedToEstablishWebSocketProxy = 1205
    FailedToCloseWebSocket = 1206
    ProcessNotConfigured = 1207
    ProcessIsHealthy = 1208
    ProcessIsRunning = 1209
    ProcessStdOut = 1210
    ProcessStdErr = 1211
    ProcessExited = 1212
    ProcessException = 1213
    ProcessStarting = 1214
    ProcessTerminationSignalSent = 1215
    ProcessForceStopped = 1216

  EventId.Vite:
    ManifestFileWontBeRead = 1301
    DetectedChangeInManifest = 1302
    ManifestFileNotFound = 1303
    SsrManifestFileWontBeRead = 1304
    DetectedChangeInSsrManifest = 1305
    SsrManifestFileNotFound = 1306
  ```

- [ ] **Step 2: Replace the feature/offset registry**

  Change `EventId.cs` to define the full values in one place:

  ```csharp
  namespace Phoria.Logging;

  public static class EventId
  {
      public static class Islands
      {
          public const int EntryAttributeMissing = 1101;
          public const int ViteManifestKeyNotFound = 1102;
          public const int ManifestEntryDoesntHaveCssChunks = 1103;
          public const int ServerUnhealthyDegradingToClient = 1104;
      }

      public static class Server
      {
          public const int MiddlewareProxyViaHttpError = 1201;
          public const int ServerIsHealthy = 1202;
          public const int ServerIsUnhealthy = 1203;
          public const int EstablishingWebSocketProxy = 1204;
          public const int FailedToEstablishWebSocketProxy = 1205;
          public const int FailedToCloseWebSocket = 1206;
          public const int ProcessNotConfigured = 1207;
          public const int ProcessIsHealthy = 1208;
          public const int ProcessIsRunning = 1209;
          public const int ProcessStdOut = 1210;
          public const int ProcessStdErr = 1211;
          public const int ProcessExited = 1212;
          public const int ProcessException = 1213;
          public const int ProcessStarting = 1214;
          public const int ProcessTerminationSignalSent = 1215;
          public const int ProcessForceStopped = 1216;
      }

      public static class Vite
      {
          public const int ManifestFileWontBeRead = 1301;
          public const int DetectedChangeInManifest = 1302;
          public const int ManifestFileNotFound = 1303;
          public const int SsrManifestFileWontBeRead = 1304;
          public const int DetectedChangeInSsrManifest = 1305;
          public const int SsrManifestFileNotFound = 1306;
      }
  }
  ```

  Do not retain offset constants that invite composite expressions. Remove `EventFeature` if it has no remaining consumers after the reference migration.

- [ ] **Step 3: Update every in-scope logger declaration**

  Replace expressions such as:

  ```csharp
  EventId = EventFeature.Server + ServerEvent.ProcessStarting
  ```

  with:

  ```csharp
  EventId = EventId.Server.ProcessStarting
  ```

  Apply the same direct-reference form to `LoggerMessage.Define` calls and all seven logging files. Remove the per-file `ServerEvent`, `IslandsEvent`, and `ViteEvent` nested offset classes once no longer used. Preserve the existing event names, method signatures, levels, templates, and exception behavior.

- [ ] **Step 4: Verify no composite event IDs remain**

  Run:

  ```bash
  rg "EventFeature|EventId\s*=.*\+|LoggerMessage\.Define" packages/Phoria --glob '*.cs'
  ```

  Expected: `LoggerMessage.Define` may still appear, but every `eventId` argument is a direct `EventId.<Feature>.<Name>` constant and no `EventFeature` or per-file offset class remains.

- [ ] **Step 5: Build and test both target frameworks**

  Run:

  ```bash
  dotnet build packages/Phoria/Phoria.csproj --configuration Release
  dotnet test --solution Phoria.sln --configuration Release
  ```

  Expected: the package build has zero warnings/errors and both `net8.0` and `net10.0` test assemblies pass.

- [ ] **Step 6: Commit the implementation**

  ```bash
  git add packages/Phoria/Logging/EventId.cs packages/Phoria/Server packages/Phoria/Islands packages/Phoria/Vite
  git commit -m "refactor: centralize logger event ids"
  ```
