"""API for loading raw data from open-rigs datasets into pandas data frames."""

from dotmap import DotMap
from swc.aeon.io.api import to_datetime, to_seconds
from swc.aeon.schema.streams import Device

from open_rigs.api.io import load
from open_rigs.api.reader import Csv, HarpRegister, Video
from open_rigs.api.streams import csv_reader, harp_reader, video_reader

__all__ = [
    "Csv",
    "Device",
    "DotMap",
    "HarpRegister",
    "Video",
    "csv_reader",
    "harp_reader",
    "load",
    "to_datetime",
    "to_seconds",
    "video_reader",
]
