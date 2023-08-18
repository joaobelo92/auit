"""Classes and functions for the multi-objective optimization of layouts."""

import numpy as np
from pymoo.core.result import Result
from pymoo.core.problem import Problem
from pymoo.core.callback import Callback
from pymoo.algorithms.moo.nsga3 import NSGA3
from pymoo.algorithms.moo.unsga3 import UNSGA3
from pymoo.algorithms.moo.rvea import RVEA
from pymoo.algorithms.moo.sms import SMSEMOA
from pymoo.util.ref_dirs import get_reference_directions
from pymoo.termination import get_termination
from pymoo.optimize import minimize
from pymoo.mcdm.high_tradeoff import HighTradeoffPoints
from pymoo.decomposition.aasf import AASF
from pymoo.visualization.scatter import Scatter
import client
import networking.layout
import networking.element
from tqdm import tqdm
import time
from typing import Literal, Optional

# Disable pymoo warnings
from pymoo.config import Config

Config.warnings["not_compiled"] = False


# Experiment Settings:
# Optimization Method: NSGA-III vs Weighted Sum --> NSGA-III (ref_dirs: Riesz, pop: 1000, n_gen: 100), WS on true PF with w_1 = w_2 = 0.5
# Objective Formulations: Discrete Hand Reachability Objective --> NSGA-III (ref_dirs: Riesz, pop: 1000, n_gen: 100), WS on true PF with w_1 = w_2 = 0.5
# Algorithms: NSGA-III vs U-NSGA-III vs SMSEMOA --> NSGA-III (ref_dirs: Riesz, pop: 4, n_gen: 100), U-NSGA-III (ref_dirs: Riesz, pop: 4, n_gen: 100), SMSEMOA (ref_dirs: Riesz, pop: 4, n_gen: 100)
# Decompositions: WS vs Tchebicheff vs AASF --> w_1 = 0.25, w_2 = 0.75; w_1 = w_2 = 0.5; w_1 = 0.75, w_2 = 0.25 with AASF(eps=1e-10, beta=25) to provide sharper direction towards 45 deg line

class PrintProgress(Callback):
    """A callback to print the current progress (i.e., the current generation)."""

    def __init__(self, n_gen):
        super().__init__()
        self.n_gen = n_gen

    def notify(self, algorithm):
        # print(f"Generation: {algorithm.n_iter}")
        tqdm(
            desc="n_gen",
            total=self.n_gen,
            initial=algorithm.n_iter,
            unit="gen",
        ).update()

class LayoutProblem(Problem):
    """A multi-objective optimization problem for layouts."""

    def __init__(
        self,
        n_objectives: int,
        n_constraints: int,
        initial_layout: networking.layout.Layout,
        socket,
        **kwargs,
    ):

        self.initial_layout = initial_layout

        """Initialize the problem."""
        # Calculate the number of variables
        n_variables = (
            initial_layout.n_items * 7
        )  # 3 position variables + 4 rotation variables


        # Set the lower and upper bounds:
        # Each position is bounded between -3 and 3 for x and z and -2 and 2 for y (This is arbitrary)
        # Each rotation is bounded between 0 and 1 for x, y, z and w
        xlower = [-3] * n_variables
        xupper = [3] * n_variables
        xlower[1] = -2
        xupper[1] = 2
        for i in range(3, n_variables, 7):
            for j in range(4):  # x, y, z and w
                xlower[i + j] = 0
                xupper[i + j] = 1

        # Call the superclass constructor
        super().__init__(
            n_var=n_variables,
            n_obj=n_objectives,
            n_ieq_constr=n_constraints,
            xl=xlower,
            xu=xupper,
            **kwargs,
        )

        # Store the socket
        self.socket = socket

    def _evaluate(self, x: np.ndarray, out, *args, **kwargs):
        """Evaluate the problem."""
        # Convert the decision variables to a list of layouts
        layouts = [self._x_to_layout(x[i]) for i in range(x.shape[0])]

        # Send the layouts to the server and receive the costs
        response_type, response_data = client.send_costs_request(self.socket, layouts)

        # Check if the response is an EvaluationResponse
        if response_type == "e":
            # Transform costs to a numpy array by storing
            # only the value of the costs for each layout's costs
            costs = np.array(
                [
                    [cost for cost in layout_costs]
                    for layout_costs in response_data.costs
                ]
            )

            # Set the objectives
            out["F"] = costs

            # If the problem is constrained, check the constraints
            if self.n_ieq_constr > 0:

                # Transform violations to a numpy array
                violations = np.array(
                    [
                        [violation for violation in layout_constraint_violations]
                        for layout_constraint_violations in response_data.violations
                    ]
                )

                # Set the constraint violations
                out["G"] = violations

        # If response type is unknown, print a message
        else:
            print("Received an unknown response type: %s" % response_type)

    def _x_to_layout(self, x):
        """Convert the decision variables to a layout."""
        # Create a list of items
        items = []
        for i in range(0, len(x), 7):
            items.append(
                networking.element.Element(
                    id=self.initial_layout.items[i].id,
                    position=networking.element.Position(x=x[i], y=x[i + 1], z=x[i + 2]),
                    rotation=networking.element.Rotation(x=x[i + 3], y=x[i + 4], z=x[i + 5], w=x[i + 6]),
                )
            )

        # Create and return the layout
        return networking.layout.Layout(items=items)


