#region   2009 - 2013 Venio. All rights reserved
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written permission of the copyright owner.
// Filename: RAMIndexManager.cs
#endregion


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Data;
using Lucene.Net.Documents;
using System.Data.SqlClient;
using System.Threading;
using System.Collections;
using Lucene.Net.Analysis.Standard;
using System.Runtime.InteropServices;
using IndexingServer;


namespace Indexer
{
    /// <Summary>
    /// Index the given block of documents in the RAM directory
    /// </Summary>
    public class RAMIndexManager
    {
        private IndexWriter ramIndexWriter = null;  
        public RAMDirectory ramDir = null;  
        public string indexlocation;        //Destination index location
       
        private long TotalIndexedSize = 0;
        

        public RAMIndexManager(string index_location)
        {           
            InitializeWriter(index_location);   
            TotalIndexedSize = 0;
        }

        /// <summary>
        /// RAM index size in bytes
        /// </summary>
        public long GetRAMSize()
        {   
            return TotalIndexedSize;
        }

        public long GetIndexSize()
        {
            //Console.WriteLine(this.ramDir.SizeInBytes());
            return this.ramDir.SizeInBytes();
        }
          
        
        /// <summary>
        /// Return total document in the RAM directory
        /// </summary>
        public int DocumentsCount
        {
            get
            {
                return this.ramIndexWriter.DocCount();
            }
        }

        /// <summary>
        ///  Initialize index writer
        /// </summary>
        private void InitializeWriter(string index_location)
        {
            this.indexlocation = index_location;   
            ramDir = new RAMDirectory();           //Create an empty RAM directory   
            this.ramIndexWriter = new IndexWriter(ramDir, new StandardAnalyzer(new Hashtable()), true);
            this.ramIndexWriter.SetUseCompoundFile(false);  
            this.ramIndexWriter.SetMaxFieldLength(int.MaxValue); 
            this.ramIndexWriter.SetMaxBufferedDocs(IndexingConfig.maxBufferedDocs);
        }

        /// <summary>
        ///  Close index writer
        /// </summary>
        public void CloseWriter()
        {
            this.ramIndexWriter.Close();    
        }

        /// <summary>
        /// Create a Lucene index
        /// </summary>
        /// <param name="File">File info</param>
        /// <param name="fileId">Unique id for each document</param>
        public void IndexMedia(FileInfo File, int fileId)
        {
            try
            {
                Document doc = new Document();
                doc.Add(new Field("FileID", fileId.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));

                doc.Add(
                    new Field("Contents",
                        new StreamReader(
                            new FileStream(File.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)
                                        )
                            )
                        );

                ramIndexWriter.AddDocument(doc);
                doc = null;
                TotalIndexedSize += File.Length;
            }
            catch (Exception e) 
            {
                Console.WriteLine("Error while indexing file: " + File.Name.ToString().Trim() + " : " + e.Message);
                //no need to throw exception. Do not interrupt the whole process because of a single file.
            }
        }
    }
}
