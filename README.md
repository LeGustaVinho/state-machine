# State Machine

A robust and flexible state machine implementation for Unity, designed to handle complex game logic with ease, based on Unity Mecanim. The AdvancedStateMachine class allows you to define states, transitions, and conditions using various parameter types such as floats, integers, booleans, and triggers.

##Features

- **Generic and Type-Safe:** Utilize generic types to define states and parameters.
- **Multiple Parameter Types:** Supports float, int, bool, and trigger parameters.
- **Flexible Conditions:** Define complex transition conditions using multiple parameters.
- **Priority-Based Transitions:** Manage transition priorities to control state flow.
- **Easy Integration:** Seamlessly integrates with Unity's lifecycle methods.
- **Flexible States:** Define custom states with enter, update, and exit behaviors.
- **Condition Operations:** Utilize WhenAll or WhenAny logic for transition conditions.

## How to install

### - From OpenUPM:

- Open **Edit -> Project Settings -> Package Manager**
- Add a new Scoped Registry (or edit the existing OpenUPM entry)

| Name  | package.openupm.com  |
| ------------ | ------------ |
| URL  | https://package.openupm.com  |
| Scope(s)  | com.legustavinho  |

- Open **Window -> Package Manager**
- Click `+`
- Select `Add package from git URL...`
- Paste `com.legustavinho.legendary-tools-state-machine` and click `Add`

##Basic Usage
###Defining States
Create custom states by inheriting from the `State<T>` class, where T is the type of your parameter identifiers (e.g., `string`, `enum`).

```csharp
public class IdleState : State<string>
{
    public IdleState() : base("Idle") { }

    protected override void OnStateEntered()
    {
        Console.WriteLine("Entered Idle State");
    }

    protected override void OnStateUpdated()
    {
        Console.WriteLine("Updating Idle State");
    }

    protected override void OnStateExited()
    {
        Console.WriteLine("Exited Idle State");
    }
}

public class MoveState : State<string>
{
    public MoveState() : base("Move") { }

    protected override void OnStateEntered()
    {
        Console.WriteLine("Entered Move State");
    }

    // Implement other methods as needed
}
```
###Creating the State Machine

Instantiate the `AdvancedStateMachine<T>` class, providing an "AnyState" and a name for your state machine.

```csharp
var anyState = new State<string>("Any");
var stateMachine = new AdvancedStateMachine<string>(anyState, "CharacterStateMachine");
```
###Adding Parameters
Add parameters that will be used in transitions. Parameters can be of types `Float`, `Int`, `Bool`, or `Trigger`.

```csharp
stateMachine.AddParameter("IsMoving", ParameterType.Bool);
stateMachine.AddParameter("Speed", ParameterType.Float);
```

###Defining Transitions
Connect states and define conditions for transitions using the ConnectTo method and adding conditions.

```csharp
var idleState = new IdleState();
var moveState = new MoveState();

// Add states to the state machine
stateMachine.AddNode(idleState);
stateMachine.AddNode(moveState);

// Connect Idle to Move when IsMoving is true
var toMoveTransition = idleState.ConnectTo(
    moveState, 
    priority: 0, 
    NodeConnectionDirection.Unidirectional
);
toMoveTransition.AddCondition("IsMoving", BoolParameterCondition.True);

// Connect Move to Idle when IsMoving is false
var toIdleTransition = moveState.ConnectTo(
    idleState, 
    priority: 0, 
    NodeConnectionDirection.Unidirectional
);
toIdleTransition.AddCondition("IsMoving", BoolParameterCondition.False);
```

###Starting the State Machine
Start the state machine by specifying the initial state.

```csharp
stateMachine.Start(idleState);
```

###Updating Parameters and State Machine
Change parameter values and update the state machine, typically within your application's update loop.

```csharp
// Simulate movement input
stateMachine.SetBool("IsMoving", true);

// Update the state machine
stateMachine.Update();

// Output:
// Entered Move State

// Simulate stopping movement
stateMachine.SetBool("IsMoving", false);

// Update the state machine
stateMachine.Update();

// Output:
// Exited Move State
// Entered Idle State
```

##Advanced Usage
###Using Triggers
Triggers are parameters that reset after being consumed in a transition.

```csharp
stateMachine.AddParameter("Jump", ParameterType.Trigger);

var jumpState = new State<string>("Jump");
stateMachine.AddNode(jumpState);

// Transition from any state to Jump when Jump trigger is set
var jumpTransition = anyState.ConnectTo(
    jumpState, 
    priority: 1, 
    NodeConnectionDirection.Unidirectional
);
jumpTransition.AddCondition("Jump");

// Set the trigger
stateMachine.SetTrigger("Jump");

// Update the state machine
stateMachine.Update();

// Output:
// Entered Jump State
```

###Priority-Based Transitions
Manage transitions that can occur under the same conditions by assigning priorities.

```csharp
// High priority transition
var highPriorityTransition = idleState.ConnectTo(
    jumpState, 
    priority: 0, 
    NodeConnectionDirection.Unidirectional
);
highPriorityTransition.AddCondition("Jump");

// Lower priority transition
var lowPriorityTransition = idleState.ConnectTo(
    moveState, 
    priority: 1, 
    NodeConnectionDirection.Unidirectional
);
lowPriorityTransition.AddCondition("IsMoving", BoolParameterCondition.True);
```
In this scenario, if both `Jump` and `IsMoving` conditions are true, the state machine will transition to `jumpState` due to its higher priority (lower priority value).

###Condition Operations
Specify whether all conditions need to be met (`WhenAll`) or any of them (`WhenAny`) for a transition.

```csharp
// Transition when all conditions are met
var transitionAll = idleState.ConnectTo(
    moveState,
    priority: 0,
    NodeConnectionDirection.Unidirectional,
    ConditionOperation.WhenAll
);
transitionAll.AddCondition("IsMoving", BoolParameterCondition.True);
transitionAll.AddCondition("Speed", FloatParameterCondition.Greater, 0.5f);

// Transition when any condition is met
var transitionAny = idleState.ConnectTo(
    moveState,
    priority: 1,
    NodeConnectionDirection.Unidirectional,
    ConditionOperation.WhenAny
);
transitionAny.AddCondition("IsMoving", BoolParameterCondition.True);
transitionAny.AddCondition("Speed", FloatParameterCondition.Greater, 0.5f);
```

###Subscribing to Events
Monitor state machine events like start, stop, and transitions, as well as state-specific events.

```csharp
// State machine events
stateMachine.OnStart += (sm) => Console.WriteLine("State Machine Started");
stateMachine.OnStop += (sm) => Console.WriteLine("State Machine Stopped");
stateMachine.OnTransit += (fromState, toState) =>
{
    Console.WriteLine($"Transitioned from {fromState?.Name} to {toState?.Name}");
};

// State events
idleState.OnStateEnter += (state) => Console.WriteLine($"{state.Name} Entered");
idleState.OnStateExit += (state) => Console.WriteLine($"{state.Name} Exited");
```

##Notes
- Ensure that all parameters used in conditions are added to the state machine using `AddParameter`.
- The `SetTrigger`, `SetBool`, `SetInt`, and `SetFloat` methods automatically check for transitions after updating the parameter.
- When connecting states, make sure to specify the correct direction using `NodeConnectionDirection.Unidirectional` or `NodeConnectionDirection.Bidirectional`.

##License
This project is licensed under the MIT License.

##Contributing
Contributions are welcome! Please submit a pull request or open an issue for any bugs or feature requests.