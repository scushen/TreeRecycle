using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Framework.Config;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// Starts Castle ActiveRecord against the "dbConnection" connection string.
    /// Same start-up as KennelBooker's BusinessObjectsFactory, minus the SQLite test plumbing.
    /// </summary>
    public class BusinessObjectsFactory
    {
        public void InitializeActiveRecord()
        {
            if (ActiveRecordMediator.GetSessionFactoryHolder() == null)
            {
                Assembly asm = typeof(BusinessObjectsFactory).Assembly;
                ActiveRecordStarter.Initialize(asm, GetConfiguration());
            }
        }

        /// <summary>
        /// The "dbConnection" string, taken from the first of these that has a value:
        /// 1. Azure App Service connection string named dbConnection (exposed as SQLAZURECONNSTR_ / SQLCONNSTR_ / CUSTOMCONNSTR_ env vars);
        /// 2. Web.config's dbConnection entry, if it isn't the empty placeholder;
        /// 3. connectionStrings.config next to Web.config - the git-ignored local file.
        /// Web.config can't use configSource for this, because a configSource file that's missing (as on Azure) stops the site starting.
        /// </summary>
        public static string GetConnectionString()
        {
            foreach (string prefix in new[] { "SQLAZURECONNSTR_", "SQLCONNSTR_", "CUSTOMCONNSTR_" })
            {
                string fromAzure = Environment.GetEnvironmentVariable(prefix + "dbConnection");
                if (!string.IsNullOrWhiteSpace(fromAzure))
                    return fromAzure;
            }

            ConnectionStringSettings fromConfig = ConfigurationManager.ConnectionStrings["dbConnection"];
            if (fromConfig != null && !string.IsNullOrWhiteSpace(fromConfig.ConnectionString))
                return fromConfig.ConnectionString;

            string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connectionStrings.config");
            if (File.Exists(localFile))
            {
                XElement entry = XDocument.Load(localFile).Descendants("add")
                    .FirstOrDefault(e => (string)e.Attribute("name") == "dbConnection");
                if (entry != null && !string.IsNullOrWhiteSpace((string)entry.Attribute("connectionString")))
                    return (string)entry.Attribute("connectionString");
            }

            throw new ConfigurationErrorsException("No dbConnection connection string. Set it in the Azure App Service configuration, "
                                                   + "or create connectionStrings.config from connectionStrings.example.config.");
        }

        private static IConfigurationSource GetConfiguration()
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("connection.driver_class", "NHibernate.Driver.SqlClientDriver");
            properties.Add("dialect", "NHibernate.Dialect.MsSql2008Dialect");
            properties.Add("connection.provider", "NHibernate.Connection.DriverConnectionProvider");
            properties.Add("connection.connection_string", GetConnectionString());
            properties.Add("proxyfactory.factory_class", "NHibernate.ByteCode.Castle.ProxyFactoryFactory, NHibernate.ByteCode.Castle");
            properties.Add("show_sql", "false");

            InPlaceConfigurationSource config = new InPlaceConfigurationSource();
            config.Add(typeof(ActiveRecordBase), properties);
            return config;
        }
    }
}
