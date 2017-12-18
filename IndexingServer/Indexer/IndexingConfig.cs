using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;
using System.Collections;
using Indexer;

namespace IndexingServer
{
    /// <Author>Abhishek shrestha</Author>
    /// <Summary> Set configuration for indexing process and begin the threads.</Summary>
    
    public static class IndexingConfig
    {
        //Is the share location initialized
        public static bool Initialized;
        public static string indexlocation = null;        
       
        //Arbitarely set mergefactor and buffereddocs. These are not usable unless index is really huge.
        public static int mergeFactor = 2000;
        public static int maxBufferedDocs = 200;

        // Send mail after indexing is complete
        public static string mailFrom = string.Empty; 
        public static string mailSubject = string.Empty; 
        public static string mailHost = string.Empty; 
        public static int mailPort = 0; 

        public static Thread[] IndexingThreads;
        //Size of RAM index to hold in memory.
        public static long SizeOfRamIndex = 50 * 1024 * 1024;   
        //If more than one thread is use, probable this can be useful to divide resources equally.
        public static long SizeOfRamIndexPerThread = 50 * 1024 * 1024;
        //Number of threads to handle the actual indexing process.
        public static int NumberOfIndexingThreads = 1;


        public static RamIndexHandler handler = new RamIndexHandler();
        //Base folder for indexing
        public static string sourceDirectoryLocation = string.Empty;
             
        public static Thread ramIndexHandler;

        /// <summary>
        /// Start the indexing process in a separate thread.
        /// </summary>
        public static void StartThreads()
        {
            IndexingClass ixClass = new IndexingClass();
            //For now use only a single thread. TODO: handle multiple simultaneous threads.
            NumberOfIndexingThreads = 1;
            IndexingThreads = new Thread[NumberOfIndexingThreads];
            for (int i = 0; i < NumberOfIndexingThreads; i++)
            {               
                IndexingThreads[i] = new Thread(new ThreadStart(ixClass.MainIndexProcess));
                IndexingThreads[i].IsBackground = true;
                IndexingThreads[i].Name = i.ToString();
                IndexingThreads[i].Start();
            }
            SizeOfRamIndex = SizeOfRamIndex * 1024 * 1024;
            SizeOfRamIndexPerThread = SizeOfRamIndex / NumberOfIndexingThreads;            
            //User another thread which is triggered by previous thread. This can be helpful while indexing large number of files. One thread writes to mem and other thread mem to drive.
            RamIndexHandler.Clear();
            ramIndexHandler = new Thread(new ThreadStart(handler.ManageRamIndexes));
            ramIndexHandler.Name = "RamIndexHandler";
            ramIndexHandler.Start();
        }

        /// <summary>
        /// Read ini file from default location.
        /// </summary>
        /// <param name="dbName">Database name where logs should be written</param>
        /// <param name="dbServer">Database server</param>
        /// <param name="userName">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public static bool ReadIndexINIFile(out string dbName, out string dbServer, out string userName, out string password)
        {
            try
            {
                //Read indexing Settings.
                string inifileLocation = System.Windows.Forms.Application.StartupPath + @"\IndexingSettings.ini";
                CustomUtility.IniFileParser parser = new CustomUtility.IniFileParser(inifileLocation);
                NumberOfIndexingThreads = int.Parse(parser.GetSetting("INDEXING_SETTING", "totalThreads"));
                //SizeOfRamIndex = int.Parse(parser.GetSetting("INDEXING_SETTING", "SizeOfRamIndex"));
                //maxBufferedDocs = int.Parse(parser.GetSetting("INDEXING_SETTING", "maxBufferedDocs"));
                //mergeFactor = int.Parse(parser.GetSetting("INDEXING_SETTING", "mergeFactor"));
                sourceDirectoryLocation = parser.GetSetting("INDEXING_SETTING", "sourceLocation");
                indexlocation = parser.GetSetting("INDEXING_SETTING", "indexLocation");
                //Read DB Settings if exists
                dbName = parser.GetSetting("DB_SETTINGS", "Control_DB");
                dbServer = parser.GetSetting("DB_SETTINGS", "DatabaseServerName");
                userName = parser.GetSetting("DB_SETTINGS", "username");
                password = parser.GetSetting("DB_SETTINGS", "password");
                return true;
            }
            catch (Exception exp)
            {
                throw new Exception("Error while parsing the ini file. Please make sure that the file is present and is properly configured.\n" + exp.Message);
            }
        }
    }
}
