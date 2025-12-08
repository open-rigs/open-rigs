from pydantic import Field
from open_rigs.rigs.base import BaseSchema

class Experiment(BaseSchema):
    """The base class for creating open-rigs experiment models."""

    workflow: str = Field(description="Path to the workflow running the experiment.")
    commit: str = Field(description="Commit hash of the experiment/rig repo.")
    repository_url: str = Field(
        description="The URL of the git repository used to version experiment source code."
    )
