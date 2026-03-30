# open-rigs

Standard, shareable descriptions of experimental rigs for the open-rigs ecosystem.

---

## Regenerating packages

### 1. Regenerate the PyRAT JSON schema (Python → JSON)

Run whenever `SessionConfig` or any model in `src/open_rigs/pyrat/` changes to generate the JSON classes:

```bash
uv run python src/open_rigs/_generators/session_schema.py
```
---
### 2. Regenerate the C# Bonsai types (JSON → C#)

Run after step 1 to update the generated C# classes into the bonsai package:

```bash
dotnet bonsai.sgen src/open_rigs/schemas/pyrat_session.json --namespace OpenRigs.Pyrat -o src/OpenRigs.Pyrat --serializer json
```
---
### 3. Repack the NuGet packages

Run after step 2 to publish all updated packages to the local Bonsai feed:

```bash
dotnet pack OpenRigs.sln -c Release
```

Or individually if only one package changed:

```bash
dotnet pack src/OpenRigs.Core/OpenRigs.Core.csproj       -c Release
dotnet pack src/OpenRigs.Devices/OpenRigs.Devices.csproj -c Release
dotnet pack src/OpenRigs.Logging/OpenRigs.Logging.csproj -c Release
dotnet pack src/OpenRigs.Video/OpenRigs.Video.csproj     -c Release
dotnet pack src/OpenRigs.Vision/OpenRigs.Vision.csproj   -c Release
dotnet pack src/OpenRigs.Pyrat/OpenRigs.Pyrat.csproj     -c Release
```

Output: `artifacts/package/release/*.nupkg`

Configure local Bonsai config (`.bonsai/NuGet.config`) to pick up packages from that folder.

```bash 
<add key="OpenRigs" value="../artifacts/package/release" />
```