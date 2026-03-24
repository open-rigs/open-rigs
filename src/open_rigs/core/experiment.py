from pydantic import Field
from swc.aeon.schema import Experiment


class ExperimentSession(Experiment):
    """The base class for creating open-rigs experiment models."""

    subject_id: str = Field(description="The subject id for this session.")
    session_id: str = Field(description="The session identifier.")
