# Central Event ID Registry

## Goal

Replace composite `EventFeature + offset` expressions with direct, full numeric event ID constants while preserving every currently emitted event ID.

## Design

`packages/Phoria/Logging/EventId.cs` owns the event ID registry. It exposes nested feature groups under `EventId`, such as `EventId.Server`, `EventId.Islands`, and `EventId.Vite`. Each group contains named `const int` values with the complete emitted ID, not a local offset.

For example:

```csharp
public static class EventId
{
    public static class Server
    {
        public const int ProcessStarting = 1214;
    }
}
```

All in-scope `[LoggerMessage]` attributes and `LoggerMessage.Define` calls reference these constants directly. Message templates, log levels, method names, and runtime behavior remain unchanged.

The existing feature bases are no longer used to compose event IDs. They may be removed if they have no remaining consumers; no replacement compatibility API is added because these constants are internal implementation details of the published assembly's logging implementation.

## Compatibility

The numeric IDs remain unchanged, including feature grouping and documented gaps. Existing log consumers continue receiving the same event IDs.

## Scope

Update the central registry and the seven logging files currently using named offsets. Do not change unrelated logging behavior or introduce a new logging abstraction.

## Verification

- Confirm every in-scope event uses a direct `EventId.*` constant.
- Confirm no in-scope `EventFeature + ...` or equivalent composite event ID expression remains.
- Build `packages/Phoria/Phoria.csproj` in Release with zero warnings.
- Run the full .NET test solution on both target frameworks.
