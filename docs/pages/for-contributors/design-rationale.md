# Design Rationale

This guide explains the current structure of Flow and the reasons behind its main API choices.

# 1. The Core Architecture: Recipe and Chef

Flow separates declaring a computation from running it. The public API composes a flow through this path:

`Flow DSL -> Description -> AST -> Interpreter`

`IFlow<T>` is the public type used by factories and operators. Factories such as `Flow.Create` and operators such as `Select` return description nodes that record the operation and its inputs. These nodes implement the internal `IDescription<T>` interface, whose `Lower()` method creates the corresponding AST node.

`FlowEngine.ExecuteAsync` lowers a description and passes the resulting AST to an interpreter. The interpreter visits each AST node and returns an `Outcome<T>`. This keeps the public API focused on composing and executing flows while the AST and interpreter handle execution details.

Composition creates new descriptions, but it does not make captured delegates pure or their state safe to share. Callbacks run according to the operator and execution path; if they capture mutable state, the application remains responsible for its behaviour under repeated or concurrent execution.

## 1a. Runtime-Generated Child Flows

Some operations return another flow from a callback. `Chain`, `Recover`, and the `use` callback of `WithResource` are examples. Their child flows generally cannot be known while the description is being composed. The callback runs during interpretation, and the returned flow is then lowered and executed.

This boundary matters when inspecting a flow: its composed description can show the callback, but it cannot enumerate flows that the callback has not yet produced.

## 1b. Pragmatism over Purity

Flow draws on functional programming while aiming to remain approachable to C# developers. Its named operators provide familiar ways to compose operations without requiring users to learn functional programming terminology first.

For example, `Flow.All` and `Flow.Any` offer explicit ways to combine concurrent flows. The API favours these discoverable operations over requiring callers to express every combination through a more general abstraction.

For a mapping of Flow concepts to functional programming, see [Notes for FP Developers](./for-fp-developers.md).

# 2. Execution Options and Description IDs

Execution-specific options are separate from the flow description. The public `FlowEngine.ExecuteAsync` overload accepts `Execution.Options`, which currently contains a `CancellationToken`. A token only affects operations that observe it.

Each internal description node has a string ID generated when that node is constructed. These IDs belong to the internal description model; Flow does not expose public `FlowId` or `OperationId` types or an observer and diagnostic-event API.

# 3. The Failure Model

`Failure<T>` contains an `Exception`, and executing a flow returns either `Success<T>` or `Failure<T>` through `Outcome<T>`. This follows familiar .NET error handling. Use failure for exceptional conditions; represent expected business outcomes in the success value when that makes the domain clearer.

# 4. Resource Management

`Flow.WithResource` accepts an acquisition callback and a `use` callback that receives the acquired resource and returns a flow. The interpreter executes the child flow and disposes the resource when its scope completes. Synchronous and asynchronous disposable resources have corresponding overloads.

The acquisition callback determines which resource instance is supplied for an execution. The API does not itself guarantee that separate executions receive distinct instances; resource ownership and sharing remain the caller's responsibility.

# 5. Built-in Behaviours During Lowering

`.WithRetry()` and `.WithTimeout()` add behaviour description nodes. When lowered, each node lowers its source and applies the corresponding strategy to the resulting AST. The strategy changes the operation-specific AST nodes, and the interpreter executes that result.

The effect depends on where the behaviour is placed. For example, retry on `Create` wraps that creation operation, while retry on ordinary operators such as `Select` is a no-op. Timeout can propagate through ordinary operators to their upstream flow. See [Behaviours](../reference/behaviours.md#applicability) for the current cases and resource limits.

Custom `.WithBehaviour()` has different timing: the public extension calls `IBehaviour.Apply` during composition and retains the returned flow description. Put work intended for every execution in operators returned by `Apply`; code run directly inside `Apply` runs when the behaviour is composed.

# 6. The `Validate` Operator

`Validate` inspects a value on the success path. Its predicate receives the value, and its exception factory creates a failure when the predicate returns `false`. A failed upstream flow passes through without invoking the validation predicate.

The synchronous and asynchronous overloads use `Func` delegates. The cancellable asynchronous overload also receives the execution `CancellationToken` so the predicate can observe cancellation.
