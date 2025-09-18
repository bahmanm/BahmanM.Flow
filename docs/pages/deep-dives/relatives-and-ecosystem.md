# Flow's Relatives in the .NET Ecosystem

Navigating an ecosystem as large as .NET can be quite time-consuming and tricky; trying out things, reading documentation, evaluating this or that, ...

Hopefully, this guide can help you save some precious time:
-  It **isn't** a comprehensive list of all the libraries out there
-  It **isn't** a feature-by-feature comparison; that'd be pretty pointless.
-  It **is** a choose-by-scenario overview; so that you can see how Flow fits alongside (and sometimes within) your favourite library.

---

# Flow and Polly: Composable Flows and Resiliency Wrappers

Polly focuses on *resilience policies* applied to *individual operations*. 

Flow focuses on *composing the entire business logic as a Flow*, with resilience as a native, integral behaviour.

| Scenario                            | Use Polly                                                                                                                                                      | Use Flow                                                                                                                                                                                                      |
|:------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Add Resilience to Existing Code** | When you need to quickly add a retry or circuit breaker to an existing HTTP call, database query, or method invocation without touching the surrounding logic. | You are designing a new business operation or refactoring an existing one, and you want resilience (retries, timeouts, recoveries) to be an inherent, composable part of the entire workflow's definition.    |
| **Declarative Workflow Definition** | You apply policies imperatively around your code.                                                                                                              | You define your entire business operation as a declarative Flow, where each step explicitly defines its success, failure, and how to recover. Resiliency becomes a composable 'behaviour' of the Flow itself. |
| **Error Handling**                  | Handle exceptions at the boundary of the policy.                                                                                                               | Handle errors and outcomes explicitly across the *entire Flow* using `Outcome`. This provides a more cohesive and granular way to manage failures and recoveries within the business logic.               |
| **Interoperability**                | You can absolutely use Polly *within* an individual step of a Flow, say, to add specific HTTP client resilience. They are not mutually exclusive.              | Flow isn't a replacement for Polly for all scenarios. Think of Flow as the conductor for your entire business opera, where Polly might be a specialized instrument playing a particular part within one act.  |

---

# Flow and MediatR: Orchestrating Operations and Mediating Commands

MediatR is about *decoupling command/query senders from their handlers*. 

Flow is about *composing a series of operations into a single, cohesive, and resilient business transaction*.

| Scenario                           | Use MediatR                                                                                                                                                                                                                                                     | Use Flow                                                                                                                                                                                        |
|:-----------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Decoupling and Event Sourcing**  | Decouple the sender of a command or query from its specific handler implementation (a la CQRS). Great for managing application-level commands and events.                                                                                                       | Define the *sequence and logic* of what actually happens *inside* a command handler, or when a query is executed. Flow helps you compose the actual work, rather than just mediate the request. |
| **Request/Response Orchestration** | Orchestrate the *dispatch* of a request to its handler.                                                                                                                                                                                                         | Orchestrate the *execution flow* within a handler: define the series of steps, error handling, and recovery logic that constitutes the business operation.                                      |
| **Interoperability**               | They shine together! You can use Flow to define the complex business logic that lives *inside* a MediatR handler. A MediatR handler might receive a command, and then internally invoke a Flow to execute the actual processing. They are highly complementary. |                                                                                                                                                                                                 |

---

# Flow and LanguageExt: Pragmatic Bridge and Pure Functional Ecosystem

LanguageExt offers a comprehensive, opinionated, and powerful purely functional programming toolkit for C#. 

Flow offers a pragmatic, lightweight bridge to declarative composition and explicit outcome management for mainstream C# developers.

| Scenario                            | Use LanguageExt                                                                                                                                                                                                                                                                                                          | Use Flow                                                                                                                                                                                                                                                                                                                                            |
|:------------------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Comfort Level**                   | Your team is deeply committed to, and proficient in, purely functional programming paradigms. You are comfortable with concepts like monads, functors, higher-kinded types, and extensive use of immutable data structures. You value type-safety and referential transparency above all else.                           | Your team consists of C# developers with strong OOP backgrounds who are looking to gain the benefits of composing Flows and explicit outcome management *without* a steep learning curve or needing to adopt a full functional programming paradigm. Flow aims to be ergonomic within a familiar C# context.                                   |
| **Ecosystem and Scope**             | You need a vast and comprehensive library that re-implements many core types and patterns from scratch, offering a complete FP ecosystem within C#.                                                                                                                                                                      | You want a minimalistic library focused specifically on the declarative composition of operations and the explicit handling of their outcomes (`Outcome<T>`). It provides a focused set of tools without requiring a complete shift in how you write all your C# code.                                                                              |
| **Philosophical Approach**          | Strongly adhere to pure functional programming principles and a full immersion into an FP style.                                                                                                                                                                                                                         | Prefer something pragmatic and approachable. It's about bringing the *benefits* of compositional thinking to C# developers, not about enforcing a specific FP dogma. It integrates cleanly with existing C# OOP codebases, allowing for incremental adoption.                                                                                       |
| **Error Handling and Side Effects** | Robust mechanisms (e.g., `Either`, `Option`, `Try`) for handling effects and errors in a pure, trackable way.                                                                                                                                                                                                            | Simple, explicit way to manage success or failure and recover from errors within a Flow, i.e. `Outcome<T>`. It makes side effects explicit and manageable within the compositional context, but doesn't aim for the same level of purity as LanguageExt across the entire application.                                                     |
| **Interoperability**                | You can use LanguageExt utilities within your Flow steps if you desire. However, if you are already using LanguageExt extensively, the need for Flow might be reduced, as LanguageExt provides its own powerful mechanisms for effect composition. They could potentially coexist in different layers of an application. | Flow is designed to complement existing C# codebases. It provides a focused solution for composing operations that can be adopted incrementally, without requiring your entire project to re-architect around a pure FP paradigm. You can use Flow's `Outcome<T>` within existing C# code, and you can call existing C# methods from within a Flow. |

---

# Conclusion

Flow is designed to be a practical, opinionated tool for C# developers who want to build more resilient and maintainable business logic without a steep learning curve. It complements existing libraries by focusing specifically on the **composition of operations and explicit outcome management**, providing a clear, declarative approach to complex workflows.

_If you find your favourite library missing from this document, all you need to do is [open an issue](https://github.com/bahmanm/BahmanM.Flow/issues/new)!_
