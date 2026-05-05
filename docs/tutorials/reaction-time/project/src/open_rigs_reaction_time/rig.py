from typing import Literal, Dict
from pydantic import Field

from open_rigs.core.rig import Rig
from open_rigs.devices.harp import HarpHobgoblin
from open_rigs.vision import Screen

from open_rigs_reaction_time import __semver__


class OpenRigsReactionTimeRig(Rig):
    version: Literal[__semver__] = __semver__
    harp_hobgoblin: HarpHobgoblin = Field(description="Harp Hobgoblin device")
    screen: Screen = Field(description="The main display for visual stimuli")