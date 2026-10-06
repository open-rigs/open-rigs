# Log Data Schema

`LogDataSchema` writes one piece of serialized text to its own file in the session folder. Its job is the snapshot: the rig configuration and the task configuration a session actually ran with, and any calibration artefact the rig loaded, saved beside the data so a session folder says exactly which hardware, which protocol and which calibration produced it. It subscribes to the `PathPrefix` subject published by [LogController](log-controller.md), like every other logger.

---

## Bonsai workflow

:::workflow
![LogDataSchema](~/assets/workflows/logging/LogDataSchema.svg){data-bonsai="~/src/OpenRigs.Logging/LogDataSchema.bonsai"}
:::

The input is text, normally the output of the generated `SerializeToJson` for the configuration type with `Formatting` set to `Indented`. The include waits for both the text and `PathPrefix`, then formats the file name and writes the text once. The wait matters: a file sink reads its path when it subscribes, so the path has to be resolved before the writer is built. Inside the include that order is fixed, whatever the order of the nodes around it.

### Externalized properties

| Property | Default | Description |
|----------|---------|-------------|
| `Name` | `Schema` | The name of the snapshot. It names the subfolder and prefixes the file. |

### Files

```
<PathPrefix>\<Name>\<Name>_<DateTime>.json
```

For example `RigSchema\RigSchema_2026-10-06T00-03-17.json` inside the session folder. The layout follows the data logging specification: one folder per source, the file named after it with the time it was written.

---

## Usage

The template's starting workflow snapshots both configurations in its `Logging` group:

1. `SubscribeSubject RigConfig`, then the generated `SerializeToJson` (Indented), then `LogDataSchema` with `Name` set to `RigSchema`.
2. `SubscribeSubject TaskConfig`, then `SerializeToJson`, then `LogDataSchema` with `Name` set to `TaskSchema`.

A rig that loads a calibration artefact snapshots it the same way, with the artefact's JSON text as the input and a name such as `ValveCalibrationDO1`. See [Calibration](../OpenRigs.Core/calibration.md).

Keep every `LogDataSchema`, with the other loggers, in the workflow's logging group. The configuration groups publish what is to be logged on a subject; the logging group subscribes to it. Two groups that each need the other's subject cannot be built.
