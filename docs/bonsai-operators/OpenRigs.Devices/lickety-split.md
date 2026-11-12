# LicketySplit

The LicketySplit (`who_am_i = 1400`) is a Harp lick detector. It samples one or more capacitive/ADC channels and emits trigger events when the signal crosses configurable thresholds, providing low-latency lick timestamps on the Harp clock.

---

## Python schema

`LicketySplit` is defined in `open_rigs.devices` and extends `HarpDevice`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `port_name` | `str` | - | Serial port the device is connected to (e.g. `COM3`) |
| `synchronize_clock` | `bool` | `False` | Set the device's clock to the host's UTC time on connecting, before the register dump. Turn off when a clock synchroniser drives the clock input. |
| `channel0_trigger_threshold` | `UShort` | `0` | ADC threshold above which Channel 0 triggers a lick |
| `channel0_untrigger_threshold` | `UShort` | `0` | ADC threshold below which Channel 0 untriggers a lick |

### Configuration example

In your `rig.py`:

```python
from open_rigs.devices import LicketySplit

class Rig(...):
    ...
    lick_detector: LicketySplit = LicketySplit(
        port_name="COM7",
        channel0_trigger_threshold=1200,
        channel0_untrigger_threshold=900,
    )
```

Or as YAML:

```yaml
lick_detector:
  port_name: COM7
  channel0_trigger_threshold: 1200
  channel0_untrigger_threshold: 900
```

---

## Bonsai workflow

:::workflow
![LicketySplit](~/assets/workflows/devices/LicketySplit.svg){data-bonsai="~/src/OpenRigs.Devices/LicketySplit.bonsai"}
:::

The workflow opens the Harp serial connection and publishes the device event stream on a named `PublishSubject`. On connecting it sends the startup sequence: optionally a `SynchronizeTimestamp` write (when `SynchronizeClock` is true), then an `OperationControl` write to request the register dump. Events are published from the `OperationControl` write reply onwards. Once the first dump event arrives, the workflow writes the configured trigger and untrigger thresholds to the device. Turn `SynchronizeClock` off when a clock synchroniser drives the clock input.
