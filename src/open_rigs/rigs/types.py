from typing import Annotated
from pydantic import Field

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
