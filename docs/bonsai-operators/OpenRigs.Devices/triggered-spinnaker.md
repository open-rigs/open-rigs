# Triggered Spinnaker

The Triggered Spinnaker is a Bonsai wrapper around a FLIR/Spinnaker SDK camera operating in hardware-trigger mode. Each external trigger pulse (typically from a Behavior Board camera trigger output) produces exactly one frame, so the resulting video is aligned to the rig's hardware clock rather than the PC system clock.

---

## .NET type

Unlike the other devices in this section, `TriggeredSpinnaker` is implemented as a C# Bonsai operator and is fully covered by the API reference. See [OpenRigs.Video.TriggeredSpinnaker](xref:OpenRigs.Video.TriggeredSpinnaker) for the property list, frame output type, and method signatures.

The `.bonsai` file in `src/OpenRigs.Devices/` is a thin reusable group around the operator, exposing the camera index and stream subject names as externalized properties so the same module can drive multiple cameras on the rig.

---

## Bonsai workflow

:::workflow
![TriggeredSpinnaker](~/assets/workflows-static/devices/TriggeredSpinnaker.svg){data-bonsai="~/assets/workflows-static/devices/TriggeredSpinnaker.bonsai"}
:::

The workflow passes the camera trigger events into the `TriggeredSpinnaker` operator, publishes the frame stream on a named subject, and forwards each frame's `VideoDataFrame` to downstream consumers (display, encoders, writers). Combine this module with a Behavior Board `CameraTriggerController` to produce a deterministic, Harp-timestamped video stream.

The operator pairs each frame with the next trigger event in arrival order. It copies the chunk data (`FrameID`, `Timestamp`, `ExposureTime`, `Gain`) inside the camera callback and fills dropped frames by `FrameID`, which keeps the pairing aligned for the rest of the session.

