using System;

namespace LegendaryTools.StateMachineV2
{
    public class HardState<T> : IHardState<T> where T : struct, Enum, IConvertible
    {
        public string Name => Type.ToString();
        
        public T Type { get; }
        
        public event Action OnStateEnter;
        public event Action OnStateUpdate;
        public event Action OnStateExit;
        
        void IState.InvokeOnStateEnter()
        {
            OnStateEnter?.Invoke();
        }

        void IState.InvokeOnStateUpdate()
        {
            OnStateUpdate?.Invoke();
        }

        void IState.InvokeOnStateExit()
        {
            OnStateExit?.Invoke();
        }

        public HardState(T type)
        {
            Type = type;
        }
    }
}