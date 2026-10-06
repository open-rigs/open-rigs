# Calibration

A rig turns a quantity the task asks for into something the hardware understands: a reward volume into a valve opening time, a display level into a projector command. The relation is measured once per machine and stored as an artefact that the rig reads at startup. `OpenRigs.Core` provides the artefact types, the operators that fit and look up a curve, and a dispense calibration procedure that measures one. The [Calibration](../../articles/calibration.md) article explains how they fit together on a rig; this page is the operator reference.

Every operator here deals in `(x, y)` pairs where `x` is the quantity the rig controls (a pulse width) and `y` is the quantity measured (a volume).

---

## Artefact types

Generated once from the pydantic models in `open_rigs.core.calibration` into the `OpenRigs.Core.Calibration` namespace. Every rig schema binds to these types instead of generating its own copy, so an artefact written by one repository reads in any other.

| Type | Fields | Description |
|------|--------|-------------|
| `CalibrationCurve` | `xName`, `xUnit`, `yName`, `yUnit`, `points`, `payloadPath`, plus provenance | Measured points with their axis names and units. The common artefact. No fit is stored; a rig fits the points at startup. |
| `CalibrationPoint` | `x`, `y` | One measured point. |
| provenance (on every artefact) | `calibratedAt` (UTC), `calibratedBy`, `machineName`, `notes` | Who measured it, when and where. A rig warns when `machineName` is not its own or the artefact is older than its threshold. |

The generated `DeserializeFromJson` and `SerializeToJson` for these types live in `OpenRigs.Core` and are the way a rig reads an artefact file and a procedure writes one.

---

## SelectCalibrationPoints

Turns a collection of calibration points, such as the `Points` of a deserialized `CalibrationCurve`, into `(x, y)` pairs for `FitCalibrationCurve` by reading the members named by `XSelector` and `YSelector` from each point. Pairs already in `Tuple` form pass through unchanged.

| Property | Default | Description |
|----------|---------|-------------|
| `XSelector` | `X` | The member of each point holding the quantity the rig controls. Nested members use dots. |
| `YSelector` | `Y` | The member of each point holding the measured quantity. Nested members use dots. |

---

## FitCalibrationCurve

Derives a calibration curve from measured `(x, y)` points. Input is a sequence of pairs, for example from `SelectCalibrationPoints`, or a `Tuple` of the x values and the y values. The output, a `CalibrationCurveFit`, keeps the calibrated range (`MinX`, `MaxX`, `MinY`, `MaxY`), the point count, whether the curve is monotone, and for a linear fit its `Slope` and `Intercept`.

| Property | Default | Description |
|----------|---------|-------------|
| `Model` | `LinearFit` | How the curve is derived from the points. `LinearFit`, a least-squares line, suits repeated noisy measurements such as valve weighings. |
| `Order` | `2` | The polynomial order. Used by `PolynomialFit` only; there must be more points than the order. |

### CalibrationModel

| Value | Curve |
|-------|-------|
| `LinearFit` | A straight line fitted by least squares through all points. Repeated measurements at the same x are averaged by the fit. The default for valve calibrations. |
| `PolynomialFit` | A polynomial of the given order fitted by least squares through all points. |
| `Linear` | Piecewise linear interpolation passing through every point. |
| `CubicSpline` | A natural cubic spline passing through every point. |
| `Step` | A step function holding each point's value until the next point. Forward lookup only. |

---

## LookupCalibration

Looks each value up on a calibration curve. Input is the value alone, with the curve mapped onto the `Curve` property from `FitCalibrationCurve`, or a `Tuple` of the value and the curve, for example from `WithLatestFrom`.

| Property | Default | Description |
|----------|---------|-------------|
| `Direction` | `Forward` | `Forward`: x to y, the controlled quantity to the measured one (pulse width to volume). `Inverse`: y to x (requested volume to pulse width). Inverse lookup needs a monotone curve. |
| `OutOfRange` | `Clamp` | `Clamp` moves an out-of-range value to the nearest end of the calibrated range and flags the result. `Throw` raises an error naming the value and the range. |
| `Curve` | | The curve to look values up on. Ignored when the input pairs the value with the curve. |

The output is a `CalibrationLookup`:

| Member | Description |
|--------|-------------|
| `Output` | The looked-up value: the width to write to the register for an inverse lookup. |
| `EffectiveInput` | The input the output corresponds to. It differs from the request only when the value was clamped, and is what to log as delivered. |
| `Clamped` | Whether the input was moved to the end of the calibrated range. |

---

## The rig side

Reading an artefact named in `rig.yml` and turning a requested volume into a width takes platform operators only: `ResolveRigFile` on the artefact path from the configuration (see [Rig Configuration](rig-config.md)), `ReadAllText`, the Core `DeserializeFromJson` into `CalibrationCurve`, `SelectCalibrationPoints` on its `Points`, `FitCalibrationCurve` with the model named in the configuration, then `LookupCalibration` with `Direction` set to `Inverse`. A request of 2 µl on a curve of 0.05 µl per ms plus 0.1 µl comes back as 38 ms. The artefact in use is snapshotted into the session folder with [LogDataSchema](../OpenRigs.Logging/log-data-schema.md).

