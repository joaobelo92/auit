"""Classes for messages sent between the server and the client.

The message types for which both request and response defined here are:
    - Hello
    - Optimization
    - Evaluation
    - Error

The JSON structures for the messages are defined in the from_json and to_json
functions of the individual message classes.
"""

from __future__ import annotations
from dataclasses import dataclass, field
import json
from typing import List
from .layout import Layout


@dataclass
class Message:
    """A message is a JSON object sent between the server and the client."""

    def from_json(message_data: str) -> Message:
        """Return a message from a JSON string.

        Args:
            json_str: The JSON string representing the message data.

        The from_json function for the messages defined here is not implemented
        because the message type is not known from the JSON string.
        It is implemented in the from_json functions of the individual message
        classes.
        """
        raise NotImplementedError()
    
    def to_json(self) -> str:
        """Return a JSON string representing the message data.

        The to_json function for the messages is implemented in the
        to_json functions of the individual message classes.
        """
        raise NotImplementedError()


@dataclass
class Request(Message):
    """A request is a message sent from the client to the server."""

    pass


@dataclass
class Response(Message):
    """A response is a message sent from the server to the client."""

    pass


@dataclass
class HelloRequest(Request):
    """A hello request is a request sent from the client to the server
    to establish a connection."""

    pass

    def from_json(message_data: str) -> HelloRequest:
        """Return a hello request from a JSON string.

        Args:
            json_str: The JSON string representing the request data (ignored).

        The JSON strings for the messages defined here are:
            - HelloRequest ("H"): {}

        """
        return HelloRequest()

    def to_json(self) -> str:
        """Return a JSON string representing the request data.

        The JSON strings for the messages defined here are:
            - HelloRequest ("H"): {}

        """
        return json.dumps({})


@dataclass
class HelloResponse(Response):
    """A hello response is a response sent from the server to the client
    to establish a connection."""

    pass

    def from_json(message_data: str) -> HelloResponse:
        """Return a hello response from a JSON string.

        Args:
            json_str: The JSON string representing the response data (ignored).

        The JSON strings for the messages defined here are:
            - HelloResponse ("h"): {}

        """
        return HelloResponse()

    def to_json(self) -> str:
        """Return a JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - HelloResponse ("h"): {}

        """
        return json.dumps({})


@dataclass
class OptimizationRequest(Request):
    """An optimization request is a request sent from AUIT to the solver
    to receive the Pareto optimal solutions to a layout optimization problem."""
    manager_id: str
    n_objectives: int  # Number of objectives
    initial_layout: Layout  # Initial layout
    n_constraints: int = 0  # Number of inequality constraints (<= 0)

    def from_json(message_data: str) -> OptimizationRequest:
        """Return an optimization request from a JSON string.

        Args:
            json_str: The JSON string representing the request data.

        The JSON strings for the messages defined here are:
            - OptimizationRequest ("O"): {
                "nObjectives": <int>,
                "nConstraints": <int>,
                "initialLayout": {
                    "items": [
                        {
                            "id": <str>,
                            "position": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                            }
                            "rotation": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                                "w": <float>,
                            }
                        },
                        ...
                    ],
                },
            }

        """
        data = json.loads(message_data)
        initial_layout = Layout.from_dict(data["initialLayout"]) \
            if type(data["initialLayout"]) is dict \
            else Layout.from_json(data["initialLayout"])
        return OptimizationRequest(
            manager_id=data["managerId"],
            n_objectives=data["nObjectives"],
            n_constraints=data["nConstraints"] if "nConstraints" in data else 0,
            initial_layout=initial_layout,
        )
    
    def to_json(self) -> str:
        """Return a JSON string representing the request data.

        The JSON strings for the messages defined here are:
            - OptimizationRequest ("O"): {
                "nObjectives": <int>,
                "nConstraints": <int>,
                "initialLayout": {
                    "items": [
                        {
                            "id": <str>,
                            "position": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                            }
                            "rotation": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                                "w": <float>,
                            }
                        },
                        ...
                    ],
                },
            }

        """
        
        return json.dumps({
            "nObjectives": self.n_objectives,
            "nConstraints": self.n_constraints,
            "initialLayout": self.initial_layout.__dict__(),
        })


@dataclass
class OptimizationResponse(Response):
    """An optimization response is a response sent from the solver to AUIT
    that contains the Pareto optimal solutions to the layout optimization problem."""
    manager_id: str
    solutions: List[Layout]
    suggested: Layout

    def from_json(message_data: str) -> OptimizationResponse:
        """Return an optimization response from a JSON string.

        Args:
            json_str: The JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - OptimizationResponse ("o"): {
                "solutions": {"items": [
                    {
                        "items": [
                            {
                                "id": <str>,
                                "position": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                },
                                "rotation": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                    "w": <float>,
                                }
                            },
                            ...
                        ],
                    },
                    ...
                ]},
                "suggested": {
                    "items": [
                        {
                            "id": <str>,
                            "position": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                            },
                            "rotation": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                                "w": <float>,
                            }
                        },
                        ...
                    ],
                },
            }

        """
        data = json.loads(message_data)
        suggested_layout = None
        if "suggested" in data:
            suggested_layout = Layout.from_dict(data["suggested"]) \
                if type(data["suggested"]) is dict \
                else Layout.from_json(data["suggested"])
        else:
            suggested_layout = Layout.from_dict(data["solutions"][0]) \
                if type(data["solutions"][0]) is dict \
                else Layout.from_json(data["solutions"][0])
        return OptimizationResponse(
            solutions=[
                Layout.from_dict(solution) if isinstance(solution, dict)
                else Layout.from_json(solution)
                for solution in data["solutions"]
            ],
            suggested=suggested_layout
        )

    def to_json(self) -> str:
        """Return a JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - OptimizationResponse ("o"): {
                "solutions": {"items": [
                    {
                        "items": [
                            {
                                "id": <str>,
                                "position": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                }
                                "rotation": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                    "w": <float>,
                                }
                            },
                            ...
                        ],
                    },
                    ...
                ]},
                "suggested": {
                    "items": [
                        {
                            "id": <str>,
                            "position": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                            },
                            "rotation": {
                                "x": <float>,
                                "y": <float>,
                                "z": <float>,
                                "w": <float>,
                            }
                        },
                        ...
                    ],
                },
            }

        """
        return json.dumps({
            "manager_id": self.manager_id,
            "solutions": [solution.__dict__() for solution in self.solutions],
            "suggested": self.suggested.__dict__(),
            
        })


