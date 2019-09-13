using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util;
using NR.nrdo.Util.OutputUtil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Objects.Views
{
    public sealed class AddViewsStep : StepBase
    {
        public override string Identifier { get { return "adding-views"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropViewsStep || other is AddFkeysStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropFieldsStep || other is AddStoredProcsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            var allDesired = ViewType.AllFrom(changes.Desired)
                                     .DependencySort((first, second) => second.State.Body.IndexOf(first.Name.Substring(first.Name.LastIndexOf('.') + 1), StringComparison.OrdinalIgnoreCase) >= 0)
                                     .ToImmutableList();

            foreach (var desired in allDesired)
            {
                var current = changes.Current.Get(desired);
                if (current != null && ViewType.IsEqual(current.State, desired.State, changes.DbDriver)) continue;

                var body = desired.State.Body;
                var createSql = changes.SchemaDriver.GetCreateViewSql(desired.Name, body);

                changes.Put(createSql, desired);
            }
        }
    }
}