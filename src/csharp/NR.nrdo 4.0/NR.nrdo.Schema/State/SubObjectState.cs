using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;

namespace NR.nrdo.Schema.State
{
    public abstract class SubObjectState : ObjectState
    {
        protected readonly Identifier identifier;
        public Identifier Identifier { get { return identifier; } }

        public string Name { get { return Identifier.Name; } }
        public override ObjectType ObjectType { get { return Identifier.ObjectType; } }

        protected readonly Identifier parentIdentifier;
        public Identifier ParentIdentifier { get { return parentIdentifier; } }

        public string ParentName { get { return ParentIdentifier.Name; } }
        public ObjectType ParentType { get { return ParentIdentifier.ObjectType; } }

        protected SubObjectState(Identifier parentIdentifier, Identifier identifier)
        {
            if (identifier.ObjectType.IsRoot) throw new ArgumentException(identifier + " is not a sub object type");
            if (!parentIdentifier.ObjectType.IsRoot) throw new ArgumentException(parentIdentifier + " is not a root object type and cannot be a parent to " + identifier);

            this.parentIdentifier = parentIdentifier;
            this.identifier = identifier;
        }

        public abstract SubObjectState WithName(string newName);
        public abstract SubObjectState WithParent(Identifier newParent);

        public override string ToString()
        {
            return Identifier + " in " + ParentIdentifier;
        }

        // This is needed so that the "real" SubObjectState<TType, TState> can override the un-generic WithName but also provide
        // a generic-typed version
        public abstract class Internal<TType, TState> : SubObjectState
            where TType : ObjectType<TType, TState>, new()
        {
            internal Internal(Identifier parentIdentifier, Identifier identifier)
                : base(parentIdentifier, identifier) { }

            public sealed override SubObjectState WithName(string newName)
            {
                return withNameInternal(newName);
            }
            protected abstract SubObjectState<TType, TState> withNameInternal(string newName);

            public sealed override SubObjectState WithParent(Identifier newParent)
            {
                return withParentInternal(newParent);
            }
            protected abstract SubObjectState<TType, TState> withParentInternal(Identifier newParent);
        }
    }

    public class SubObjectState<TType, TState> : SubObjectState.Internal<TType, TState>
        where TType : ObjectType<TType, TState>, new()
    {
        private readonly TState state;
        public TState State { get { return state; } }

        public new TType ObjectType { get { return (TType)base.ObjectType; } }

        internal SubObjectState(Identifier parentIdentifier, Identifier identifier, TState state)
            : base(parentIdentifier, identifier)
        {
            this.state = state;
        }

        protected override SubObjectState<TType, TState> withNameInternal(string newName)
        {
            var newIdentifier = identifier.WithName(newName);
            if (object.ReferenceEquals(identifier, newIdentifier)) return this;
            return new SubObjectState<TType, TState>(parentIdentifier, newIdentifier, state);
        }

        public new SubObjectState<TType, TState> WithName(string newName)
        {
            return withNameInternal(newName);
        }

        protected override SubObjectState<TType, TState> withParentInternal(Identifier newParent)
        {
            if (object.ReferenceEquals(parentIdentifier, newParent)) return this;
            parentIdentifier.ObjectType.CheckType(newParent);
            return new SubObjectState<TType, TState>(newParent, identifier, state);
        }

        public new SubObjectState<TType, TState> WithParent(Identifier newParent)
        {
            return withParentInternal(newParent);
        }

        public SubObjectState<TType, TState> With(TState newState)
        {
            if (object.ReferenceEquals(state, newState)) return this;
            return new SubObjectState<TType, TState>(parentIdentifier, identifier, newState);
        }

        public SubObjectState<TType, TState> With(Func<TState, TState> change)
        {
            return With(change(state));
        }
    }
}
