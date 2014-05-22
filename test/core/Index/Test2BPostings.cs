using System;

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
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TimeUnits = Lucene.Net.Util.TimeUnits;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;

	/// <summary>
	/// Test indexes ~82M docs with 26 terms each, so you get > Integer.MAX_VALUE terms/docs pairs
	/// @lucene.experimental
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct", "Compressing" }) @TimeoutSuite(millis = 4 * TimeUnits.HOUR) public class Test2BPostings extends Lucene.Net.Util.LuceneTestCase
	public class Test2BPostings : LuceneTestCase
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void test() throws Exception
		public virtual void Test()
		{
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("2BPostings"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}

		IndexWriterConfig iwc = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH).setRAMBufferSizeMB(256.0).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(false, 10)).setOpenMode(IndexWriterConfig.OpenMode.CREATE);

		IndexWriter w = new IndexWriter(dir, iwc);

		MergePolicy mp = w.Config.MergePolicy;
		if (mp is LogByteSizeMergePolicy)
		{
		 // 1 petabyte:
		 ((LogByteSizeMergePolicy) mp).MaxMergeMB = 1024 * 1024 * 1024;
		}

		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.OmitNorms = true;
		ft.IndexOptions = IndexOptions.DOCS_ONLY;
		Field field = new Field("field", new MyTokenStream(), ft);
		doc.add(field);

		int numDocs = (int.MaxValue / 26) + 1;
		for (int i = 0; i < numDocs; i++)
		{
		  w.addDocument(doc);
		  if (VERBOSE && i % 100000 == 0)
		  {
			Console.WriteLine(i + " of " + numDocs + "...");
		  }
		}
		w.forceMerge(1);
		w.close();
		dir.close();
		}

	  public sealed class MyTokenStream : TokenStream
	  {
		internal readonly CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));
		internal int Index;

		public override bool IncrementToken()
		{
		  if (Index <= 'z')
		  {
			ClearAttributes();
			TermAtt.Length = 1;
			TermAtt.buffer()[0] = (char) Index++;
			return true;
		  }
		  return false;
		}

		public override void Reset()
		{
		  Index = 'a';
		}
	  }
	}

}