using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Sequences
{
    public sealed class SequenceType : RootObjectType<SequenceType, Stateless>
    {
        public override string Name { get { return "sequence"; } }

        public override IEnumerable<RootObjectState<SequenceType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from seq in connection.GetAllSequences()
                   select Create(seq.QualifiedName);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropSequencesStep();
                yield return new AddSequencesStep();
            }
        }

        public static RootObjectState<SequenceType, Stateless> Create(string name)
        {
            return CreateState(name, Stateless.Value);
        }
    }
}
