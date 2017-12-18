using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using System.Collections;
using Indexer;

namespace IndexingServer
{
    class IndexingClass
    {
       /// <summary>
       /// Open text files and begin to index
       /// </summary>
        public void MainIndexProcess()
        {
            try
            {
                //define unique fileid for each file
                int id = 0;
                int threadid = int.Parse(Thread.CurrentThread.Name);
                int NumberOfBytesForAChar = Encoding.Unicode.GetBytes("a").Length;  
                long SizeOfRamIndexPerThread = IndexingConfig.SizeOfRamIndex/IndexingConfig.NumberOfIndexingThreads;
                DirectoryInfo dinfo = new DirectoryInfo(IndexingConfig.sourceDirectoryLocation);
                FileInfo[] Files = dinfo.GetFiles("*.txt");
                RAMIndexManager ramIndexManager = null;
                foreach (FileInfo file in Files)
                {
                    ++id;
                    if (ramIndexManager == null)
                    {
                        ramIndexManager = new RAMIndexManager(IndexingConfig.indexlocation);
                        ramIndexManager.IndexMedia(file, id);
                        //If RAM index reaches limit
                        if (ramIndexManager.GetRAMSize() >= SizeOfRamIndexPerThread || Files.Length >= id)
                        {
                            HandOverRamIndex(ref ramIndexManager);
                        }
                    }
                }
                //for small file numbers this can be harcoded here to call the new thread to write index to disk but better implementation would be to call only when size exceeds.
                RamIndexHandler.InvokeRamindexHandlerThread();
            }
            catch (Exception e)
            {
                Console.WriteLine("Thread " + Thread.CurrentThread.Name + " Aborted.");
            }
        }

        /// <summary>
        /// Populate the index info table so that other thread can pick up.
        /// </summary>
        /// <param name="ramIndexManager">Instance of class ramIndexManager</param>
        private void HandOverRamIndex(ref RAMIndexManager ramIndexManager)
        {
            ramIndexManager.CloseWriter();
            RamIndexHandler.AddRamIndex(ramIndexManager);
            ramIndexManager = null;
        }
    }
}
