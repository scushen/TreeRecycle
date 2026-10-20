using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
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

        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["dbConnection"].ConnectionString;
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
