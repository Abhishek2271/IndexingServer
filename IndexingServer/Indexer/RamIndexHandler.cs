using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Data;
using System.Data.SqlClient;
using Lucene.Net.Analysis.Standard;
using IndexingServer;

namespace Indexer
{
    /// <summary>
    /// Create RAM indexes and write to disk.
    /// </summary>
    public class RamIndexHandler
    {
        //The RAM indexes to be written to disk
        private static ArrayList RamIndexes = new ArrayList();
        //Create a wait handle to make sure threads dont create race conditions and conflicts.
        private static EventWaitHandle wh = new AutoResetEvent(false);
        
        
        public static long total_ramdir_size = 0;

        public RamIndexHandler()
        {
            //IndexingStatus = new DataTable();
            //IndexingStatus.Columns.Add("fileid", typeof(Int64));
            //IndexingStatus.Columns.Add("indexstatuslog", typeof(bool));
        }

        public static void AddRamIndex(RAMIndexManager manager)
        {
            lock (RamIndexes)
            {
                try
                {
                    RamIndexes.Add(manager);
                    //wh.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught exception handling the ramindex.");
                    //IndexConfig.Log(LOGTYPE.EXCEPTION, e.Message, e.StackTrace.ToString());
                }
            }
        }

        public static void InvokeRamindexHandlerThread()
        {
            wh.Set();
        }

       
        /// <summary>
        /// Take RAM indexes from memory and write to disk
        /// </summary>
        public void ManageRamIndexes()
        {
            DataTable indexStatusTbl = new DataTable();
            try
            {
                //List holding all RAM indexes
                List<Lucene.Net.Store.Directory> dir = new List<Lucene.Net.Store.Directory>();
                
                RAMIndexManager rammanager;
                //The whole console is meant to be ran as a server at some point so run this loop indefinitely.
                while (true)
                {
                    //Wait for the set
                    wh.WaitOne();
                    //Whenever there is files to be written in disk, the list will be populated
                    while (RamIndexes.Count > 0)
                    {                        
                        while (total_ramdir_size < IndexingConfig.SizeOfRamIndex && RamIndexes.Count > 0)
                        {
                            total_ramdir_size += ((RAMIndexManager)RamIndexes[0]).GetIndexSize();
                            lock (RamIndexes)
                            {
                                rammanager = ((RAMIndexManager)RamIndexes[0]);
                                RamIndexes.RemoveAt(0);
                            }
                            dir.Add(rammanager.ramDir);
                            rammanager = null;
                        }
                        //If the size exceeds the allowed size transfer data to disk                       
                        if (total_ramdir_size >= IndexingConfig.SizeOfRamIndex)
                        {
                            HandoverRamindexesToWriteToDisk(ref dir);
                        }
                    }

                    HandoverRamindexesToWriteToDisk(ref dir);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The Thread is aborted inside ramindexhandler.");
            }
        }

        private void HandoverRamindexesToWriteToDisk(ref List<Lucene.Net.Store.Directory> dir)
        {
            if (dir.Count > 0)
            {
                //Write to disk
                Console.WriteLine("Writing " + dir.Count + " RAM indexes to disk");
                WriteRamIndexToDisk(dir.ToArray(), IndexingConfig.indexlocation);
                Console.WriteLine("Finished Writing " + dir.Count + " ramIndexManager to disk");
                dir.Clear();
                
                //If there is nothing in queue then the process is complete    
                if (RamIndexes.Count == 0)
                {
                    Console.WriteLine("Writing to main Index at:" + DateTime.Now.ToString());
                    WriteToMainIndex(IndexingConfig.indexlocation);
                    Console.WriteLine("Finished Writing to main Index at:" + DateTime.Now.ToString());
                }
            }
        }

        /// <summary>
        /// create a temp index which might need merging.
        /// </summary>
        /// <param name="dir">Lucene index directory</param>
        /// <param name="index_Location">Index location</param>
        private static void WriteRamIndexToDisk(Lucene.Net.Store.Directory[] dir, string index_Location)
        {
            IndexWriter indexWriter = null;
            try
            {
                string Current_Temp_index_location = index_Location + "\\" + DateTime.Now.ToString("ddMMyyyyhhmmss");

                if (!System.IO.Directory.Exists(Current_Temp_index_location))
                {
                    System.IO.Directory.CreateDirectory(Current_Temp_index_location);
                }

                if (UnlockIndexIfLocked(Current_Temp_index_location))
                {
                    indexWriter = new IndexWriter(Current_Temp_index_location, new StandardAnalyzer(new Hashtable()), !IndexReader.IndexExists(Current_Temp_index_location));
                    indexWriter.SetMaxFieldLength(int.MaxValue);
                    indexWriter.SetMergeFactor(IndexingConfig.mergeFactor);
                    indexWriter.SetMaxBufferedDocs(IndexingConfig.maxBufferedDocs);
                    indexWriter.SetUseCompoundFile(false);
                    indexWriter.AddIndexes(dir);
                }
                else
                {
                    Console.WriteLine("The intended index is locked. Please contact administrator...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to write index to disk. Please check the write validations in the index location...");
            }
            finally
            {
                dir = null;
                if (indexWriter != null)
                {
                    indexWriter.Close();
                    indexWriter = null;
                }
                total_ramdir_size = 0;
            }
            GC.Collect(GC.MaxGeneration);
        }

        /// <summary>
        /// See if index is locked. This is rare but might happen due to previous writes or updates
        /// </summary>
        /// <param name="indexLocation">Index location</param>
        /// <returns>True if unlocked, false if cannot be unlocked</returns>
        private static bool UnlockIndexIfLocked(string indexLocation)
        {
            if (IndexReader.IsLocked(indexLocation))
            {
                try
                {
                    IndexReader.Unlock(Lucene.Net.Store.FSDirectory.GetDirectory(indexLocation));                   
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
                return true;
        }

        
        /// <summary>
        /// Create a main index from segments of temp indexes.
        /// </summary>
        /// <param name="index_location"></param>
        private static void WriteToMainIndex(string index_location)
        {
            IndexWriter indexWriter = null;
            List<IndexReader> indexreaders = new List<IndexReader>();

            List<string> tempIndexList = new List<string>();
            try
            {

                foreach (DirectoryInfo dfo in new DirectoryInfo(index_location).GetDirectories())
                {
                    if (IndexReader.IndexExists(dfo.FullName))
                    {
                        indexreaders.Add(IndexReader.Open(dfo.FullName));
                        tempIndexList.Add(dfo.FullName);
                    }
                }
                if (indexreaders.Count > 0)
                {
                    if (UnlockIndexIfLocked(index_location))
                    {
                        indexWriter = new IndexWriter(index_location, new StandardAnalyzer(new Hashtable()), !IndexReader.IndexExists(index_location));
                        indexWriter.SetMaxFieldLength(int.MaxValue);
                        indexWriter.SetMergeFactor(IndexingConfig.mergeFactor);
                        indexWriter.SetMaxBufferedDocs(IndexingConfig.maxBufferedDocs);
                        indexWriter.SetUseCompoundFile(false);
                        indexWriter.AddIndexes(indexreaders.ToArray());
                        indexWriter.Optimize();
                        indexWriter.Close();


                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error writing the main index.");
            }
            finally
            {
               
                foreach (DirectoryInfo dfo in new DirectoryInfo(index_location).GetDirectories())
                {
                    try
                    {
                        dfo.Delete(true);
                    }
                    catch (Exception e)
                    {
                        //No need to handle deletion error
                    }
                }
                if (indexWriter != null)
                    indexWriter.Close();
            }
            GC.Collect(GC.MaxGeneration);
        }

        public static void Clear()
        {
            RamIndexes.Clear();
            total_ramdir_size = 0;
        }
    }
}


