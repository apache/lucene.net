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
	using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// this tests the patch for issue #LUCENE-715 (IndexWriter does not
	/// release its write lock when trying to open an index which does not yet
	/// exist).
	/// </summary>
	public class TestIndexWriterLockRelease : LuceneTestCase
	{

	  public virtual void TestIndexWriterLockRelease()
	  {
		Directory dir = NewFSDirectory(CreateTempDir("testLockRelease"));
		try
		{
		  new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode_e.APPEND));
		}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		catch (FileNotFoundException | NoSuchFileException e)
		{
		  try
		  {
			new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetOpenMode(OpenMode_e.APPEND));
		  }
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		  catch (FileNotFoundException | NoSuchFileException e1)
		  {
		  }
		}
		finally
		{
		  dir.Dispose();
		}
	  }
	}

}