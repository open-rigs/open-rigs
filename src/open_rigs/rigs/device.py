from typing import ClassVar, Literal
from pydantic import Field
from open_rigs.rigs.base import Device
from swc.aeon.rigs.harp import HarpDevice
from swc.aeon.rigs.harp import BehaviorBoard as HarpBehaviorBoard
import open_rigs.rigs.controllers as Controllers

class SerialDevice(Device):
    """A base class for creating serial device models."""
    port_name: str = Field(examples=["COMx"], description="The name of the device serial port.")

class LicketySplit(HarpDevice):
    """Represents a Harp LicketySplit device."""
    device_type: Literal["LicketySplit"] = "LicketySplit"
    who_am_i: ClassVar[int] = 1400

class BehaviorBoard(HarpBehaviorBoard):
    """Represents a Harp Behavior Board device."""
    device_type: Literal["BehaviorBoard"] = "BehaviorBoard"
    who_am_i: ClassVar[int] = 1216

    pulse_controller: Controllers.PulseController | None = Field(default=None, description="Optional PulseController module for generating digital output pulses.")
    camera_controller: Controllers.CameraController | None = Field(default=None, description="Optional CameraController module for emitting camera trigger pulses.")
    running_wheel: Controllers.RunningWheelModule | None = Field(default=None, description="Optional RunningWheelModule module to define wheel geometry.")

class ArduinoDevice(SerialDevice):
    """Represents an Arduino serial device used in Bonsai workflows."""
    device_type: Literal["Arduino"] = "Arduino"
    baud_rate: int = Field(alias="BaudRate", description="Baud rate for the Arduino serial connection.")
    sampling_interval: int = Field(alias="SamplingInterval", description="Sampling interval, in milliseconds, between analog and I2C measurements.")

    led_driver: Controllers.LedDriver | None = Field(default=None, description="Optional LedDriver module for generating digital output pulses.")