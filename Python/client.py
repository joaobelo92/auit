"""Client for the AUIT server."""

import zmq
import sys
from networking.messages import (
    HelloRequest,
    OptimizationRequest,
    EvaluationRequest,
    from_json,
)
from networking.layout import Layout


def handle_response(response_type, response_data, verbose=False):
    """Handle a response."""
    # If response type is HelloResponse, print a message
    if response_type == "h":
        if verbose: print("Received a HelloResponse")
    # If response type is EvaluationResponse, print a message
    elif response_type == "e":
        if verbose:
            print("Received an EvaluationResponse")
            print("Costs:", response_data.costs)
            print("Violations:", response_data.violations)
    # If response type is ErrorResponse, print the error
    elif response_type == "x":
        if verbose: print("Received an ErrorResponse: %s" % response_data.error)
    # If response type is unknown, print a message
    else:
        if verbose: print("Received an unknown response type: %s" % response_type)


def send_request(socket, request_type, request_data, verbose=False):
    """Send a request and return the response."""
    # If verbose, print a message
    if verbose:
        print("Sending a %s request" % request_type)
        print("request_data:", request_data)


    # Send the request
    socket.send_string(
        request_type + request_data.to_json()
    )

    # Receive a response
    response = socket.recv_string()

    if verbose:
        print("Received a response:", response)

    # Parse the response
    response_type = response[0]
    response_data = from_json(response_type, response[1:] if len(response) > 1 else "")

    # Handle the response
    handle_response(response_type, response_data)

    # Return the response
    return response_type, response_data


def send_hello_request(socket):
    """Send a HelloRequest and return the response."""
    # Construct the request
    request_type = "H"
    request_data = HelloRequest()

    # Send the request and return the response
    return send_request(socket, request_type, request_data)


def send_costs_request(socket, layouts, verbose=False):
    """Send an EvaluationRequest and return the response."""
    # Print a message
    if verbose:
        print("Sending EvaluationRequest...")

    # Construct the request
    request_type = "E"
    request_data = EvaluationRequest(
        layouts=layouts,
    )

    # Send the request and return the response
    return send_request(socket, request_type, request_data)


def main():
    """Main function."""
    # Get port number from command line argument named "port" or "p"
    if len(sys.argv) == 1:  # if no arguments are given
        port = 5556
    else:
        if sys.argv[1] == "-p" or sys.argv[1] == "--port":
            port = int(sys.argv[2])
        else:
            raise ValueError("Unknown command line argument: %s" % sys.argv[1])

    # Create a context and a socket
    context = zmq.Context()
    socket = context.socket(zmq.REQ)
    socket.connect(f"tcp://localhost:{port}")


if __name__ == "__main__":
    main()
