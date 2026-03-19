import pydantic
import open_rigs.core.data_types as _core_dt
import open_rigs.devices.data_types as _dev_dt


def _collect_fields(*modules):
    fields = {}
    for mod in modules:
        for name in getattr(mod, "__all__", []):
            cls = getattr(mod, name)
            if isinstance(cls, type):
                fields[name] = (cls, ...)
    return fields


DataTypes = pydantic.create_model("DataTypes", **_collect_fields(_core_dt, _dev_dt))
