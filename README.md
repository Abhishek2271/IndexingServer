# IndexingServer
The application contains two parts:

1. Indexer: 

Generates an assembly file. The purpose is to create an api that can handle indexing of large volume of data. From the given folder, text (.txt) files are selected for indexing. The asssembly uses RAM to hold indexes partly until a certain limit is reached (50MB by default). The files are then written to disk and RAM is cleared for next batch of data. The assembly is meant to handle large number of large text files by making efficient use of available RAM to minimize disk IO so that the indexing is faster. Two separate threads are used to handle Disk IO and RAM index.

The output is the searchable Lucene index.

2. Console application: 

A windows console application that takes the text files in the defined location in the ini file then creates a searchable index. The process or the current steps are displayed in the console window. This application uses assembly from Indexing Server for actual indexing and Utility.dll to parse the ini.

-------------------------------------------------------------------------------------------------------------------------------------
Steps to use:
1. Go to \ConsoleApplication1\bin\Debug.
2. Open the ini file.
3. For configuring the ini file. 
	-- Please enter the input path which should be used as source for indexing
	-- An output path where the Lucene index should be created
	-- Under the [DB_SETTINGS] please provide the database credentials for DB connectivity.
4. Run ConsoleApplication.exe

NOTE: Currently DB connectivity is disabled and the process is only visible in the console window. 
--------------------------------------------------------------------------------------------------------------------------------
TESTING THE CREATED INDEX:
Prerequisite: JAVA should be installed.
To test the created index, please go to \ConsoleApplication1\bin\Debug. Then open the lukeall-3.5.0.jar file. 
Provide the index location this should open the index which should be searchable and the documents should be viewable within Luke.

NOTE: LUKE IS NOT MY WORK. It is a tool used to peek inside a Lucene index.

Luke can also be downloaded from 
http://www.npackd.org/p/org.getopt.Luke/3.5

