using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects.Fulltext
{
    public sealed class FulltextCatalogType : RootObjectType<FulltextCatalogType, Stateless>
    {
        public override string Name { get { return "fulltext-catalog"; } }

        public override IEnumerable<RootObjectState<FulltextCatalogType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from name in connection.GetAllFulltextCatalogs() select Create(name);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropFulltextCatalogsStep();
                yield return new AddFulltextCatalogsStep();
            }
        }

        public static RootObjectState<FulltextCatalogType, Stateless> Create(string name)
        {
            return CreateState(name, Stateless.Value);
        }
    }
}
