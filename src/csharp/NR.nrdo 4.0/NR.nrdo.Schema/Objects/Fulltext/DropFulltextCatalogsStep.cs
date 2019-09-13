using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fulltext
{
    public sealed class DropFulltextCatalogsStep : StepBase
    {
        public override string Identifier { get { return "dropping-fulltext-catalogs"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropFulltextIndexesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in FulltextCatalogType.AllFrom(changes.Current))
            {
                if (changes.Desired.Contains(current)) continue;

                changes.RemoveWithoutTransaction(changes.SchemaDriver.GetDropFulltextCatalogSql(current.Name), current);
            }
        }
    }
}
