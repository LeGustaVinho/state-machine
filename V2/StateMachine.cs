using System;
using System.Collections.Generic;
using LegendaryTools.GraphV2;
using UnityEngine;

namespace LegendaryTools.StateMachineV2
{
    public interface IHardState<T> where T : struct, Enum, IConvertible
    {
        string Name { get; }
        
        T Type { get; }
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;
        
        internal void InvokeOnStateEnter();
        internal void InvokeOnStateUpdate();
        internal void InvokeOnStateExit();
    }
    
    public class HardState<T> : IHardState<T> where T : struct, Enum, IConvertible
    {
        public string Name => Type.ToString();
        
        public T Type { get; }
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;
        
        void IHardState<T>.InvokeOnStateEnter()
        {
            OnStateEnter?.Invoke();
        }

        void IHardState<T>.InvokeOnStateUpdate()
        {
            OnStateUpdate?.Invoke();
        }

        void IHardState<T>.InvokeOnStateExit()
        {
            OnStateExit?.Invoke();
        }

        public HardState(T type)
        {
            Type = type;
        }
    }

    public interface IHardStateMachine<T> where T : struct, Enum, IConvertible, IEquatable<T>
    {
        string Name { get; }
        bool IsRunning { get; }
        IHardState<T> CurrentState { get; }
        Dictionary<T, IHardState<T>> States { get; }
        void Start(IHardState<T> startState);
        void Stop();
        void Update();
        void SetTrigger(T trigger);
    }

    public class HardStateMachine<T> : IHardStateMachine<T> where T : struct, Enum, IConvertible, IEquatable<T>
    {
        public string Name => typeof(T).Name;
        public bool IsRunning => CurrentState != null;
        public IHardState<T> CurrentState { get; protected set; }
        public Dictionary<T, IHardState<T>> States { get; }

        private readonly Func<IHardState<T>, IHardState<T>, bool> allowTransition;
        
        public HardStateMachine(Func<IHardState<T>, IHardState<T>, bool> allowTransition = null)
        {
            this.allowTransition = allowTransition;
            States = new Dictionary<T, IHardState<T>>();
            string[] enumNames = typeof(T).GetEnumNames();
            foreach (string enumName in enumNames)
            {
                T enumValue = enumName.GetEnumValue<T>();
                States.Add(enumValue, new HardState<T>(enumValue));
            }
        }
        
        public void Start(IHardState<T> startState)
        {
            if(IsRunning) return;
            if (!States.TryGetValue(startState.Type, out IHardState<T> state)) return;
            CurrentState = startState;
        }

        public void Stop()
        {
            if(!IsRunning) return;
            CurrentState = null;
        }

        public void Update()
        {
            if(!IsRunning) return;
            CurrentState.InvokeOnStateUpdate();
        }

        public void SetTrigger(T trigger)
        {
            if(!IsRunning) return;
            if (!States.TryGetValue(trigger, out IHardState<T> toState)) return;
            Transit(CurrentState, toState);
        }
        
        private void Transit(IHardState<T> fromState, IHardState<T> toState)
        {
            if (allowTransition != null && !allowTransition.Invoke(fromState, toState)) return;
            fromState?.InvokeOnStateExit();
            CurrentState = toState;
            toState?.InvokeOnStateEnter();
        }
    }
    
    public interface IStateMachine<T> : IGraph where T : IEquatable<T>
    {
        string Name { get; set; }
        IState<T> AnyState { get; }
        IState<T> CurrentState { get; }
        bool IsRunning { get; }
        
        Dictionary<T, ParameterState<T>> ParameterValues { get; }

        void Start(IState<T> startState);
        void Stop();
        void Update();
        
        void AddParameter(T parameterName, ParameterType parameterType);
        bool RemoveParameter(T parameterName, ParameterType parameterType);
        
        void SetTrigger(T name);
        void SetBool(T name, bool value);
        void SetInt(T name, int value);
        void SetFloat(T name, float value);
    }

