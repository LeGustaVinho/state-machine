# State Machine

State Machine can be used to easily create finite and hierarchical State Machines using the StateMachine class itself or you can inherit from it to create more specific behavior.

Functionalities:

- Hierarchical state machines, where one state machine has another state machine internally
- State, state connections and triggers are classes, which can be extended
- Trigger is strongly typed, it can be a string, enum, int or any other type you want.
- States have input and output events that can be overriden to create behaviors.
- Allows passing of context data between states through state connections
- State connections can be one-way or two-way
- State machine has a Memento, allowing to go back and forward in the history of transitions

Dependencies:

- [Legendary Tools - Graphs](https://github.com/LeGustaVinho/graphs "Legendary Tools - Graphs")

