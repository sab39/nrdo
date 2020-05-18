using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public class CacheBaselines
    {
        private readonly CacheBaseline baseline;
        public CacheBaseline Baseline { get { return baseline; } }

        private readonly CacheBaseline cycleBaseline;
        public CacheBaseline CycleBaseline { get { return cycleBaseline; } }

        internal CacheBaselines(CacheBaseline baseline, CacheBaseline cycleBaseline)
        {
            this.baseline = baseline;
            this.cycleBaseline = cycleBaseline;
        }
    }
}