    public interface IState<T> : INode where T : IEquatable<T>
    {
        public string Name { get; set; }

        IStateConnection<T> ConnectTo(INode to, int priority, NodeConnectionDirection newDirection, 
            ConditionOperation conditionOperation = ConditionOperation.WhenAll);
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;

        internal void InvokeOnStateEnter();
        internal void InvokeOnStateUpdate();
        internal void InvokeOnStateExit();
    }
    
    public interface IStateConnection<T> : INodeConnection, IComparable<IStateConnection<T>> where T : IEquatable<T>
    {
        List<Condition<T>> Conditions { get; }
        ConditionOperation ConditionOperation { get; internal set; }
        string Name { get; set; }
        int Priority { get; internal set; }
        event Action OnTransit;
        void AddCondition(T name, FloatParameterCondition parameterCondition, float value);
        void AddCondition(T name, IntParameterCondition parameterCondition, int value);
        void AddCondition(T name, BoolParameterCondition parameterCondition);
        void AddCondition(T name);
        void RemoveCondition(Predicate<Condition<T>> predicate);
        bool Evaluate(Dictionary<T, ParameterState<T>> parametersState);
        void ConsumeTriggers(Dictionary<T, ParameterState<T>> parametersState);
        void OnTransited();
        internal void InvokeOnTransit();
    }

    public enum ConditionOperation
    {
        WhenAll,
        WhenAny,
    }
    
    public enum ParameterType
    {
        Float,
        Int,
        Bool,
        Trigger,
    }

    public enum FloatParameterCondition
    {
        Greater,
        Less
    }
    
    public enum IntParameterCondition
    {
        Equals,
        NotEquals,
        Greater,
        Less
    }
    
    public enum BoolParameterCondition
    {
        True,
        False,
    }

    public class ParameterState<T> where T : IEquatable<T>
    {
        public T Name;
        public ParameterType Type;
        public float Value;

        public ParameterState(T name, ParameterType type, float value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }
    
    public abstract class Condition<T> where T : IEquatable<T>
    {
        public T Name;
        public ParameterType Type { get; protected set; }

        public abstract bool Evaluate(T name, ParameterState<T> parameterState);
    }
    
    public class FloatCondition<T> : Condition<T> where T : IEquatable<T>
    {
        public FloatParameterCondition Condition;
        public float Value;

        public FloatCondition(T name)
        {
            Name = name;
            Type = ParameterType.Float;
        }

        public FloatCondition(T name, FloatParameterCondition condition, float value) : this(name)
        {
            Condition = condition;
            Value = value;
        }

        public override bool Evaluate(T name, ParameterState<T> parameterState)
        {
            if (!Name.Equals(name)) return false;
            return Condition switch
            {
                FloatParameterCondition.Greater => parameterState.Value > Value,
                FloatParameterCondition.Less => parameterState.Value < Value,
                _ => false
            };
        }
    }
    
    public class IntCondition<T> : Condition<T> where T : IEquatable<T>
    {
        public IntParameterCondition Condition;
        public int Value;
        
        public IntCondition(T name)
        {
            Name = name;
            Type = ParameterType.Int;
        }
        
        public IntCondition(T name, IntParameterCondition condition, int value) : this(name)
        {
            Condition = condition;
            Value = value;
        }
        
        public override bool Evaluate(T name, ParameterState<T> parameterState)
        {
            if (!Name.Equals(name)) return false;
            return Condition switch
            {
                IntParameterCondition.Equals =>  Convert.ToInt32(parameterState.Value) == Value,
                IntParameterCondition.NotEquals => Convert.ToInt32(parameterState.Value) != Value,
                IntParameterCondition.Greater => parameterState.Value > Value,
                IntParameterCondition.Less => parameterState.Value < Value,
                _ => false
            };
        }
    }
    
    public class BoolCondition<T> : Condition<T> where T : IEquatable<T>
    {
        public BoolParameterCondition Condition;
        
        public BoolCondition(T name)
        {
            Name = name;
            Type = ParameterType.Bool;
        }
        
