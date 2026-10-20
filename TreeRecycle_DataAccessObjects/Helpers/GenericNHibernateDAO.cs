using System;
using System.Collections.Generic;
using System.Text;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord;
using System.Data;
using NHibernate.Criterion;
using NHibernate;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// generic Class that holds all public methods for the Active record Base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public abstract class GenericNHibernateDAO<T> : ActiveRecordBase
    {
        #region Public Methods



        /// <summary>
        /// returns one record
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T FindByPrimaryKey(object value)
        {
            return (T)FindByPrimaryKey(typeof(T), value);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="throwOnNotFound"></param>
        /// <returns></returns>
        public static T FindByPrimaryKey(object value, bool throwOnNotFound)
        {
            return (T)FindByPrimaryKey(typeof(T), value, throwOnNotFound);
        }


        /// <summary>
        /// returns all rows in a table
        /// </summary>
        /// <returns>collection of LocationsDAO</returns>
        public static IList<T> FindAll()
        {
            return (IList<T>)FindAll(typeof(T));
        }

        /// <summary>
        /// returns all rows in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static IList<T> FindAll(ICriterion criteria)
        {
            return (IList<T>)FindAll(typeof(T), criteria);
        }

        public static IList<T> FindAll(DetachedCriteria criteria)
        {
            return (IList<T>)FindAll(typeof(T), criteria);
        }


        /// <summary>
        /// returns all rows in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static IList<T> FindAll(ICriterion[] criterias)
        {
            return (IList<T>)FindAll(typeof(T), criterias);
        }

        /// <summary>
        /// returns all rows in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <param name="orders">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static IList<T> FindAll(ICriterion criteria, Order order)
        {
            return (IList<T>)FindAll(typeof(T), new Order[] { order }, new ICriterion[] { criteria });
        }

        /// <summary>
        /// returns all rows in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <param name="orders">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static IList<T> FindAll(ICriterion[] criterias, Order[] orders)
        {
            return (IList<T>)FindAll(typeof(T), orders, criterias);
        }

        /// <summary>
        /// returns one row in a table matching a criteria
        /// </summary>
        /// <param name="start">The start record</param>
        /// <param name="max">The max records</param>
        /// <param name="criteria">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static IList<T> FindTop(int start, int max, ICriterion[] criteria)
        {
            return (IList<T>)SlicedFindAll(typeof(T), start, max, criteria);
        }

        public static IList<T> FindTop(int max, ICriterion[] criteria)
        {
            return FindTop(0, max, criteria);
        }


        //public static IList<T> FindTop(int max, ICriterion[] criteria)
        //{
        //    return FindTop(0, max, criteria);
        //}

        /// <summary>
        /// Finds the top number of records
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="max">The max.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="order">The order.</param>
        /// <returns></returns>
        public static IList<T> FindTop(int start, int max, ICriterion criteria, Order order)
        {
            return (IList<T>)SlicedFindAll(typeof(T), start, max, new Order[] { order }, criteria);
        }

        public static IList<T> FindTop(int max, ICriterion criteria, Order order)
        {
            return FindTop(0, max, criteria, order);
        }

        /// <summary>
        /// Finds the top number of records
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="max">The max.</param>
        /// <param name="criterias">The criteria.</param>
        /// <param name="order">The order.</param>
        /// <returns></returns>
        public static IList<T> FindTop(int start, int max, ICriterion[] criterias, Order order)
        {
            return (IList<T>)SlicedFindAll(typeof(T), start, max, new Order[] { order }, criterias);
        }

        public static IList<T> FindTop(int max, ICriterion[] criterias, Order order)
        {
            return FindTop(0, max, criterias, order);
        }

        /// <summary>
        /// Finds the top number of records.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="max">The max.</param>
        /// <param name="criterias">The criteria.</param>
        /// <param name="orders">The orders.</param>
        /// <returns></returns>
        public static IList<T> FindTop(int start, int max, ICriterion[] criterias, Order[] orders)
        {
            return (IList<T>)SlicedFindAll(typeof(T), start, max, orders, criterias);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="max"></param>
        /// <param name="criterias"></param>
        /// <param name="orders"></param>
        /// <returns></returns>
        public static IList<T> FindTop(int max, ICriterion[] criterias, Order[] orders)
        {
            return FindTop(0, max, criterias, orders);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public static T FindOne(DetachedCriteria criteria)
        {
            return (T)FindOne(typeof(T), criteria);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="detachedQuery"></param>
        /// <returns></returns>
        public static T FindOne(IDetachedQuery detachedQuery)
        {
            return (T)FindOne(typeof(T), detachedQuery);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public static T FindOne(params ICriterion[] criteria)
        {
            return (T)FindOne(typeof(T), criteria);
        }


        /// <summary>
        /// returns one row in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static T FindFirst(ICriterion criteria)
        {
            return (T)FindFirst(typeof(T), criteria);
        }

        /// <summary>
        /// returns one row in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static T FindFirst(ICriterion[] criterias)
        {
            return (T)FindFirst(typeof(T), criterias);
        }

        /// <summary>
        /// returns one row in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <param name="orders">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static T FindFirst(ICriterion criteria, Order order)
        {
            return (T)FindFirst(typeof(T), new Order[] { order }, new ICriterion[] { criteria });
        }

        /// <summary>
        /// returns one row in a table matching a criteria 
        /// </summary>
        /// <param name="criteria">Criteria to be followed</param>
        /// <param name="orders">Criteria to be followed</param>
        /// <returns>Rows of type passed</returns>
        public static T FindFirst(ICriterion[] criterias, Order[] orders)
        {
            return (T)FindFirst(typeof(T), orders, criterias);
        }

        // returns all members matching the criteria
        /// <summary>
        /// returns all members matching the criteria
        /// </summary>
        /// <param name="property">string value of property</param>
        /// <param name="value">value passed</param>
        /// <returns>Rows of type passed</returns>
        public static IList<T> FindAllByProperty(string property, object value)
        {
            return (IList<T>)FindAllByProperty(typeof(T), property, value);
        }

                               
        public static IList<T> FindAllByProperty(string orderByColumn, string property, object value)
        {
            return (IList<T>)FindAllByProperty(typeof(T), property, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int Count()
        {
            return Count(typeof(T));
        }

        public static int Count(DetachedCriteria detachedCriteria)
        {
            return Count(typeof(T), detachedCriteria);
        }

        public static int Count(ICriterion[] criterias)
        {
            return Count(typeof(T), criterias);
        }

        public static int Count(string filter, params object[] args)
        {
            return Count(typeof(T), filter, args);
        }


        public static bool Exists()
        {
            return Exists(typeof(T));
        }

        public static bool Exists(DetachedCriteria detachedCriteria)
        {
            return Exists(typeof(T), detachedCriteria);
        }

        public static bool Exists(IDetachedQuery detachedQuery)
        {
            return Exists(typeof(T), detachedQuery);
        }

        public static bool Exists(params ICriterion[] criteria)
        {
            return Exists(typeof(T), criteria);
        }

        public static new void Refresh(object instance)
        {
            Refresh(instance);
        }


        protected internal static Array SlicedFindAll(int firstResult, int maxResults, DetachedCriteria criteria)
        { 
            return SlicedFindAll(typeof(T), firstResult, maxResults, criteria);
        }

        public static Array SlicedFindAll(int firstResult, int maxResults, IDetachedQuery detachedQuery)
        {
            return SlicedFindAll(typeof(T), firstResult, maxResults, detachedQuery);
        }

        public static Array SlicedFindAll(int firstResult, int maxResults, params ICriterion[] criteria)
        {
            return SlicedFindAll(typeof(T), firstResult, maxResults, criteria);
        }

        public static Array SlicedFindAll(int firstResult, int maxResults, Order[] orders, DetachedCriteria criteria)
        {
            return SlicedFindAll(typeof(T), firstResult, maxResults, orders, criteria);
        }

        public static Array SlicedFindAll(int firstResult, int maxResults, Order[] orders, params ICriterion[] criteria)
        {
            return SlicedFindAll(typeof(T), firstResult, maxResults, orders, criteria);
        }
       


        // 
        /// <summary>
        /// returns number of records based on the Stored Procedure call
        /// </summary>
        /// <param name="targetType">Type of object passed</param>
        /// <param name="dml">procedure name</param>
        /// <returns>row numbers</returns>
        protected internal static int ExecuteNonQuery(Type targetType, string dml)
        {
            ISessionFactoryHolder holder = ActiveRecordMediator.GetSessionFactoryHolder();
            NHibernate.ISession session = holder.CreateSession(targetType);
            int rowsAffected = -1;
            IDbConnection conn = null;
            IDbCommand cmd = null;

            try
            {
                conn = session.Connection;
                cmd = conn.CreateCommand();
                cmd.CommandText = dml;
                cmd.CommandType = CommandType.Text;
                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new ActiveRecordException("Could not perform ExecuteNonQuery for "
                  + targetType.Name, ex);
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                holder.ReleaseSession(session);
            }

            return rowsAffected;
        }

        #endregion //Public Functions

    }
}
