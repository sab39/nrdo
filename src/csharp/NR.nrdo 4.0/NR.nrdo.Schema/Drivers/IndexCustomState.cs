using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers
{
    /// <summary>
    /// Subclasses of IndexCustomState represent extra information about indexes specific to one database vendor.
    /// </summary>
    public abstract class IndexCustomState
    {
        /// <summary>
        /// A summary of what kind of custom state is represented here - used in error messages
        /// </summary>
        public abstract string CustomStateType { get; }
    }
}
