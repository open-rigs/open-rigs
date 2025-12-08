from typing import Union, Literal
from pydantic import Field, ConfigDict, RootModel
from open_rigs.rigs.base import BaseSchema


class CameraBase(BaseSchema):
    """Base class for discriminated camera configurations."""
    model_config = ConfigDict(discriminator="camera_type")

    camera_type: str = Field(description="Discriminator for the camera model type.")


class ArducamOV9180(CameraBase):
    camera_type: Literal["Arducam"] = Field(default="Arducam", description="Camera type discriminator for Arducam devices.")
    trigger_frequency: float = Field(default=50, ge=1, description="The frequency at which the camera is triggered (in Hz).")
    device_index: int = Field(default=0, ge=0, description="The index of the device.")


class SpinnakerCamera(CameraBase):
    camera_type: Literal["Spinnaker"] = Field(default="Spinnaker", description="Camera type discriminator for Spinnaker devices.")
    trigger_frequency: float = Field(default=50, ge=1, description="The frequency at which the camera is triggered (in Hz).")
    exposure_time: float = Field(default=15000, ge=0, description="The exposure time for the camera (in microseconds).")
    serial_number: str | None = Field(default="00000", description="The serial number of the camera.")
    gain: float = Field(default=1, ge=0, description="The camera gain.")
    binning: int = Field(default=1, ge=1, description="The binning setting for the camera.")

CameraModule = Union[ArducamOV9180, SpinnakerCamera]

class Camera(RootModel[CameraModule]):
    """Discriminated camera configuration (Arducam or Spinnaker)."""
    model_config = ConfigDict(title="Camera")


class CameraAcquisition(BaseSchema):
    """Represents the CameraAcquisition workflow module, with or without a trigger subject input."""
    camera: CameraModule = Field(description="Configuration for the camera used by the CameraAcquisition workflow.")