using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Objects.Views
{
    public sealed class ViewType : RootObjectType<ViewType, ViewType.State>
    {
        public override string Name { get { return "view"; } }

        public override IEnumerable<RootObjectState<ViewType, State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from view in connection.GetAllViews() select Create(view.QualifiedName, view.Body);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropViewsStep();
                yield return new AddViewsStep();
            }
        }

        public class State
        {
            internal State(string body)
            {
                this.body = body;
            }
            private readonly string body;
            public string Body { get { return body; } }
        }

        public static RootObjectState<ViewType, State> Create(string name, string body)
        {
            return CreateState(name, new State(body));
        }

        private static string normalizeNewlines(string s)
        {
            return s.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
        }

        public static bool IsEqual(State a, State b, DbDriver dbDriver)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;

            // We do an exact, case-sensitive comparison of the body rather than using the database's usually case-insensitive
            // version because it's actual code, which means it may contain literal strings whose case matters
            // (not to mention that the programmer may care about case for stylistic reasons, or in comments)
            return normalizeNewlines(a.Body.Trim()) == normalizeNewlines(b.Body.Trim());
        }
    }
}
