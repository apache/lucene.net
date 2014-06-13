using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	// Make sure if you use NoDeletionPolicy that no file
	// referenced by a commit point is ever deleted

	public class TestNeverDelete : LuceneTestCase
	{

	  public virtual void TestIndexing()
	  {
		File tmpDir = CreateTempDir("TestNeverDelete");
		BaseDirectoryWrapper d = NewFSDirectory(tmpDir);

		// We want to "see" files removed if Lucene removed
		// them.  this is still worth running on Windows since
		// some files the IR opens and closes.
		if (d is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)d).NoDeleteOpenFile = false;
		}
		RandomIndexWriter w = new RandomIndexWriter(Random(), d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE));
		w.w.Config.MaxBufferedDocs = TestUtil.NextInt(Random(), 5, 30);

		w.Commit();
		Thread[] indexThreads = new Thread[Random().Next(4)];
		long stopTime = DateTime.Now.Millisecond + AtLeast(1000);
		for (int x = 0; x < indexThreads.Length; x++)
		{
		  indexThreads[x] = new ThreadAnonymousInnerClassHelper(this, w, stopTime);
		  indexThreads[x].Name = "Thread " + x;
		  indexThreads[x].Start();
		}

		Set<string> allFiles = new HashSet<string>();

		DirectoryReader r = DirectoryReader.Open(d);
		while (DateTime.Now.Millisecond < stopTime)
		{
		  IndexCommit ic = r.IndexCommit;
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: check files: " + ic.FileNames);
		  }
		  allFiles.addAll(ic.FileNames);
		  // Make sure no old files were removed
		  foreach (string fileName in allFiles)
		  {
			Assert.IsTrue("file " + fileName + " does not exist", SlowFileExists(d, fileName));
		  }
		  DirectoryReader r2 = DirectoryReader.OpenIfChanged(r);
		  if (r2 != null)
		  {
			r.Dispose();
			r = r2;
		  }
		  Thread.Sleep(1);
		}
		r.Dispose();

		foreach (Thread t in indexThreads)
		{
		  t.Join();
		}
        w.Close();
		d.Dispose();

		TestUtil.Rm(tmpDir);
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestNeverDelete OuterInstance;

		  private RandomIndexWriter w;
		  private long StopTime;

		  public ThreadAnonymousInnerClassHelper(TestNeverDelete outerInstance, RandomIndexWriter w, long stopTime)
		  {
			  this.OuterInstance = outerInstance;
			  this.w = w;
			  this.StopTime = stopTime;
		  }

		  public override void Run()
		  {
			try
			{
			  int docCount = 0;
			  while (DateTime.Now.Millisecond < StopTime)
			  {
				Document doc = new Document();
				doc.Add(NewStringField("dc", "" + docCount, Field.Store.YES));
				doc.Add(NewTextField("field", "here is some text", Field.Store.YES));
				w.AddDocument(doc);

				if (docCount % 13 == 0)
				{
				  w.Commit();
				}
				docCount++;
			  }
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }
	}

}