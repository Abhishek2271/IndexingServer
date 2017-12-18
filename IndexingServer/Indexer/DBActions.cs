using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace IndexingServer
{
    /// <summary>
    /// Handle all database related "actions" here.
    /// </summary>
    public static class DBActions
    {
        public static string ControlDB { get; set; }
        public static string DatabaseServerName { get; set; }
        public static string username { get; set; }
        public static string password { get; set; }
        public static string connectionString;

        /// <summary>
        /// Initilize class
        /// </summary>
        /// <param name="_ControlDb">User database</param>
        /// <param name="_DatabaseServerName">Database servername</param>
        /// <param name="_userName">Server Username</param>
        /// <param name="_password">Server Password</param>
        //public DBActions(string _ControlDb, string _DatabaseServerName, string _userName, string _password)
        //{
        //    ControlDB = _ControlDb;
        //    DatabaseServerName = _DatabaseServerName;
        //    username = _userName;
        //    password = _password;
        //    connectionString = GetConnectionString(false);
        //}

        /// <summary>
        /// Create a connection string
        /// </summary>
        /// <param name="windowsauth">windows auth. bool</param>
        /// <returns></returns>
        public static string GetConnectionString(bool windowsauth)
        {
            if (windowsauth)
            {
                return "Data Source=" + DatabaseServerName + "; " +
                       "Initial Catalog=" + ControlDB + "; " +
                       "MultipleActiveResultSets = true;" +
                       "Integrated Security=True";
            }
            else
            {
                return
                    "Data Source=" + DatabaseServerName + "; " +
                    "Initial Catalog=" + ControlDB + "; " +
                    "User ID=" + username + ";" +
                    "MultipleActiveResultSets = true;" +
                    "Password=" + password;
            }
        }

        /// <summary>
        /// Check if db is reachable
        /// </summary>
        /// <returns>true if reachable else false</returns>
        public static bool CheckDBConnection()
        {
            try
            {
                return false;
                connectionString = GetConnectionString(false);
                //SqlConnection conn = GetSqlConnection();
                //conn.Open();
            }
            catch
            {
                Console.WriteLine("Could not connect to database. Please make sure that the control database and the authentications are correct...");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get an open sql connection 
        /// </summary>
        /// <returns>sql connection</returns>
        public static SqlConnection GetSqlConnection()
        {
            SqlConnection conn_local = new SqlConnection(connectionString);
            conn_local.Open();
            return conn_local;
        }

        /// <summary>
        /// SQL Execute query
        /// </summary>
        /// <param name="command">sql command</param>
        /// <returns>number of rows affected by query</returns>
        public static int ExecuteNonQuery(SqlCommand command)
        {
            SqlConnection conn = null;
            try
            {
                command.Connection = conn = GetSqlConnection();
                command.CommandTimeout = 120 * 1000;
                int o = command.ExecuteNonQuery();
                command.Dispose();
                return o;
            }
            catch (Exception e)
            {
                return -1;
            }
            finally
            {
                if (conn != null)
                    conn.Dispose();
            }
        }

        /// <summary>
        /// Get object from query
        /// </summary>
        /// <param name="query">input query</param>
        /// <returns>result object</returns>
        public static object ExecuteScalar(string query)
        {
            SqlCommand command = new SqlCommand();
            SqlConnection conn = null;
            try
            {
                command.Connection = conn = GetSqlConnection();
                command.CommandText = query;
                command.CommandTimeout = 120 * 1000;
                object o = command.ExecuteScalar();
                command.Dispose();
                return o;
            }
            catch (Exception e)
            {
                return -1;
            }
            finally
            {
                if (conn != null)
                    conn.Dispose();
            }
        }

        /// <summary>
        /// Keep logs in DB
        /// </summary>
        /// <param name="lgtype">log type</param>
        /// <param name="loginfo">Some information about error</param>
        /// <param name="logdetail">Details of log</param>
        public static void Log(string lgtype, string loginfo, string logdetail)
        {
            string query = @"insert into TBL_EX_INDEXLOG([logtype],[loginfo],[logdetail],[logdatetime]) 
                                select    @logtype, 
                                          @loginfo, @logdetail, getdate() ";
            SqlCommand command = new SqlCommand();
            command.CommandText = query;

            command.Parameters.Add("@logtype", System.Data.SqlDbType.NVarChar, 200).Value = lgtype.ToString();
            command.Parameters.Add("@loginfo", System.Data.SqlDbType.NVarChar).Value = loginfo;
            command.Parameters.Add("@logdetail", System.Data.SqlDbType.NVarChar).Value = logdetail;
            try
            {
                ExecuteNonQuery(command);
            }
            catch (Exception e)
            {

            }
        }
    }

}

           

  