# Function to create an algorithm instance (pop size: Exp. 1-2: 1000, Exp. 3: 4)
def get_algorithm(n_objectives: int, pop_size: int = 100, seed: int = 1):
    """Create an algorithm instance."""
    # create the reference directions to be used for the optimization
    # ref_dirs = get_reference_directions(
    #     "uniform", n_objectives, n_partitions=pop_size-1, seed=seed
    # )  # Exp. 3
    ref_dirs = get_reference_directions("energy", n_objectives, pop_size, seed=seed)

    # create the algorithm object
    algorithm = NSGA3(pop_size=pop_size, ref_dirs=ref_dirs, seed=seed)  # Exp. 1-3
    # algorithm = UNSGA3(pop_size=pop_size, ref_dirs=ref_dirs, seed=seed)  # Exp. 3
    # algorithm = SMSEMOA(pop_size=pop_size, ref_dirs=ref_dirs, seed=seed)  # Exp. 3
    # algorithm = RVEA(pop_size=pop_size, ref_dirs=ref_dirs, seed=seed)  # Exp. 3

    return algorithm


# Function to generate the Pareto optimal layouts (i.e., the Pareto front)
def generate_pareto_optimal_layouts_and_suggested(
    n_objectives: int,
    n_constraints: int,
    initial_layout: networking.layout.Layout,
    socket,
    reduce: Optional[Literal['htp', 'aasf', 'aasf-riesz']] = 'aasf-riesz',
    plot=False,
    save=False,
    verbose=True,
) -> tuple[list[networking.layout.Layout], networking.layout.Layout]:
    """Generate the Pareto optimal layouts and a suggested default layout as a compromise solution.

    Args:
        n_objectives: The number of objectives.
        initial_layout: The initial layout.
        socket: The socket to use for communication with the server.
        reduce: Whether to reduce the Pareto front using the high tradeoff points algorithm.
        plot: Whether to plot the Pareto front.
    """
    # Start the timer
    start_time = time.time()

    # Create the problem
    problem = LayoutProblem(
        n_objectives=n_objectives,
        n_constraints=n_constraints,
        initial_layout=initial_layout,
        socket=socket,
    )

    # Create the algorithm
    algorithm = get_algorithm(n_objectives)

    # Create the termination criterion
    n_gen = 100  # Exp. 1-3: 100
    termination = get_termination("n_gen", n_gen)

    # Run the optimization
    res = minimize(
        problem,
        algorithm,
        termination,
        seed=1,
        callback=PrintProgress(n_gen=n_gen),
        # verbose=True,
        save_history=False, # setting this to true leads to a crash // pymoo 0.6.0.1
        copy_algorithm=False,
    )

    # Print the results
    if verbose:
        print("Pareto front: %s" % res.F)
        print("Non-dominated solutions: %s" % res.X)
        print("Elapsed time: %s seconds" % (round(time.time() - start_time, 2)))

    # Save the results
    if save:
        algorithm_name = algorithm.__class__.__name__
        np.save(
            f"examples/neck_and_arm_angle/algorithm/{algorithm_name}_pareto_front.npy",
            res.F,
        )
        np.save(
            f"examples/neck_and_arm_angle/algorithm/{algorithm_name}_non_dominated_solutions.npy",
            res.X,
        )

    scatterplot = Scatter(title="Pareto front")
    scatterplot.add(res.F, alpha=0.5)

    # If a single global optimum is found, return it
    if res.F.shape[0] == 1:
        if (
            res.X.shape[0] == 1
        ):  # If the array is 1D (i.e., single-objective; e.g., res.X: [-2.1 -2.4 13.4  0.1  0.8  0.4  0.6])
            single_optimal_layout = problem._x_to_layout(res.X[0])
            return [single_optimal_layout], single_optimal_layout
        # else if the array is 2D (i.e., multi-objective; e.g., res.X: [[-2.1 -2.4 13.4  0.1  0.8  0.4  0.6]])
        single_optimal_layout = problem._x_to_layout(res.X)
        return [single_optimal_layout], single_optimal_layout

    # If the reduce flag is set to True...
    if reduce:
        if reduce == "htp":
            # ...reduce the set of Pareto optimal layouts to the high tradeoff points
            htp = HighTradeoffPoints()
            points_of_interest = htp(res.F)  # This is a boolean array
        elif reduce == "aasf-riesz":
            # ...reduce the set of Pareto optimal layouts using AASF with Riesz energy norm
            aasf = AASF(rho=1e-4)
            MAX_NO_SOLUTION_PROPOSALS = 10
            ref_dirs = get_reference_directions(
                "energy", n_objectives, MAX_NO_SOLUTION_PROPOSALS
            )
            # Determine the Pareto optimal layouts
            points_of_interest = np.zeros(res.F.shape[0], dtype=bool)
            for ref_dir in ref_dirs:
                aasf_optimum_index = aasf.do(res.F, weights=ref_dir).argmin()
                points_of_interest[aasf_optimum_index] = True
        elif reduce == "aasf":
            # ...reduce the set of Pareto optimal layouts using AASF
            aasf = AASF(eps=0, beta=5)
            # Create an array of weight combinations to yield n_obj+1 layouts
            # There is a weight combination for each objective and one for the equal weights
            weights = np.zeros((n_objectives + 1, n_objectives))
            for i in range(n_objectives):
                weights[i, i] = 1
            weights[-1, :] = 1 / n_objectives
            # Determine the Pareto optimal layouts
            points_of_interest = np.zeros(res.F.shape[0], dtype=bool)
            for weight_combination in weights:
                aasf_optimum_index = aasf.do(res.F, weights=weight_combination).argmin()
                points_of_interest[aasf_optimum_index] = True

        # Add the high tradeoff points to the scatterplot
        scatterplot.add(
            res.F[points_of_interest], s=40, facecolors="none", edgecolors="r"
        )

        # If the Pareto front should be plotted, plot it
        if plot:
            scatterplot.show()

        # Return the Pareto optimal layouts
        pareto_optimal_layouts = [problem._x_to_layout(x) for x in res.X[points_of_interest]]
        # Determine the AASF equal weights layout
        suggested_layout = get_aasf_equal_weights_layout(problem, res)
        return pareto_optimal_layouts, suggested_layout

    # If the Pareto front should be plotted, plot it
    if plot:
        scatterplot.show()

    # Otherwise, return all the Pareto optimal layouts
    pareto_optimal_layouts = [problem._x_to_layout(x) for x in res.X]
    # Determine the AASF equal weights layout
    suggested_layout = get_aasf_equal_weights_layout(problem, res)
    return pareto_optimal_layouts, suggested_layout


def get_aasf_equal_weights_layout(problem: LayoutProblem, res: Result) -> networking.layout.Layout:
    """Determine the AASF equal weights layout given the optimization results.

    Args:
        res: The optimization results.
    """
    aasf = AASF(eps=0, beta=5)
    equal_weights = np.ones(res.F.shape[1] if res.F.ndim == 2 else len(res.F))
    compromise_point = res.X[aasf.do(res.F, weights=equal_weights).argmin()]
    aasf_equal_weights_layout = problem._x_to_layout(compromise_point)
    return aasf_equal_weights_layout