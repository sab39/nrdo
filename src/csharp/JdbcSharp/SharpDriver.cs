using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using java.sql;
using java.util;

namespace JdbcSharp
{
    public class SharpDriver : Driver
    {
        static SharpDriver()
        {
            DriverManager.registerDriver(new SharpDriver());
        }

        // Accepts urls of the form jdbc:sharp:(providerinvariantname):(connectionstring)
        // eg jdbc:sharp:System.Data.SqlClient:server=whatever;uid=whatever;pw=whatever
        public bool acceptsURL(string url)
        {
            return url.StartsWith("jdbc:sharp:");
        }

        public Connection connect(string url, Properties props)
        {
            try
            {
                return new SharpConnection(url);
            }
            catch (DbException e)
            {
                throw new SQLException(e.Message).initCause(e);
            }
        }

        public int getMajorVersion()
        {
            return 0;
        }

        public int getMinorVersion()
        {
            return 0;
        }

        public DriverPropertyInfo[] getPropertyInfo(string __p1, java.util.Properties __p2)
        {
            throw new NotImplementedException();
        }

        public bool jdbcCompliant()
        {
            // probably a safe bet ;)
            return false;
        }

        public java.util.logging.Logger getParentLogger()
        {
            return null;
        }
    }
}
