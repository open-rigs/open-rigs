import json
from pathlib import Path
from open_rigs.rigs.experiment import Experiment as ExperimentBase
from open_rigs.rigs.experiment import BaseSchema
from rig import TestRig

class Experiment(ExperimentBase):
    rig: TestRig

class TestExperiment(BaseSchema):
    experiment: Experiment | None = None

def main() -> None:
    schema = TestExperiment.model_json_schema(union_format="primitive_type_array")
    schema.pop("properties", None)
    Path("TestExperiment.json").write_text(json.dumps(schema, indent=2))

if __name__ == "__main__":
    main()