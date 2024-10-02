using System;
using System.Collections.Generic;
using LegendaryTools.GraphV2;
using UnityEngine;

namespace LegendaryTools.StateMachineV2
{
    public interface IStateMachine : IGraph
    {
        string Name { get; set; }
        IState AnyState { get; }
        bool IsRunning { get; }
        
        Dictionary<string, float> ParameterValues { get; }
        Dictionary<string, StateParameter> ParameterDefinitions { get; }

        void Start(IState startState);
        void Stop();
        void Update();
        
        void AddParameter(string parameterName, StateParameter stateParameter);
        bool RemoveParameter(string parameterName, StateParameter stateParameter);
        
        void SetTrigger(string name);
        void SetBool(string name, bool value);
        void SetInt(string name, int value);
        void SetFloat(string name, float value);
    }

    public interface IState : INode
    {
        public string Name { get; set; }
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;

        internal void InvokeOnStateEnter();
        internal void InvokeOnStateUpdate();
        internal void InvokeOnStateExit();
    }
    
    public interface IStateConnection : INodeConnection
    {
        Dictionary<string, Condition> Conditions { get; }
        string Name { get; set; }
        event Action OnTransit;
        void AddCondition(string name, FloatParameterCondition parameterCondition, float value);
        void AddCondition(string name, IntParameterCondition parameterCondition, int value);
        void AddCondition(string name, BoolParameterCondition parameterCondition);
        void AddCondition(string name);
        void RemoveCondition(string name);
        bool Evaluate(Dictionary<string, float> parametersState);
        void OnTransited();
        internal void InvokeOnTransit();
    }

    public enum StateParameter
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
    
    public abstract class Condition
    {
        public string Name;
        public StateParameter Type { get; protected set; }

        public abstract bool Evaluate(string name, float value);
    }
    
    public class FloatCondition : Condition
    {
        public FloatParameterCondition Condition;
        public float Value;

        public FloatCondition(string name)
        {
            Name = name;
            Type = StateParameter.Float;
        }

        public FloatCondition(string name, FloatParameterCondition condition, float value) : this(name)
        {
            Condition = condition;
            Value = value;
        }

