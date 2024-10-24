"""Layout class."""

from __future__ import annotations
from dataclasses import dataclass
import json
from typing import List
from .element import Element


@dataclass
class Layout:
    """A layout is a set of UI elements."""

    elements: List[Element]

    def __dict__(self) -> dict:
        """Return a dictionary representation of the layout."""
        return {"elements": [element.__dict__() for element in self.elements]}

    def from_dict(data: dict) -> Layout:
        """Return a layout from a dictionary."""
        return Layout(
            elements=[
                Element.from_dict(element) if isinstance(element, dict) else Element.from_json(element)
                for element in data["elements"]
            ]
        )

    def to_json(self) -> str:
        """Return a JSON representation of the layout."""
        return json.dumps(self.__dict__())

    def from_json(data: str) -> Layout:
        """Return a layout from a JSON string."""
        return Layout.from_dict(json.loads(data))

    @property
    def n_items(self) -> int:
        """Return the number of items in the layout."""
        return len(self.elements)
