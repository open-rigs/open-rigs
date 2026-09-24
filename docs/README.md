# open-rigs docs

DocFX site sources for the [open-rigs documentation](https://open-rigs.github.io/open-rigs/).

## Building locally

Workflow SVGs are rendered artefacts - not committed to the repository. Generate them before serving:

```powershell
pwsh docs/build.ps1
dotnet docfx docs/docfx.json --serve
```

`build.ps1` Writes SVGs into `docs/assets/workflows/` and `docs/workflows/`. DocFX then serves the site at `http://localhost:8080`.

On CI, the `workflow-images` job renders SVGs directly into `artifacts/docs/site/` so they slot into the deployed site without a separate copy step.
