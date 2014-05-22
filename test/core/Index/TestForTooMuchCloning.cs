using System.Text;

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
	using TextField = Lucene.Net.Document.TextField;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermRangeQuery = Lucene.Net.Search.TermRangeQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestForTooMuchCloning : LuceneTestCase
	{

	  // Make sure we don't clone IndexInputs too frequently
	  // during merging:
	  public virtual void Test()
	  {
		// NOTE: if we see a fail on this test with "NestedPulsing" its because its 
		// reuse isnt perfect (but reasonable). see TestPulsingReuse.testNestedPulsing 
		// for more details
		MockDirectoryWrapper dir = newMockDirectory();
		TieredMergePolicy tmp = new TieredMergePolicy();
		tmp.MaxMergeAtOnce = 2;
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(tmp));
		const int numDocs = 20;
		for (int docs = 0;docs < numDocs;docs++)
		{
		  StringBuilder sb = new StringBuilder();
		  for (int terms = 0;terms < 100;terms++)
		  {
			sb.Append(TestUtil.randomRealisticUnicodeString(random()));
			sb.Append(' ');
		  }
		  Document doc = new Document();
		  doc.add(new TextField("field", sb.ToString(), Field.Store.NO));
		  w.addDocument(doc);
		}
		IndexReader r = w.Reader;
		w.close();

		int cloneCount = dir.InputCloneCount;
		//System.out.println("merge clone count=" + cloneCount);
		Assert.IsTrue("too many calls to IndexInput.clone during merging: " + dir.InputCloneCount, cloneCount < 500);

		IndexSearcher s = newSearcher(r);

		// MTQ that matches all terms so the AUTO_REWRITE should
		// cutover to filter rewrite and reuse a single DocsEnum
		// across all terms;
		TopDocs hits = s.search(new TermRangeQuery("field", new BytesRef(), new BytesRef("\uFFFF"), true, true), 10);
		Assert.IsTrue(hits.totalHits > 0);
		int queryCloneCount = dir.InputCloneCount - cloneCount;
		//System.out.println("query clone count=" + queryCloneCount);
		Assert.IsTrue("too many calls to IndexInput.clone during TermRangeQuery: " + queryCloneCount, queryCloneCount < 50);
		r.close();
		dir.close();
	  }
	}

}