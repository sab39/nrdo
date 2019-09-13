using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects
{
    public abstract class StepBase
    {
        public abstract string Identifier { get; }
        public virtual bool MustHappenBefore(StepBase other) { return false; }
        public virtual bool MustHappenAfter(StepBase other) { return false; }
        public abstract void Perform(SchemaChanges changes, IOutput output);

        public override bool Equals(object obj)
        {
            var other = obj as StepBase;
            if (other == null) return false;

            var resultByName = other.Identifier == this.Identifier;
            var resultByType = other.GetType() == this.GetType();
            if (resultByName != resultByType) throw new ApplicationException("Possible duplicated identifier on step");

            return resultByName;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}
