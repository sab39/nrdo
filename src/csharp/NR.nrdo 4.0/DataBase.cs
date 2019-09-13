using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Linq;
using NR.nrdo.Connection;

namespace NR.nrdo
{
    public class DataBase
    {
        private readonly string name;
        private readonly DbDriver dbDriver;
        private readonly string connectionString;

        public string Name { get { return name; } }
        public DbDriver DbDriver { get { return dbDriver; } }
        public string ConnectionString { get { return connectionString; } }

        private DataBase(string name, DbDriver dbDriver, string connectionString)
        {
            this.name = name;
            this.dbDriver = dbDriver;
            this.connectionString = connectionString;
        }

        private static Func<Func<DbDriver, string, DataBase>, DataBase> initDefault;
        private static Func<IEnumerable<string>> initEnumerate;
        private static Func<string, Func<DbDriver, string, DataBase>, DataBase> initNamed;

        public static void SetInitialization(Func<Func<DbDriver, string, DataBase>, DataBase> getDefault,
                                             Func<IEnumerable<string>> enumerateNames,
                                             Func<string, Func<DbDriver, string, DataBase>, DataBase> getNamed)
        {
            lock (Nrdo.LockObj)
            {
                if (defaultDb.IsValueCreated || namedDbs.Count > 0) throw new ApplicationException("Database already initialized");

                initDefault = getDefault;
                initEnumerate = enumerateNames;
                initNamed = getNamed;
            }
        }

        private static string getConnectionStringFromConfig(string name)
        {
            var setting = ConfigurationManager.ConnectionStrings[name];
            return setting == null ? null : setting.ConnectionString;
        }

        private static IEnumerable<string> getAllConnectionStringNamesFromConfig()
        {
            foreach (ConnectionStringSettings c in ConfigurationManager.ConnectionStrings)
            {
                if (c.Name.ToLower() != "connectionstring") yield return c.Name;
            }
        }

        static DataBase()
        {
            SetInitializationFromConfig(SqlServerDriver.Instance);
        }

        public static void SetInitializationFromConfig(DbDriver driver)
        {
            SetInitialization(init => init(driver, getConnectionStringFromConfig("ConnectionString") ?? ConfigurationManager.AppSettings["ConnectionString"]),
                              getAllConnectionStringNamesFromConfig,
                              (name, init) => init(driver, getConnectionStringFromConfig(name)));
        }

        private static Dictionary<string, DataBase> namedDbs = new Dictionary<string, DataBase>();

        private static readonly Lazy<DataBase> defaultDb = new Lazy<DataBase>(delegate
        {
            lock (Nrdo.LockObj)
            {
                return initDefault((dbDriver, connStr) => new DataBase(null, dbDriver, connStr));
            }
        });

        public static DataBase Default { get { return defaultDb.Value; } }

        public static DataBase Get(string name)
        {
            if (name == null) return Default;

            lock (Nrdo.LockObj)
            {
                if (initNamed == null) return null;

                if (!namedDbs.ContainsKey(name))
                {
                    namedDbs[name] = initNamed(name, (dbDriver, connStr) => new DataBase(name, dbDriver, connStr));
                }
                return namedDbs[name];
            }
        }

        public static IEnumerable<DataBase> AllNamed
        {
            get
            {
                lock (Nrdo.LockObj)
                {
                    if (initEnumerate != null)
                    {
                        foreach (var name in initEnumerate())
                        {
                            Get(name);
                        }
                    }
                    return namedDbs.Values.ToList();
                }
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as DataBase;
            return other != null && object.Equals(dbDriver, other.dbDriver) && connectionString == other.connectionString;
        }

        public override int GetHashCode()
        {
            return dbDriver.GetHashCode() ^ connectionString.GetHashCode();
        }
    }
}
