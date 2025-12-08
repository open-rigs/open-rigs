from typing import Dict
from pydantic import Field

from open_rigs.rigs.base import BaseSchema
from open_rigs.rigs.device import BehaviorBoard, ArduinoDevice, SerialDeviceModule
from open_rigs.rigs.video import Camera


class TestRig(BaseSchema):
    behavior_boards: Dict[str, BehaviorBoard] = Field(description="Mapping from logical names to BehaviorBoard devices (e.g. 'main', 'aux').")
    cameras: Dict[str, Camera] = Field(description="Mapping from camera role names to camera configurations (e.g. 'body', 'face', 'top').")
    arduinos: Dict[str, ArduinoDevice] | None = Field(default=None,description="Optional mapping from logical names to Arduino devices used in the dome (e.g. for LED drivers or other IO).")
    serial_devices: Dict[str, SerialDeviceModule] | None = Field(default=None,description="Optional mapping from logical names to auxiliary serial devices used in the dome.")