        public BoolCondition(T name, BoolParameterCondition condition) : this(name)
        {
            Condition = condition;
        }
        
        public override bool Evaluate(T name, ParameterState<T> parameterState)
        {
            if (!Name.Equals(name)) return false;
            return Condition switch
            {
                BoolParameterCondition.True => Convert.ToBoolean(parameterState.Value),
                BoolParameterCondition.False => !Convert.ToBoolean(parameterState.Value),
                _ => false
            };
        }
    }
    
    public class TriggerCondition<T> : Condition<T> where T : IEquatable<T>
    {
        public TriggerCondition(T name)
        {
            Name = name;
            Type = ParameterType.Trigger;
        }

        public override bool Evaluate(T name, ParameterState<T> parameterState)
        {
            return Name.Equals(name) && Convert.ToBoolean(parameterState.Value);
        }
    }

    public class StateConnection<T> : NodeConnection, IStateConnection<T> where T : IEquatable<T>
    {
        public string Name { get; set; }

        private int priority;
        int IStateConnection<T>.Priority
        {
            get => priority;
            set => priority = value;
        }
        
        private ConditionOperation conditionOperation;
        ConditionOperation IStateConnection<T>.ConditionOperation
        {
            get => conditionOperation;
            set => conditionOperation = value;
        }

        public event Action OnTransit;
        public List<Condition<T>> Conditions { get; protected set; } = new List<Condition<T>>();

        public StateConnection(INode fromNode, INode toNode, int priority, NodeConnectionDirection direction,
            ConditionOperation conditionOperation = ConditionOperation.WhenAll) : base(fromNode, toNode, direction)
        {
            this.priority = priority;
            this.conditionOperation = conditionOperation;
        }

        public void AddCondition(T name, FloatParameterCondition parameterCondition, float value)
        {
            ValidateParam(name, ParameterType.Float);
            Conditions.Add(new FloatCondition<T>(name, parameterCondition, value));
        }
        
        public void AddCondition(T name, IntParameterCondition parameterCondition, int value)
        {
            ValidateParam(name, ParameterType.Int);
            Conditions.Add(new IntCondition<T>(name, parameterCondition, value));
        }

        public void AddCondition(T name, BoolParameterCondition parameterCondition)
        {
            ValidateParam(name, ParameterType.Bool);
            Conditions.Add(new BoolCondition<T>(name, parameterCondition));
        }
        
        public void AddCondition(T name)
        {
            ValidateParam(name, ParameterType.Trigger);
            Conditions.Add(new TriggerCondition<T>(name));
        }

        private void ValidateParam(T name, ParameterType expectedDefinition)
        {
            IGraph rootGraph = FromNode.Owner.GraphHierarchy.Length == 0 ? FromNode.Owner : FromNode.Owner.GraphHierarchy[0];
            if(rootGraph is not IStateMachine<T> rootStateMachine) 
                throw new InvalidOperationException($"Root {nameof(StateMachine<T>)} does not implements {nameof(IStateMachine<T>)}.");
            StateMachine<T>.ValidateParam(name, rootStateMachine, expectedDefinition, out ParameterState<T> parameterState);
        }

        public void RemoveCondition(Predicate<Condition<T>> predicate)
        {
            Conditions.RemoveAll(predicate);
        }

        public bool Evaluate(Dictionary<T, ParameterState<T>> parametersState)
        {
            switch (conditionOperation)
            {
                case ConditionOperation.WhenAll:
                {
                    foreach (Condition<T> condition in Conditions)
                    {
                        if (!parametersState.TryGetValue(condition.Name, out ParameterState<T> cParameterState))
                        {
                            throw new InvalidOperationException($"You are trying to test a condition called {condition.Name} that has no parameter in the {nameof(StateMachine<T>)}.");
                        }
                        if (!condition.Evaluate(condition.Name, cParameterState)) return false;
                    }
                    return true;
                }
                case ConditionOperation.WhenAny:
                {
                    foreach (Condition<T> condition in Conditions)
                    {
                        if (!parametersState.TryGetValue(condition.Name, out ParameterState<T> cParameterState))
                        {
                            throw new InvalidOperationException($"You are trying to test a condition called {condition.Name} that has no parameter in the {nameof(StateMachine<T>)}.");
                        }
                        if (condition.Evaluate(condition.Name, cParameterState)) return true;
                    }
                    return false;
                }
            }

            return false;
        }

