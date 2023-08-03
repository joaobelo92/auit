"""Server for the Python solver backend."""

import zmq
import sys
import json
from networking.messages import (
    HelloResponse,
    ErrorResponse,
    OptimizationResponse,
    EvaluationResponse,
    from_json,
)
from solver import optimize_layout

def handle_request(request_type, request_data, verbose=False):
    """Return a tuple of the response type and the response data."""
    # If request type is HelloRequest, return a HelloResponse
    if request_type == "H":
        if verbose: print("Received a HelloRequest")
        return "h", HelloResponse()
    # If request type is OptimizationRequest, solve the optimization problem and
    # return an OptimizationResponse
    if request_type == "O":
        if verbose:
            print("Received an OptimizationRequest")
            print("request_data:", request_data)
        solutions, suggested_layout = optimize_layout(
            request_data.n_objectives,
            request_data.n_constraints,
            request_data.initial_layout,
        )
        return "o", OptimizationResponse(solutions=solutions, suggested=suggested_layout)
    if request_type == "P":
        if verbose: print("Received a Problem Layout")
        return "h", None
    # If request type is unknown, return an ErrorResponse
    else:
        if verbose: print("Received an unknown request type: %s" % request_type)
        return "x", ErrorResponse(error="Unknown request type: %s" % request_type)


def run_server(port=5555, verbose=False):
    """Run the server."""
    # Create a context and a socket
    context = zmq.Context()
    socket = context.socket(zmq.REP)
    socket.bind(f"tcp://*:{port}")

    # Loop forever
    while True:

        # Print listening message
        print(f"Listening on port {port}...")

        # Receive a request
        request = socket.recv_string()

        # Print the request
        if verbose:
            print("Received a request:")
            print("request_type:", request[0])
            print("request_data:", json.loads(request[1:]))

        # Parse the request
        request_type = request[0]
        request_data = from_json(request_type, request[1:] if len(request) > 1 else "")

        # Handle the request
        response_type, response_data = handle_request(request_type, request_data, verbose)

        # Send the response
        if response_data is not None:
            if verbose:
                print("Sending response...")
                print("response_type:", response_type)
                print("response_data:", response_data.to_json())
            socket.send_string(
                response_type + response_data.to_json()
            )
        else:
            socket.send_string("ok")


def main():
    """Main function."""
    # Get port number from command line argument named "port" or "p"
    if len(sys.argv) == 1:  # if no arguments are given
        port = 5555
    else:
        if sys.argv[1] == "-p" or sys.argv[1] == "--port":
            port = int(sys.argv[2])
        else:
            raise ValueError("Unknown command line argument: %s" % sys.argv[1])

    # Run the server
    run_server(port)


if __name__ == "__main__":
    main()