---

## Dispense calibration

The procedure for anything that delivers a dose per pulse, valves and pumps. It is a workflow of small operators, so a lab can open it and adapt it.

:::workflow
![DispenseCalibration](~/assets/workflows/core/DispenseCalibration.svg){data-bonsai="~/src/OpenRigs.Core/DispenseCalibration.bonsai"}
:::

The input is the committed `DispenseCalibration` settings: a `procedure` (the samples to fire, the priming burst, the density, the settle window, the fit used for the acceptance check) and a `balance` (the balance's serial protocol). The include primes the line and weighs a baseline, then for each sample fires a burst and weighs the gain. The gains become `(size, volume per pulse)` points, which are checked, built into a `CalibrationCurve` with provenance and written into the machine's configuration directory. The output is the `CalibrationCurve` that was written.

The hardware sits behind two subjects the including workflow declares:

| Property | Description |
|----------|-------------|
| `RequestSubjectName` | Receives `(channel, size)` pulse requests. It must be a `BehaviorSubject`, such as the `PulseController` include's `TriggerCalibratedPulse`, because each burst completes. |
| `ReadingsSubjectName` | Carries `WeightMeasurement` readings, such as the `BalanceReading` subject of the serial balance include below. |

The procedure refuses a curve that is not monotone, in either direction, because the rig inverts it to look a volume up.

### The operators inside

| Operator | Role | Properties |
|----------|------|------------|
| `PulseBurst` | Fires one sample's burst: `repeats` requests, one every `size` plus `interPulseIntervalMs`, and completes once the last pulse has landed. Fed the procedure instead, it fires the priming burst. | `Channel`: the behavior board DO line, 1 to 3. |
| `WeighStable` | Weighs once its first input completes: waits for `SettleWindow` consecutive stable readings and emits the last one's weight in grams, or fails after `TimeoutSeconds`. An empty burst still weighs, which gives the baseline. | `SettleWindow` (10), `TimeoutSeconds` (60), both mapped from the procedure. |
| `BuildCalibrationCurve` | Assembles the artefact from the measured points with provenance, and refuses points that are not monotone or that measured nothing. | `CalibratedBy`, `Notes`, `XName`, `XUnit`, `YName`, `YUnit`, `Model` and `Order` for the check, `RequireMonotone`, `RequirePositive`. |
| `WriteCalibrationArtefact` | Writes the serialized artefact as `calibration\<FileName>.json` in the configuration directory, replaced on every run, plus a dated copy under `calibration\history\`. Input is the JSON text paired with the directory. Output names the file written and the `rig.yml` line to paste. | `FileName` |

:::workflow
![WriteCalibrationArtefact](~/assets/workflows/core/WriteCalibrationArtefact.svg){data-bonsai="~/src/OpenRigs.Core/WriteCalibrationArtefact.bonsai"}
:::

---

## Serial balance

`OpenRigs.Devices:SerialBalance.bonsai` is the balance side of the procedure. Its input is the `balance` block of the settings. It opens the port, tares the balance after its boot delay, requests a reading every print period, parses each line into a `WeightMeasurement` (weight in grams, confidence 1 when stable) and publishes it on `ReadingsSubjectName`. It is built on the `SerialDevice` include, so the balance's raw messages and commands stay available on their own subjects.

:::workflow
![SerialBalance](~/assets/workflows/devices/SerialBalance.svg){data-bonsai="~/src/OpenRigs.Devices/SerialBalance.bonsai"}
:::

| Property | Default | Description |
|----------|---------|-------------|
| `PortName` | `COMx` | The balance's serial port, chosen at run time. The balance is plugged in only to calibrate. |
| `ReadingsSubjectName` | `BalanceReading` | The subject the balance publishes its `WeightMeasurement` readings on. |

Two Core operators do the work inside: `ParseBalanceReading` (the number up to `UnitSuffix`, unstable while `UnstableMarker` is present, lines in `Ignore` dropped) and `PollBalance` (the tare command after the boot delay, then the print command every print period). Their defaults follow a common serial balance protocol: 9600 baud, readings such as `12.345 g`, `?` while unstable, `T` to tare.

---

## In a rig repository

A rig commits one settings file, `config/valve-calibration.yml`, and one thin workflow, for example `src/CalibrateValve.bonsai`, whose root properties are the channel, the artefact file name, who is calibrating, and the two COM ports. The workflow declares the two subjects from its own devices and includes `DispenseCalibration`. Nothing is read from `rig.yml`, which lets the same workflow serve a rig with several boards. The [Calibration](../../articles/calibration.md) article has the run-through and the `rig.yml` side.