        public void ConsumeTriggers(Dictionary<T, ParameterState<T>> parametersState)
        {
            foreach (Condition<T> condition in Conditions)
            {
                if (condition.Type == ParameterType.Trigger) parametersState[condition.Name].Value = 0;
            }
        }

        public virtual void OnTransited()
        {

        }
        
        void IStateConnection<T>.InvokeOnTransit()
        {
            OnTransited();
            OnTransit?.Invoke();
        }

        public int CompareTo(IStateConnection<T> other)
        {
            return priority.CompareTo(other.Priority);
        }
    }
    
    public class State<T> : Node, IState<T> where T : IEquatable<T>
    {
        public string Name { get; set; }
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;

        public State(string name = "") : base(false)
        {
            Name = name;
        }
        
        protected override INodeConnection ConstructConnection(INode fromNode, INode toNode, NodeConnectionDirection direction)
        {
            return new StateConnection<T>(fromNode, toNode, 0, direction);
        }

        public override INodeConnection ConnectTo(INode to, NodeConnectionDirection newDirection)
        {
            throw new InvalidOperationException($"Call you should call signature {nameof(ConnectTo)}(INode to, int priority, NodeConnectionDirection newDirection, ConditionOperation conditionOperation = ConditionOperation.WhenAll) instead.");
        }

        public virtual IStateConnection<T> ConnectTo(INode to, int priority, NodeConnectionDirection newDirection, 
            ConditionOperation conditionOperation = ConditionOperation.WhenAll)
        {
            INodeConnection nodeConnection = base.ConnectTo(to, newDirection);
            if (nodeConnection is not IStateConnection<T> stateConnection) 
                throw new InvalidOperationException($"nodeConnection does not implement {nameof(IStateConnection<T>)}. Did you forget to override method {nameof(ConstructConnection)} ?");

            stateConnection.ConditionOperation = conditionOperation;
            stateConnection.Priority = priority;
            return stateConnection;
        }

        protected virtual void OnStateEntered()
        {
        }

        protected virtual void OnStateUpdated()
        {
        }

        protected virtual void OnStateExited()
        {
        }
        
        void IState<T>.InvokeOnStateEnter()
        {
            OnStateEntered();
            OnStateEnter?.Invoke();
        }

        void IState<T>.InvokeOnStateUpdate()
        {
            OnStateUpdated();
            OnStateUpdate?.Invoke();
        }

        void IState<T>.InvokeOnStateExit()
        {
            OnStateExited();
            OnStateExit?.Invoke();
        }
    }
    
    public class StateMachine<T> : GraphV2.Graph, IStateMachine<T> where T : IEquatable<T>
    {
        public string Name { get; set; }
        public IState<T> AnyState { get; }
        public bool IsRunning => CurrentState != null;
        public IState<T> CurrentState { get; protected set; }

        public Dictionary<T, ParameterState<T>> ParameterValues { get; protected set; } = new Dictionary<T, ParameterState<T>>();
        
        public StateMachine(IState<T> anyState, string name = "")
        {
            Name = name;
            AnyState = anyState;
        }
        
        public void Start(IState<T> startState)
        {
            if (IsRunning) return;
            if (!Contains(startState)) throw new InvalidOperationException($"{nameof(startState)} must be a state inside of {Name} {nameof(StateMachine<T>)}");
            Transit(null, startState, null);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            Transit(CurrentState, null, null);
            CurrentState = null;
        }

        public void Update()
        {
            if(IsRunning) CurrentState.InvokeOnStateUpdate();
        }

