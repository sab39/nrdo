using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Sql
{
    public static class SqlDataModification
    {
        /// <summary>
        /// Not for use with nrdo generated tables.
        /// </summary>
        /// <param name="tableName"></param>
        public static void NotifyTableModified(string tableName)
        {
            lock (Nrdo.LockObj)
            {
                NamedTable(tableName).fireModified();
            }
        }

        public class TableModification
        {
            public long ModificationCount { get; private set; }
            public event Action Modified;

            internal void fireModified()
            {
                ModificationCount++;

                var modified = Modified;
                if (modified != null) modified();
            }
        }

        private static Dictionary<string, TableModification> modifications = new Dictionary<string, TableModification>(StringComparer.OrdinalIgnoreCase);

        public static TableModification NamedTable(string tableName)
        {
            lock (Nrdo.LockObj)
            {
                if (!modifications.ContainsKey(tableName)) modifications[tableName] = new TableModification();
                return modifications[tableName];
            }
        }
    }
}
