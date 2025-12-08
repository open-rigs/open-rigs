from typing import ClassVar, Literal
from pydantic import Field
from open_rigs.rigs.base import Device
from swc.aeon.rigs.harp import HarpDevice
from swc.aeon.rigs.harp import BehaviorBoard as HarpBehaviorBoard
import open_rigs.rigs.controllers as Controllers

class SerialDevice(Device):
    """A base class for creating serial device models."""
    port_name: str = Field(examples=["COMx"], description="The name of the device serial port.")

class SerialDeviceModule(SerialDevice):
    """Represents the SerialDevice workflow module.

    Mirrors all externalized properties of SerialDevice.bonsai, including
    port configuration, framing, buffer settings, and parsing pattern.
    """
    baud_rate: int = Field(default=9600, description="Baud rate for serial communication.")
    encoding: str | None = Field(default=None, description="Optional text encoding for interpreting incoming bytes.")
    new_line: str = Field(default="\r\n", description="Line termination sequence used to delimit incoming messages.")
    parity: str = Field(default="None", description="Parity checking mode for the serial port.")
    parity_replace: int = Field(default=63, description="Byte used to replace invalid bytes detected by a parity error.")
    data_bits: int = Field(default=8, description="Number of data bits per serial frame.")
    stop_bits: str = Field(default="One", description="Number of stop bits per serial frame.")
    handshake: str = Field(default="None", description="Hardware or software handshaking mode.")
    discard_null: bool = Field(default=False, description="Whether to discard null bytes appearing in the serial stream.")
    dtr_enable: bool = Field(default=False, description="Whether to enable Data Terminal Ready (DTR) control line.")
    rts_enable: bool = Field(default=False, description="Whether to enable Request To Send (RTS) control line.")
    read_buffer_size: int = Field(default=4096, description="Size, in bytes, of the read buffer.")
    write_buffer_size: int = Field(default=2048, description="Size, in bytes, of the write buffer.")
    received_bytes_threshold: int = Field(default=1, description="Minimum number of bytes in the buffer that triggers a read event.")
    serial_message_subject_name: str = Field(default="SerialMessages", description="Name of the subject to which parsed serial messages are published.")

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