        private void Transit(IState<T> fromState, IState<T> toState, IStateConnection<T> transition)
        {
            fromState?.InvokeOnStateExit();
            transition?.InvokeOnTransit();
            CurrentState = toState;
            toState?.InvokeOnStateEnter();
        }
        
        public void AddParameter(T parameterName, ParameterType parameterType)
        {
            if (ParameterValues.ContainsKey(parameterName))
            {
                throw new InvalidOperationException($"{Name} {nameof(StateMachine<T>)} already has {parameterName} parameter");
            }
            
            ParameterValues.Add(parameterName, new ParameterState<T>(parameterName, parameterType, 0));
        }

        public bool RemoveParameter(T parameterName, ParameterType parameterType)
        {
            if (!ParameterValues.ContainsKey(parameterName))
            {
                return false;
            }
            
            ParameterValues.Remove(parameterName);
            return true;
        }

        public void SetTrigger(T name)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine<T>)} is not running.");
            ValidateParam(name, this, ParameterType.Trigger, out ParameterState<T> parameterState);
            ParameterValues[name].Value = Convert.ToSingle(true);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetBool(T name, bool value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine<T>)} is not running.");
            ValidateParam(name, this, ParameterType.Bool, out ParameterState<T> parameterState);
            ParameterValues[name].Value = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetInt(T name, int value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine<T>)} is not running.");
            ValidateParam(name, this, ParameterType.Int, out ParameterState<T> parameterState);
            ParameterValues[name].Value = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetFloat(T name, float value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine<T>)} is not running.");
            ValidateParam(name, this, ParameterType.Float, out ParameterState<T> parameterState);
            ParameterValues[name].Value = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }
        
        private bool CheckTriggerForState(IState<T> state)
        {
            List<(IState<T>, IStateConnection<T>)> availableTransitions = new List<(IState<T>, IStateConnection<T>)>();
            foreach (INodeConnection nodeConnection in state.OutboundConnections)
            {
                if (nodeConnection is not IStateConnection<T> stateConnection)
                {
                    Debug.LogWarning($"[{nameof(StateMachine<T>)}:{nameof(SetTrigger)}] NodeConnection does not implements {nameof(IStateConnection<T>)} on {Name}");
                    continue;
                }

                if (!stateConnection.Evaluate(ParameterValues)) continue;
                INode toNode = stateConnection.GetOut(CurrentState);
                if (toNode is not IState<T> toState)
                {
                    Debug.LogWarning($"[{nameof(StateMachine<T>)}:{nameof(CheckTriggerForState)}] toNode does not implements {nameof(IState<T>)} on {Name}");
                    continue;
                }
                availableTransitions.Add((toState, stateConnection));
            }

            if (availableTransitions.Count == 0) return false;
            if (availableTransitions.Count > 1)
            {
                Debug.LogWarning($"[{nameof(StateMachine<T>)}:{nameof(CheckTriggerForState)}] Multiple transitions can be taken from State {state.Name} with current params, total {availableTransitions.Count}.");
            }
            availableTransitions.Sort((x, y) => x.Item2.Priority.CompareTo(y.Item2.Priority)); //Sort by priority ascending
            Transit(CurrentState, availableTransitions[0].Item1, availableTransitions[0].Item2); //IStateConnection with priority takes precedence
            availableTransitions[0].Item2.ConsumeTriggers(ParameterValues);
            return true;
        }
        
        internal static void ValidateParam(T name, IStateMachine<T> rootStateMachine, ParameterType expectedDefinition, out ParameterState<T> parameterState)
        {
            if (!rootStateMachine.ParameterValues.TryGetValue(name, out parameterState)) 
                throw new InvalidOperationException($"{name} parameter does not exists in {rootStateMachine.Name} {nameof(StateMachine<T>)}");
            if(parameterState.Type != expectedDefinition) 
                throw new InvalidOperationException($"You are trying to set a value to a different type, {name} is type {parameterState.Type} in {rootStateMachine.Name} {nameof(StateMachine<T>)}");
        }
    }
}