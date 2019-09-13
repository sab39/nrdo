using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Drivers.Introspection;
using NR.nrdo.Schema.Shared;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Queries
{
    public sealed class QueryType : RootObjectType<QueryType, QueryType.ProcState>
    {
        // Query objects' type doesn't vary based on whether they ultimately end up as a stored proc, stored function or not stored at all.
        // This is because, as top-level objects, they may have sub objects that need to stay with them even if the qu file is changed from one type to another.
        // Even queries that end up not having a db component at all still need to be tracked because they can have associated before-statements.
        public override string Name { get { return "query"; } }

        public override IEnumerable<RootObjectState<QueryType, ProcState>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            var existingNames = new HashSet<string>(connection.DbDriver.DbStringComparer);
            foreach (var proc in connection.GetAllStoredProcsAndFunctions())
            {
                existingNames.Add(proc.QualifiedName);
                var func = proc as IntrospectedFunction;
                if (func != null)
                {
                    yield return CreateFunction(func.QualifiedName, func.Parameters, func.ReturnType, func.Body);
                }
                else
                {
                    yield return CreateProc(proc.QualifiedName, proc.Parameters, proc.Body);
                }
            }

            // Queries that do not exist as stored procs or functions just get stored as known objects in the state tables.
            foreach (var known in GetBasedOnKnownIdentifiers(connection, helper, CreateUnstored))
            {
                if (!existingNames.Contains(known.Name)) yield return known;
            }
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new PreUpgradeHooksStep();
                yield return new DropStoredProcsStep();
                yield return new AddStoredProcsStep();
            }
        }

        public class ProcState
        {
            private readonly ReadOnlyCollection<ProcParam> parameters;
            public IEnumerable<ProcParam> Parameters { get { return parameters; } }

            private readonly string body;
            public string Body { get { return body; } }

            internal ProcState(IEnumerable<ProcParam> parameters, string body)
            {
                if (body == null) throw new ArgumentNullException("body");

                this.parameters = parameters.ToList().AsReadOnly();
                this.body = body;
            }

            public virtual ProcState WithBody(string newBody)
            {
                if (newBody == body) return this;
                return new ProcState(parameters, newBody);
            }

            public override string ToString()
            {
                // Since ToString()'s for debugging we don't include all the sql, it'd be spammy
                return "procedure(" + string.Join(", ", parameters) + ")";
            }
        }

        public sealed class FunctionState : ProcState
        {
            private readonly string returnType;
            public string ReturnType { get { return returnType; } }

            internal FunctionState(IEnumerable<ProcParam> parameters, string returnType, string body)
                : base(parameters, body)
            {
                this.returnType = returnType;
            }

            public override ProcState WithBody(string newBody)
            {
                if (newBody == Body) return this;
                return new FunctionState(Parameters, returnType, newBody);
            }

            public override string ToString()
            {
                // Since ToString()'s for debugging we don't include all the sql, it'd be spammy
                return "function (" + string.Join(", ", Parameters) + ") returning " + returnType;
            }
        }

        public static bool IsTypeEqual(ProcState a, ProcState b)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            return (a is FunctionState) == (b is FunctionState);
        }

        public static bool IsSignatureEqual(ProcState a, ProcState b, DbDriver dbDriver)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;

            var aFunc = a as FunctionState;
            var bFunc = b as FunctionState;
            if (aFunc != null && bFunc != null)
            {
                // If they're both functions, the return types need to match.
                if (!dbDriver.StringEquals(aFunc.ReturnType, bFunc.ReturnType)) return false;
            }
            else if (aFunc != null || bFunc != null)
            {
                // If one of them is a function but NOT both, they aren't equal.
                return false;
            }

            return Enumerable.SequenceEqual(a.Parameters, b.Parameters, ProcParam.GetComparer(dbDriver));
        }

        private static string normalizeNewlines(string s)
        {
            return s.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
        }

        public static bool IsEqual(ProcState a, ProcState b, DbDriver dbDriver)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;

            // We do an exact, case-sensitive comparison of the body rather than using the database's usually case-insensitive
            // version because it's actual code, which means it may contain literal strings whose case matters
            // (not to mention that the programmer may care about case for stylistic reasons, or in comments)
            return IsSignatureEqual(a, b, dbDriver) && normalizeNewlines(a.Body.Trim()) == normalizeNewlines(b.Body.Trim());
        }

        public static RootObjectState<QueryType, ProcState> CreateProc(string name, IEnumerable<ProcParam> parameters, string body)
        {
            return CreateState(name, new ProcState(parameters, body));
        }

        public static RootObjectState<QueryType, ProcState> CreateFunction(string name, IEnumerable<ProcParam> parameters, string returnType, string body)
        {
            return CreateState(name, new FunctionState(parameters, returnType, body));
        }

        public static RootObjectState<QueryType, ProcState> CreateUnstored(string name)
        {
            return CreateState(name, null);
        }
    }
}
