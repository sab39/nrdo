using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects.Internal
{
    public sealed class BeforeStatementType : SubObjectType<BeforeStatementType, BeforeStatementType.State>
    {
        public override string Name { get { return "before-statement"; } }

        public override IEnumerable<SubObjectState<BeforeStatementType, State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return GetBasedOnKnownIdentifiers(connection, helper, (parent, name) => CreateAsCompleted(parent, name));
        }

        public override IEnumerable<StepBase> Steps
        {
            get { yield break; } // No specific steps for Before statements
        }

        public sealed class State
        {
            private readonly string parentSortKey;
            public string ParentSortKey { get { return parentSortKey; } }

            private readonly int sortPositionWithinParent;
            public int SortPositionWithinParent { get { return sortPositionWithinParent; } }

            private readonly bool isRunOnInitialCreate;
            public bool IsRunOnInitialCreate { get { return isRunOnInitialCreate; } }

            private readonly bool isRunOnUpgrade;
            public bool IsRunOnUpgrade { get { return isRunOnUpgrade; } }

            private readonly string step;
            public string Step { get { return step; } }

            private readonly string sql;
            public string Sql { get { return sql; } }

            internal State(string parentSortKey, int sortPositionWithinParent, string step, bool isRunOnInitialCreate, bool isRunOnUpgrade, string sql)
            {
                if (!isRunOnInitialCreate && !isRunOnUpgrade) throw new ArgumentException("Before statement is never run, what's the point of that?");

                this.parentSortKey = parentSortKey;
                this.sortPositionWithinParent = sortPositionWithinParent;
                this.step = step;
                this.isRunOnInitialCreate = isRunOnInitialCreate;
                this.isRunOnUpgrade = isRunOnUpgrade;
                this.sql = sql;
            }

            public override string ToString()
            {
                // Since ToString()'s for debugging we don't include all the sql, it'd be spammy
                return "before " + (!isRunOnInitialCreate ? "upgrade " : "") + (!isRunOnUpgrade ? "initial " : "") + step;
            }
        }

        public static SubObjectState<BeforeStatementType, State> Create(Identifier parent, string parentSortKey, int sortPosition, string name, string step,
            bool isRunOnInitialCreate, bool isRunOnUpgrade, string sql)
        {
            return CreateState(parent, name, new State(parentSortKey, sortPosition, step, isRunOnInitialCreate, isRunOnUpgrade, sql));
        }

        public static SubObjectState<BeforeStatementType, State> CreateAsCompleted(Identifier parent, string name)
        {
            return CreateState(parent, name, null);
        }
    }
}
