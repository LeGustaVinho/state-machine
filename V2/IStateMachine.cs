using System;

namespace LegendaryTools.StateMachineV2
{
    public interface IStateMachine<T>
    {
        string Name { get; set; }
        IState CurrentState { get; }
        bool IsRunning { get; }
        
        void Start(IState startState);
        void Stop();
        void Update();
        void SetTrigger(T trigger);
    }
}