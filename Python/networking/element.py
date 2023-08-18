"""Element class."""

from __future__ import annotations
from dataclasses import dataclass, field
import json

@dataclass
class Position:
    """A position is a 3D vector."""

    x: float = 0.
    y: float = 0.
    z: float = 0.

    def __dict__(self) -> dict:
        """Return a dictionary representation of the position."""
        return {
            "x": self.x,
            "y": self.y,
            "z": self.z,
        }

    def from_dict(data) -> Position:
        """Return a position from a dictionary."""
        return Position(
            x=data["x"],
            y=data["y"],
            z=data["z"],
        )
    
    def to_json(self) -> str:
        """Return a JSON representation of the position."""
        return json.dumps(self.__dict__())

    def from_json(data) -> Position:
        """Return a position from a JSON string."""
        return Position.from_dict(data)

@dataclass
class Rotation:
    """A rotation is a quaternion."""

    x: float = 0.
    y: float = 0.
    z: float = 0.
    w: float = 1.

    def __dict__(self) -> dict:
        """Return a dictionary representation of the rotation."""
        return {
            "x": self.x,
            "y": self.y,
            "z": self.z,
            "w": self.w,
        }

    def from_dict(data) -> Rotation:
        """Return a rotation from a dictionary."""
        return Rotation(
            x=data["x"],
            y=data["y"],
            z=data["z"],
            w=data["w"],
        )
    
    def to_json(self) -> str:
        """Return a JSON representation of the rotation."""
        return json.dumps(self.__dict__())

    def from_json(data) -> Rotation:
        """Return a rotation from a JSON string."""
        return Rotation.from_dict(data)

@dataclass
class Element:
    """An element is a UI element including its position
    and rotation."""

    id: str
    position: Position = field(default_factory=Position)
    rotation: Rotation = field(default_factory=Rotation)

    def __dict__(self) -> dict:
        """Return a dictionary representation of the UI element."""
        return {
            "id": self.id,
            "position": self.position.__dict__(),
            "rotation": self.rotation.__dict__(),
        }

    def from_dict(data) -> Element:
        """Return a UI element from a dictionary."""
        print(data["id"])
        return Element(
            id=data["id"],
            position=Position.from_json(data["position"]),
            rotation=Rotation.from_json(data["rotation"]),
        )
    
    def to_json(self) -> str:
        """Return a JSON representation of the UI element."""
        return json.dumps(self.__dict__())

    def from_json(data) -> Element:
        """Return a UI element from a JSON string."""
        return Element.from_dict(json.loads(data))