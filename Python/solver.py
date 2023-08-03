"""Solver for the PythonBackend.

It spins up a server that listens for optimization requests from AUIT
and responds with the Pareto optimal solutions to the layout optimization
problem."""

import server
import zmq
import optimization
import networking.layout


def optimize_layout(
    n_objectives: int, n_constraints: int, initial_layout: networking.layout.Layout
) -> tuple[list[networking.layout.Layout], networking.layout.Layout]:
    """Return the Pareto optimal solutions to the layout optimization problem
    and a suggested layout."""
    # Create a context and a socket
    AUIT_PORT = 5556
    context = zmq.Context()
    socket = context.socket(zmq.REQ)
    socket.connect(f"tcp://localhost:{AUIT_PORT}")

    # Generate the Pareto optimal layouts and the suggested layout
    layouts, suggested_layout = optimization.generate_pareto_optimal_layouts_and_suggested(
        n_objectives, n_constraints, initial_layout, socket
    )

    # Return the Pareto optimal solutions
    return layouts, suggested_layout


def main():
    """Main function."""
    return server.run_server(port=5555)


if __name__ == "__main__":
    main()