@dataclass
class EvaluationRequest(Request):
    """An evaluation request is a request sent from the solver to AUIT
    to get the vectors of costs for a list of layouts including the
    costs associated with constraint violations."""

    manager_id: str
    layouts: List[Layout]

    def from_json(message_data: str) -> EvaluationRequest:
        """Return an evaluation request from a JSON string.

        Args:
            json_str: The JSON string representing the request data.

        The JSON strings for the messages defined here are:
            - EvaluationRequest ("E"): {
                "layouts": [
                    {
                        "elements": [
                            {
                                "id": <str>,
                                "position": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                }
                                "rotation": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                    "w": <float>,
                                }
                            },
                            ...
                        ],
                    },
                    ...
                ],
            }

        """
        data = json.loads(message_data)
        return EvaluationRequest(
            layouts=[
                Layout.from_dict(layout) if isinstance(layout, dict)
                else Layout.from_json(layout)
                for layout in data["layouts"]
            ],
        )
    
    def to_json(self) -> str:
        """Return a JSON string representing the request data.

        WARNING: This method is incompatible with the from_json method.
        Example: The following code will not work:
            >>> assert request == EvaluationRequest.from_json(request.to_json())

        The JSON strings for the messages defined here are:
            - EvaluationRequest ("E"): {
                "layouts": [
                    {
                        "elements": [
                            {
                                "id": <str>,
                                "position": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                }
                                "rotation": {
                                    "x": <float>,
                                    "y": <float>,
                                    "z": <float>,
                                    "w": <float>,
                                }
                            },
                            ...
                        ],
                    },
                    ...
                ],
            }

        """
        return json.dumps({
            "manager_id": self.manager_id,
            "layouts": [layout.__dict__() for layout in self.layouts]
        })


@dataclass
class EvaluationResponse(Response):
    """An evaluation response is a response sent from AUIT to the solver
    containing the cost vectors for the list of layouts contained in the request,
    including the costs associated with constraint violations."""

    costs: List[List[float]]
    violations: List[List[float]] = field(default_factory=list)

    def from_json(message_data: str) -> EvaluationResponse:
        """Return an evaluation response from a JSON string.

        Args:
            json_str: The JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - EvaluationResponse ("e"): {
                "costs": [
                    [<float>, <float>, ...],
                    ...
                ],
                "violations": [
                    [<float>, <float>, ...],
                    ...
                ],
            }

        """
        data = json.loads(message_data)
        return EvaluationResponse(
            costs=data["costs"],
            violations=data["violations"] if "violations" in data else [],
        )

    def to_json(self) -> str:
        """Return a JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - EvaluationResponse ("e"): {
                "costs": [
                    [<float>, <float>, ...],
                    ...
                ],
                "violations": [
                    [<float>, <float>, ...],
                    ...
                ],
            }

        """
        return json.dumps({
            "costs": self.costs,
            "violations": self.violations,
        })


@dataclass
class ErrorResponse(Response):
    """An error response is a response sent from the server to the client
    to report an error."""

    error: str

    def from_json(message_data: str) -> ErrorResponse:
        """Return an error response from a JSON string.

        Args:
            json_str: The JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - ErrorResponse ("e"): {
                "error": <str>,
            }

        """
        data = json.loads(message_data)
        return ErrorResponse(error=data["error"])

    def to_json(self) -> str:
        """Return a JSON string representing the response data.

        The JSON strings for the messages defined here are:
            - ErrorResponse ("e"): {
                "error": <str>,
            }

        """
        return json.dumps({
            "error": self.error,
        })


def from_json(message_type: str, message_data: str) -> Message:
    """Return a message from a JSON string based on the provided type.

    Args:
        request_type: The type of the message.
        json_str: The JSON string representing the request data.
    """
    data = json.loads(message_data)
    if message_type == "P":
        print(data)
        return data
    if message_type == "H":
        return HelloRequest()
    elif message_type == "h":
        return HelloResponse()
    elif message_type == "O":
        return OptimizationRequest.from_json(message_data)
    elif message_type == "o":
        return OptimizationResponse.from_json(message_data)
    elif message_type == "E":
        return EvaluationRequest.from_json(message_data)
    elif message_type == "e":
        return EvaluationResponse.from_json(message_data)
    elif message_type == "x":
        return ErrorResponse.from_json(message_data)
    else:
        raise ValueError("Unknown message type: %s" % message_type)