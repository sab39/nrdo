using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;

namespace NR.nrdo.Schema.State
{
    public abstract class RootObjectState : ObjectState
    {
        protected readonly Identifier identifier;
        public Identifier Identifier { get { return identifier; } }

        public string Name { get { return Identifier.Name; } }
        public override ObjectType ObjectType { get { return Identifier.ObjectType; } }

        protected RootObjectState(Identifier identifier)
        {
            this.identifier = identifier;
        }

        public abstract RootObjectState WithName(string newName);
        public abstract RootObjectState WithIdentifier(Identifier newIdentifier);

        // This is needed so that the "real" RootObjectState<TType, TState> can override the un-generic WithName but also provide
        // a generic-typed version
        public abstract class Internal<TType, TState> : RootObjectState
            where TType : RootObjectType<TType, TState>, new()
        {
            internal Internal(Identifier identifier)
                : base(identifier)
            {
                RootObjectType<TType, TState>.CheckType(identifier);
            }

            public sealed override RootObjectState WithName(string newName)
            {
                return withIdentifierInternal(identifier.WithName(newName));
            }
            public sealed override RootObjectState WithIdentifier(Identifier newIdentifier)
            {
                return withIdentifierInternal(newIdentifier);
            }
            protected abstract RootObjectState<TType, TState> withIdentifierInternal(Identifier newIdentifier);
        }

        public override string ToString()
        {
            return Identifier.ToString();
        }
    }

    public class RootObjectState<TType, TState> : RootObjectState.Internal<TType, TState>
        where TType : RootObjectType<TType, TState>, new()
    {
        private readonly TState state;
        public TState State { get { return state; } }

        public new TType ObjectType { get { return (TType)base.ObjectType; } }

        internal RootObjectState(Identifier identifier, TState state)
            : base(identifier)
        {
            this.state = state;
        }

        protected override RootObjectState<TType, TState> withIdentifierInternal(Identifier newIdentifier)
        {
            if (object.ReferenceEquals(identifier, newIdentifier)) return this;
            return new RootObjectState<TType, TState>(newIdentifier, state);
        }

        public new RootObjectState<TType, TState> WithName(string newName)
        {
            return withIdentifierInternal(Identifier.WithName(newName));
        }

        public new RootObjectState<TType, TState> WithIdentifier(Identifier newIdentifier)
        {
            return withIdentifierInternal(newIdentifier);
        }

        public RootObjectState<TType, TState> With(TState newState)
        {
            if (object.ReferenceEquals(state, newState)) return this;
            return new RootObjectState<TType, TState>(identifier, state);
        }

        public RootObjectState<TType, TState> With(Func<TState, TState> change)
        {
            return With(change(state));
        }
    }
}
