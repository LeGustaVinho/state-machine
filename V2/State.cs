using System;
using LegendaryTools.GraphV2;

namespace LegendaryTools.StateMachineV2
{
    public class State<T> : Node, IAdvancedState<T> where T : IEquatable<T>
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
            return new AdvancedStateConnection<T>(fromNode, toNode, 0, direction);
        }

        public override INodeConnection ConnectTo(INode to, NodeConnectionDirection newDirection)
        {
            throw new InvalidOperationException($"Call you should call signature {nameof(ConnectTo)}(INode to, int priority, NodeConnectionDirection newDirection, ConditionOperation conditionOperation = ConditionOperation.WhenAll) instead.");
        }

        public virtual IAdvancedStateConnection<T> ConnectTo(INode to, int priority, NodeConnectionDirection newDirection, 
            ConditionOperation conditionOperation = ConditionOperation.WhenAll)
        {
            INodeConnection nodeConnection = base.ConnectTo(to, newDirection);
            if (nodeConnection is not IAdvancedStateConnection<T> stateConnection) 
                throw new InvalidOperationException($"nodeConnection does not implement {nameof(IAdvancedStateConnection<T>)}. Did you forget to override method {nameof(ConstructConnection)} ?");

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
}