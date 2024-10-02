using System;

namespace LegendaryTools.StateMachineV2
{
    public interface IState
    {
        public string Name { get; }
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;

        internal void InvokeOnStateEnter();
        internal void InvokeOnStateUpdate();
        internal void InvokeOnStateExit();
    }
}