from typing import Optional, Any
from pydantic import Field, BaseModel
from swc.aeon.rigs.base import BaseSchema as AeonBaseSchema
from swc.aeon.rigs.base import Device as AeonDevice

class BaseSchema(AeonBaseSchema):
    """Base for all open-rigs schemas (rigs, experiments, devices, sessions.)."""

class Device(AeonDevice):
    """The base class for creating open-rigs hardware device models."""
