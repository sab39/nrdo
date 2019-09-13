using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Objects.Views
{
    public sealed class DropViewsStep : StepBase
    {
        public override string Identifier { get { return "dropping-views"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropViewsStep || other is DropStoredProcsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddViewsStep || other is DropFkeysStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in ViewType.AllFrom(changes.Current))
            {
                var desired = changes.Desired.Get(current);
                if (desired != null && ViewType.IsEqual(current.State, desired.State, changes.DbDriver)) continue;

                changes.Remove(changes.SchemaDriver.GetDropViewSql(current.Name), current);
            }
        }
    }
}
