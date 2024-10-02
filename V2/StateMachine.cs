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
    
    public interface IStateMachine : IGraph
    {
        string Name { get; set; }
        IState AnyState { get; }
        IState CurrentState { get; }
        bool IsRunning { get; }
        
        Dictionary<string, ParameterState> ParameterValues { get; }

        void Start(IState startState);
        void Stop();
        void Update();
        
        void AddParameter(string parameterName, ParameterType parameterType);
        bool RemoveParameter(string parameterName, ParameterType parameterType);
        
        void SetTrigger(string name);
        void SetBool(string name, bool value);
        void SetInt(string name, int value);
        void SetFloat(string name, float value);
    }

    public interface IState : INode
    {
        public string Name { get; set; }

        IStateConnection ConnectTo(INode to, int priority, NodeConnectionDirection newDirection, 
            ConditionOperation conditionOperation = ConditionOperation.WhenAll);
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;

        internal void InvokeOnStateEnter();
        internal void InvokeOnStateUpdate();
        internal void InvokeOnStateExit();
    }
    
    public interface IStateConnection : INodeConnection, IComparable<IStateConnection>
    {
        List<Condition> Conditions { get; }
        ConditionOperation ConditionOperation { get; internal set; }
        string Name { get; set; }
        int Priority { get; internal set; }
        event Action OnTransit;
        void AddCondition(string name, FloatParameterCondition parameterCondition, float value);
        void AddCondition(string name, IntParameterCondition parameterCondition, int value);
        void AddCondition(string name, BoolParameterCondition parameterCondition);
        void AddCondition(string name);
        void RemoveCondition(Predicate<Condition> predicate);
        bool Evaluate(Dictionary<string, ParameterState> parametersState);
        void ConsumeTriggers(Dictionary<string, ParameterState> parametersState);
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

    public class ParameterState
    {
        public string Name;
        public ParameterType Type;
        public float Value;

        public ParameterState(string name, ParameterType type, float value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }
    
    public abstract class Condition
    {
        public string Name;
        public ParameterType Type { get; protected set; }

        public abstract bool Evaluate(string name, ParameterState parameterState);
    }
    
    public class FloatCondition : Condition
    {
        public FloatParameterCondition Condition;
        public float Value;

        public FloatCondition(string name)
        {
            Name = name;
            Type = ParameterType.Float;
        }

        public FloatCondition(string name, FloatParameterCondition condition, float value) : this(name)
        {
            Condition = condition;
            Value = value;
        }

        public override bool Evaluate(string name, ParameterState parameterState)
        {
            if (Name != name) return false;
            return Condition switch
            {
                FloatParameterCondition.Greater => parameterState.Value > Value,
                FloatParameterCondition.Less => parameterState.Value < Value,
                _ => false
            };
        }
    }
    
    public class IntCondition : Condition
    {
        public IntParameterCondition Condition;
        public int Value;
        
        public IntCondition(string name)
        {
            Name = name;
            Type = ParameterType.Int;
        }
        
        public IntCondition(string name, IntParameterCondition condition, int value) : this(name)
        {
            Condition = condition;
            Value = value;
        }
        
        public override bool Evaluate(string name, ParameterState parameterState)
        {
            if (Name != name) return false;
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
    
    public class BoolCondition : Condition
    {
        public BoolParameterCondition Condition;
        
        public BoolCondition(string name)
        {
            Name = name;
            Type = ParameterType.Bool;
        }
        
        public BoolCondition(string name, BoolParameterCondition condition) : this(name)
        {
            Condition = condition;
        }
        
        public override bool Evaluate(string name, ParameterState parameterState)
        {
            if (Name != name) return false;
            return Condition switch
            {
                BoolParameterCondition.True => Convert.ToBoolean(parameterState.Value),
                BoolParameterCondition.False => !Convert.ToBoolean(parameterState.Value),
                _ => false
            };
        }
    }
    
    public class TriggerCondition : Condition
    {
        public TriggerCondition(string name)
        {
            Name = name;
            Type = ParameterType.Trigger;
        }

        public override bool Evaluate(string name, ParameterState parameterState)
        {
            return Name == name && Convert.ToBoolean(parameterState.Value);
        }
    }

    public class StateConnection : NodeConnection, IStateConnection
    {
        public string Name { get; set; }

        private int priority;
        int IStateConnection.Priority
        {
            get => priority;
            set => priority = value;
        }
        
        private ConditionOperation conditionOperation;
        ConditionOperation IStateConnection.ConditionOperation
        {
            get => conditionOperation;
            set => conditionOperation = value;
        }

        public event Action OnTransit;
        public List<Condition> Conditions { get; protected set; } = new List<Condition>();

        public StateConnection(INode fromNode, INode toNode, int priority, NodeConnectionDirection direction,
            ConditionOperation conditionOperation = ConditionOperation.WhenAll) : base(fromNode, toNode, direction)
        {
            this.priority = priority;
            this.conditionOperation = conditionOperation;
        }

        public void AddCondition(string name, FloatParameterCondition parameterCondition, float value)
        {
            ValidateParam(name, ParameterType.Float);
            Conditions.Add(new FloatCondition(name, parameterCondition, value));
        }
        
        public void AddCondition(string name, IntParameterCondition parameterCondition, int value)
        {
            ValidateParam(name, ParameterType.Int);
            Conditions.Add(new IntCondition(name, parameterCondition, value));
        }

        public void AddCondition(string name, BoolParameterCondition parameterCondition)
        {
            ValidateParam(name, ParameterType.Bool);
            Conditions.Add(new BoolCondition(name, parameterCondition));
        }
        
        public void AddCondition(string name)
        {
            ValidateParam(name, ParameterType.Trigger);
            Conditions.Add(new TriggerCondition(name));
        }

        private void ValidateParam(string name, ParameterType expectedDefinition)
        {
            IGraph rootGraph = FromNode.Owner.GraphHierarchy.Length == 0 ? FromNode.Owner : FromNode.Owner.GraphHierarchy[0];
            if(rootGraph is not IStateMachine rootStateMachine) 
                throw new InvalidOperationException($"Root {nameof(StateMachine)} does not implements {nameof(IStateMachine)}.");
            StateMachine.ValidateParam(name, rootStateMachine, expectedDefinition, out ParameterState parameterState);
        }

        public void RemoveCondition(Predicate<Condition> predicate)
        {
            Conditions.RemoveAll(predicate);
        }

        public bool Evaluate(Dictionary<string, ParameterState> parametersState)
        {
            switch (conditionOperation)
            {
                case ConditionOperation.WhenAll:
                {
                    foreach (Condition condition in Conditions)
                    {
                        if (!parametersState.TryGetValue(condition.Name, out ParameterState cParameterState))
                        {
                            throw new InvalidOperationException($"You are trying to test a condition called {condition.Name} that has no parameter in the {nameof(StateMachine)}.");
                        }
                        if (!condition.Evaluate(condition.Name, cParameterState)) return false;
                    }
                    return true;
                }
                case ConditionOperation.WhenAny:
                {
                    foreach (Condition condition in Conditions)
                    {
                        if (!parametersState.TryGetValue(condition.Name, out ParameterState cParameterState))
                        {
                            throw new InvalidOperationException($"You are trying to test a condition called {condition.Name} that has no parameter in the {nameof(StateMachine)}.");
                        }
                        if (condition.Evaluate(condition.Name, cParameterState)) return true;
                    }
                    return false;
                }
            }

            return false;
        }

        public void ConsumeTriggers(Dictionary<string, ParameterState> parametersState)
        {
            foreach (Condition condition in Conditions)
            {
                if (condition.Type == ParameterType.Trigger) parametersState[condition.Name].Value = 0;
            }
        }

        public virtual void OnTransited()
        {

        }
        
        void IStateConnection.InvokeOnTransit()
        {
            OnTransited();
            OnTransit?.Invoke();
        }

        public int CompareTo(IStateConnection other)
        {
            return priority.CompareTo(other.Priority);
        }
    }
    
    public class State : Node, IState
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
            return new StateConnection(fromNode, toNode, 0, direction);
        }

        public override INodeConnection ConnectTo(INode to, NodeConnectionDirection newDirection)
        {
            throw new InvalidOperationException($"Call you should call signature {nameof(ConnectTo)}(INode to, int priority, NodeConnectionDirection newDirection, ConditionOperation conditionOperation = ConditionOperation.WhenAll) instead.");
        }

        public virtual IStateConnection ConnectTo(INode to, int priority, NodeConnectionDirection newDirection, 
            ConditionOperation conditionOperation = ConditionOperation.WhenAll)
        {
            INodeConnection nodeConnection = base.ConnectTo(to, newDirection);
            if (nodeConnection is not IStateConnection stateConnection) 
                throw new InvalidOperationException($"nodeConnection does not implement {nameof(IStateConnection)}. Did you forget to override method {nameof(ConstructConnection)} ?");

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
        
        void IState.InvokeOnStateEnter()
        {
            OnStateEntered();
            OnStateEnter?.Invoke();
        }

        void IState.InvokeOnStateUpdate()
        {
            OnStateUpdated();
            OnStateUpdate?.Invoke();
        }

        void IState.InvokeOnStateExit()
        {
            OnStateExited();
            OnStateExit?.Invoke();
        }
    }
    
    public class StateMachine : GraphV2.Graph, IStateMachine
    {
        public string Name { get; set; }
        public IState AnyState { get; }
        public bool IsRunning => CurrentState != null;
        public IState CurrentState { get; protected set; }

        public Dictionary<string, ParameterState> ParameterValues { get; protected set; } = new Dictionary<string, ParameterState>();
        
        public StateMachine(IState anyState, string name = "")
        {
            Name = name;
            AnyState = anyState;
        }
        
        public void Start(IState startState)
        {
            if (IsRunning) return;
            if (!Contains(startState)) throw new InvalidOperationException($"{nameof(startState)} must be a state inside of {Name} {nameof(StateMachine)}");
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

        private void Transit(IState fromState, IState toState, IStateConnection transition)
        {
            fromState?.InvokeOnStateExit();
            transition?.InvokeOnTransit();
            CurrentState = toState;
            toState?.InvokeOnStateEnter();
        }
        
        public void AddParameter(string parameterName, ParameterType parameterType)
        {
            if (ParameterValues.ContainsKey(parameterName))
            {
                throw new InvalidOperationException($"{Name} {nameof(StateMachine)} already has {parameterName} parameter");
            }
            
            ParameterValues.Add(parameterName, new ParameterState(parameterName, parameterType, 0));
        }

        public bool RemoveParameter(string parameterName, ParameterType parameterType)
        {
            if (!ParameterValues.ContainsKey(parameterName))
            {
                return false;
            }
            
            ParameterValues.Remove(parameterName);
            return true;
        }

        public void SetTrigger(string name)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, ParameterType.Trigger, out ParameterState parameterState);
            ParameterValues[name].Value = Convert.ToSingle(true);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetBool(string name, bool value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, ParameterType.Bool, out ParameterState parameterState);
            ParameterValues[name].Value = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetInt(string name, int value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, ParameterType.Int, out ParameterState parameterState);
            ParameterValues[name].Value = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetFloat(string name, float value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, ParameterType.Float, out ParameterState parameterState);
            ParameterValues[name].Value = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }
        
        private bool CheckTriggerForState(IState state)
        {
            List<(IState, IStateConnection)> availableTransitions = new List<(IState, IStateConnection)>();
            foreach (INodeConnection nodeConnection in state.OutboundConnections)
            {
                if (nodeConnection is not IStateConnection stateConnection)
                {
                    Debug.LogWarning($"[{nameof(StateMachine)}:{nameof(SetTrigger)}] NodeConnection does not implements {nameof(IStateConnection)} on {Name}");
                    continue;
                }

                if (!stateConnection.Evaluate(ParameterValues)) continue;
                INode toNode = stateConnection.GetOut(CurrentState);
                if (toNode is not IState toState)
                {
                    Debug.LogWarning($"[{nameof(StateMachine)}:{nameof(CheckTriggerForState)}] toNode does not implements {nameof(IState)} on {Name}");
                    continue;
                }
                availableTransitions.Add((toState, stateConnection));
            }

            if (availableTransitions.Count == 0) return false;
            if (availableTransitions.Count > 1)
            {
                Debug.LogWarning($"[{nameof(StateMachine)}:{nameof(CheckTriggerForState)}] Multiple transitions can be taken from State {state.Name} with current params, total {availableTransitions.Count}.");
            }
            availableTransitions.Sort((x, y) => x.Item2.Priority.CompareTo(y.Item2.Priority)); //Sort by priority ascending
            Transit(CurrentState, availableTransitions[0].Item1, availableTransitions[0].Item2); //IStateConnection with priority takes precedence
            availableTransitions[0].Item2.ConsumeTriggers(ParameterValues);
            return true;
        }
        
        internal static void ValidateParam(string name, IStateMachine rootStateMachine, ParameterType expectedDefinition, out ParameterState parameterState)
        {
            if (!rootStateMachine.ParameterValues.TryGetValue(name, out parameterState)) 
                throw new InvalidOperationException($"{name} parameter does not exists in {rootStateMachine.Name} {nameof(StateMachine)}");
            if(parameterState.Type != expectedDefinition) 
                throw new InvalidOperationException($"You are trying to set a value to a different type, {name} is type {parameterState.Type} in {rootStateMachine.Name} {nameof(StateMachine)}");
        }
    }
}