from pathlib import Path
from typing import Union
import json
import pydantic

from open_rigs.core.schema import DataTypes

SCHEMA_ROOT = Path("./src/open_rigs/schemas")


def main():
    models = [
        DataTypes
    ]
    model = pydantic.RootModel[Union[tuple(models)]]

    schema = model.model_json_schema(union_format="primitive_type_array")
    schema.pop("properties", None)
    Path(f"{SCHEMA_ROOT}/data_types.json").write_text(json.dumps(schema, indent=2))


if __name__ == "__main__":
    main()
