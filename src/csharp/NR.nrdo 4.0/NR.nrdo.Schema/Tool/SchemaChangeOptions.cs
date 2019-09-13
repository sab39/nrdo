using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Tool
{
    public struct SchemaChangeOptions
    {
        public bool PromptForPossibleDataLoss { get; set; }
        public DropBehavior TableDropBehavior { get; set; }
        public DropBehavior FieldDropBehavior { get; set; }
        public bool PreserveUnknownObjects { get; set; }

        private TimeSpan? schemaUpdateLockTimeout;
        public TimeSpan SchemaUpdateLockTimeout
        {
            get { return schemaUpdateLockTimeout ?? TimeSpan.FromSeconds(15); }
            set { schemaUpdateLockTimeout = value; }
        }

        public static SchemaChangeOptions Default
        {
            get
            {
                return new SchemaChangeOptions
                {
                    PromptForPossibleDataLoss = false,
                    TableDropBehavior = DropBehavior.Drop,
                    FieldDropBehavior = DropBehavior.Drop,
                    PreserveUnknownObjects = false,
                    SchemaUpdateLockTimeout = TimeSpan.FromSeconds(15),
                };
            }
        }
    }
}
