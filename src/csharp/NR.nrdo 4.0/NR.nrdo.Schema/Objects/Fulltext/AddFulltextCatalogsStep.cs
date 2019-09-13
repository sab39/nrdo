using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fulltext
{
    public sealed class AddFulltextCatalogsStep : StepBase
    {
        public override string Identifier { get { return "adding-fulltext-catalogs"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropFulltextCatalogsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddFulltextIndexesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in FulltextCatalogType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                // No particular need to track whether this has been done, it can be done every time
                // In theory it's also silly to do it for every catalog but in practice there won't actually be more than one catalog
                var enableFulltext = changes.SchemaDriver.GetEnableFulltextSql(changes.Connection);
                if (enableFulltext != null)
                {
                    changes.PutWithoutTransaction(enableFulltext);
                }

                changes.PutWithoutTransaction(changes.SchemaDriver.GetCreateFulltextCatalogSql(desired.Name), desired);
            }
        }
    }
}