        public override bool Evaluate(string name, float value)
        {
            if (Name != name) return false;
            return Condition switch
            {
                FloatParameterCondition.Greater => value > Value,
                FloatParameterCondition.Less => value < Value,
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
            Type = StateParameter.Int;
        }
        
        public IntCondition(string name, IntParameterCondition condition, int value) : this(name)
        {
            Condition = condition;
            Value = value;
        }
        
        public override bool Evaluate(string name, float value)
        {
            if (Name != name) return false;
            return Condition switch
            {
                IntParameterCondition.Equals =>  Convert.ToInt32(value) == Value,
                IntParameterCondition.NotEquals => Convert.ToInt32(value) != Value,
                IntParameterCondition.Greater => value > Value,
                IntParameterCondition.Less => value < Value,
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
            Type = StateParameter.Bool;
        }
        
        public BoolCondition(string name, BoolParameterCondition condition) : this(name)
        {
            Condition = condition;
        }
        
        public override bool Evaluate(string name, float value)
        {
            if (Name != name) return false;
            return Condition switch
            {
                BoolParameterCondition.True => Convert.ToBoolean(value),
                BoolParameterCondition.False => !Convert.ToBoolean(value),
                _ => false
            };
        }
    }
    
    public class TriggerCondition : Condition
    {
        public TriggerCondition(string name)
        {
            Name = name;
            Type = StateParameter.Trigger;
        }

        public override bool Evaluate(string name, float value)
        {
            return Name == name;
        }
    }

    public class StateConnection : NodeConnection, IStateConnection
    {
        public string Name { get; set; }
        public event Action OnTransit;
        public Dictionary<string, Condition> Conditions { get; protected set; } = new Dictionary<string, Condition>();
        
        public StateConnection(INode fromNode, INode toNode, NodeConnectionDirection direction) : base(fromNode, toNode, direction)
        {
        }

        public void AddCondition(string name, FloatParameterCondition parameterCondition, float value)
        {
            ValidateParam(name, StateParameter.Float);
            Conditions.Add(name, new FloatCondition(name, parameterCondition, value));
        }
        
        public void AddCondition(string name, IntParameterCondition parameterCondition, int value)
        {
            ValidateParam(name, StateParameter.Int);
            Conditions.Add(name, new IntCondition(name, parameterCondition, value));
        }

        public void AddCondition(string name, BoolParameterCondition parameterCondition)
        {
            ValidateParam(name, StateParameter.Bool);
            Conditions.Add(name, new BoolCondition(name, parameterCondition));
        }
        
        public void AddCondition(string name)
        {
            ValidateParam(name, StateParameter.Trigger);
            Conditions.Add(name, new TriggerCondition(name));
        }

        private void ValidateParam(string name, StateParameter expectedDefinition)
        {
            IGraph rootGraph = FromNode.Owner.GraphHierarchy.Length == 0 ? FromNode.Owner : FromNode.Owner.GraphHierarchy[0];
            if(rootGraph is not IStateMachine rootStateMachine) 
                throw new InvalidOperationException($"Root {nameof(StateMachine)} does not implements {nameof(IStateMachine)}.");
            StateMachine.ValidateParam(name, rootStateMachine, expectedDefinition, out float currentValue);
            if(Conditions.ContainsKey(name))
                throw new InvalidOperationException($"{nameof(StateConnection)} already has {name} parameter.");
        }

        public void RemoveCondition(string name)
        {
            Conditions.Remove(name);
        }

        public bool Evaluate(Dictionary<string, float> parametersState)
        {
            foreach (KeyValuePair<string, Condition> pair in Conditions)
            {
                if (!parametersState.TryGetValue(pair.Key, out float currentValue))
                {
                    throw new InvalidOperationException($"You are trying to test a condition called {pair.Key} that has no parameter in the {nameof(StateMachine)}.");
                }
                
                if (!pair.Value.Evaluate(pair.Key, currentValue)) return false;
            }
            
            return true;
        }

        public virtual void OnTransited()
        {

        }
        
        void IStateConnection.InvokeOnTransit()
        {
            OnTransited();
            OnTransit?.Invoke();
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
            return new StateConnection(fromNode, toNode, direction);
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

        public Dictionary<string, float> ParameterValues { get; protected set; } = new Dictionary<string, float>();
        public Dictionary<string, StateParameter> ParameterDefinitions { get; protected set; } = new Dictionary<string, StateParameter>();
        
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
        
        public void AddParameter(string parameterName, StateParameter stateParameter)
        {
            if (ParameterValues.ContainsKey(parameterName) || ParameterDefinitions.ContainsKey(parameterName))
            {
                throw new InvalidOperationException($"{Name} {nameof(StateMachine)} already has {parameterName} parameter");
            }
            
            ParameterValues.Add(parameterName, 0);
            ParameterDefinitions.Add(parameterName, stateParameter);
        }

        public bool RemoveParameter(string parameterName, StateParameter stateParameter)
        {
            if (!ParameterValues.ContainsKey(parameterName) || !ParameterDefinitions.ContainsKey(parameterName))
            {
                return false;
            }
            
            ParameterValues.Remove(parameterName);
            ParameterDefinitions.Remove(parameterName);
            return true;
        }

        public void SetTrigger(string name)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, StateParameter.Trigger, out float currentValue);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetBool(string name, bool value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, StateParameter.Bool, out float currentValue);
            ParameterValues[name] = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetInt(string name, int value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, StateParameter.Int, out float currentValue);
            ParameterValues[name] = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }

        public void SetFloat(string name, float value)
        {
            if (!IsRunning) throw new InvalidOperationException($"{Name} {nameof(StateMachine)} is not running.");
            ValidateParam(name, this, StateParameter.Float, out float currentValue);
            ParameterValues[name] = Convert.ToSingle(value);
            if (!CheckTriggerForState(CurrentState))
                CheckTriggerForState(AnyState);
        }
        
        private bool CheckTriggerForState(IState state)
        {
            foreach (INodeConnection nodeConnection in state.OutboundConnections)
            {
                if (nodeConnection is not IStateConnection stateConnection)
                {
                    Debug.LogWarning($"[{nameof(StateMachine)}:{nameof(SetTrigger)}] NodeConnection does not implements {nameof(IStateConnection)} on {Name}");
                    continue;
                }

                if (!stateConnection.Evaluate(ParameterValues)) continue;
                if (stateConnection.ToNode is not IState toState)
                {
                    Debug.LogWarning($"[{nameof(StateMachine)}:{nameof(CheckTriggerForState)}] StateConnection.ToNode does not implements {nameof(IState)} on {Name}");
                    continue;
                }
                Transit(CurrentState, stateConnection.GetOut(CurrentState) as IState, stateConnection);
                return true;
            }

            return false;
        }
        
        internal static void ValidateParam(string name, IStateMachine rootStateMachine, StateParameter expectedDefinition, out float currentValue)
        {
            if (!rootStateMachine.ParameterValues.TryGetValue(name, out currentValue)) 
                throw new InvalidOperationException($"{name} parameter does not exists in {rootStateMachine.Name} {nameof(StateMachine)}");
            if (!rootStateMachine.ParameterDefinitions.TryGetValue(name, out StateParameter definition)) 
                throw new InvalidOperationException($"{name} parameter does not exists in {rootStateMachine.Name} {nameof(StateMachine)}");
            if(definition != expectedDefinition) 
                throw new InvalidOperationException($"You are trying to set a value to a different type, {name} is type {definition} in {rootStateMachine.Name} {nameof(StateMachine)}");
        }
    }
}