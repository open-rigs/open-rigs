from enum import StrEnum
import json
from pathlib import Path
from typing import Annotated, Generic, TypeVar, Any
from pydantic import Field
from open_rigs.rigs.base import BaseSchema

SByte = Annotated[int, Field(ge=-128, le=127)]
Byte = Annotated[int, Field(ge=0, le=255)]
Short = Annotated[int, Field(ge=-32768, le=32767)]
UShort = Annotated[int, Field(ge=0, le=65535)]
Int = Annotated[int, Field(ge=-2147483648, le=2147483647)]
UInt = Annotated[int, Field(ge=0, le=4294967295)]
Long = Annotated[int, Field(ge=-9223372036854775808, le=9223372036854775807)]
ULong = Annotated[int, Field(ge=0, le=18446744073709551615)]

Float = float
Double = float
String = str
Bool = bool

class TimestampSource(StrEnum):
    NULL = "null"
    HARP = "harp"
    RENDER = "render"
    ARDUINO = "arduino"

TData = TypeVar("TData", bound=Any)

class Vector3(BaseSchema):
    x: float = Field(description="X coordinate of the point.")
    y: float = Field(description="Y coordinate of the point.")
    z: float = Field(description="Z coordinate of the point.")

class SoftwareEvent(BaseSchema, Generic[TData]):
    """
    A software event is a generic event that can be used to track any event that occurs in the software.
    """
    name: str = Field(..., description="The name of the event.")
    timestamp: float | None = Field(default=None, description="The timestamp of the event.")
    timestamp_source: TimestampSource = Field(default=TimestampSource.NULL,description="The source of the timestamp. Typically either a harp device or on the visual render loop")
    frame_index: int | None = Field(default=None,ge=0,description="The frame index of the event.")
    frame_timestamp: float | None = Field(default=None,description="The timestamp of the frame.")
    data: TData | None = Field(default=None,description="The data payload of the event.")

class DataTypes(BaseSchema):
    vector3 : Vector3 
    software_event : SoftwareEvent

def main() -> None:
    schema = DataTypes.model_json_schema(union_format="primitive_type_array")
    schema.pop("properties", None)
    Path("DataTypes.json").write_text(json.dumps(schema, indent=2))