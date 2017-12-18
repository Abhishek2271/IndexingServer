using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndexingServer;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
               
                string ControlDB = string.Empty;
                string DatabaseServerName = string.Empty;
                string username = string.Empty;
                string password = string.Empty;
                //Read from ini file for user inputs
                IndexingConfig.ReadIndexINIFile(out ControlDB, out DatabaseServerName, out username, out password);
                Console.WriteLine("\nini read complete...");
                //Only use db for logs if user specifies the db settings otherwise bypass db all togather
                if (!string.IsNullOrEmpty(ControlDB) &&
                    !string.IsNullOrEmpty(DatabaseServerName) &&
                    !string.IsNullOrEmpty(username) &&
                    !string.IsNullOrEmpty(password))
                {
                    DBActions.ControlDB = ControlDB;
                    DBActions.DatabaseServerName = DatabaseServerName;
                    DBActions.username = username;
                    DBActions.password = password;
                    if (!DBActions.CheckDBConnection())
                    {
                        Console.WriteLine("\nCould not connect to Db. Continuing without db, logs and events will be displayed in console...");
                    }
                    
                }
                
                IndexingConfig.StartThreads();
                Console.WriteLine("\nIndexing threads started...");
               
                Console.ReadKey();
            }
            catch (Exception Exp)
            {
                Console.WriteLine("Error: " + Exp.Message);
                Console.ReadKey();
            }
        }
    }
}
