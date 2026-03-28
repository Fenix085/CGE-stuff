#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MainEngine.FSM
{
    public abstract class FSMState<TContext>
    {
        private readonly List<FSMTransition<TContext>> _transitions = [];
        public IReadOnlyList<FSMTransition<TContext>> Transitions => _transitions;

        public void AddTransition(FSMTransition<TContext> transition) => _transitions.Add(transition);

        public virtual void OnEnter(TContext context) { }
        public virtual void OnUpdate(TContext context, GameTime gameTime) { }
        public virtual void OnExit(TContext context) { }
    }

    public sealed class FSMTransition<TContext>
    {
        private readonly Func<TContext, bool> _condition;
        private readonly Action<TContext>? _onTransition;

        public FSMState<TContext> NextState { get; }

        public FSMTransition(
            FSMState<TContext> nextState,
            Func<TContext, bool> condition,
            Action<TContext>? onTransition = null)
        {
            NextState = nextState;
            _condition = condition;
            _onTransition = onTransition;
        }

        public bool IsValid(TContext context) => _condition(context);
        public void OnTransition(TContext context) => _onTransition?.Invoke(context);
    }

    public sealed class FiniteStateMachine<TContext>
    {
        public FSMState<TContext>? ActiveState { get; private set; }

        public void SetInitialState(FSMState<TContext> state, TContext context)
        {
            ActiveState = state;
            ActiveState.OnEnter(context);
        }

        public void Update(TContext context, GameTime gameTime)
        {
            if (ActiveState is null) return;

            foreach (var transition in ActiveState.Transitions)
            {
                if (!transition.IsValid(context)) continue;

                ActiveState.OnExit(context);
                transition.OnTransition(context);
                ActiveState = transition.NextState;
                ActiveState.OnEnter(context);
                break;
            }

            ActiveState.OnUpdate(context, gameTime);
        }
    }
}