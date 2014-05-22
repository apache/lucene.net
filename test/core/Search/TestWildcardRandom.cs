using System;
using System.Text;

namespace Lucene.Net.Search
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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Create an index with terms from 000-999.
	/// Generates random wildcards according to patterns,
	/// and validates the correct number of hits are returned.
	/// </summary>
	public class TestWildcardRandom : LuceneTestCase
	{
	  private IndexSearcher Searcher;
	  private IndexReader Reader;
	  private Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));

		Document doc = new Document();
		Field field = newStringField("field", "", Field.Store.NO);
		doc.add(field);

		NumberFormat df = new DecimalFormat("000", new DecimalFormatSymbols(Locale.ROOT));
		for (int i = 0; i < 1000; i++)
		{
		  field.StringValue = df.format(i);
		  writer.addDocument(doc);
		}

		Reader = writer.Reader;
		Searcher = newSearcher(Reader);
		writer.close();
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: setUp searcher=" + Searcher);
		}
	  }

	  private char N()
	  {
		return (char)(0x30 + random().Next(10));
	  }

	  private string FillPattern(string wildcardPattern)
	  {
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < wildcardPattern.Length; i++)
		{
		  switch (wildcardPattern[i])
		  {
			case 'N':
			  sb.Append(N());
			  break;
			default:
			  sb.Append(wildcardPattern[i]);
		  break;
		  }
		}
		return sb.ToString();
	  }

	  private void AssertPatternHits(string pattern, int numHits)
	  {
		// TODO: run with different rewrites
		string filledPattern = FillPattern(pattern);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: run wildcard pattern=" + pattern + " filled=" + filledPattern);
		}
		Query wq = new WildcardQuery(new Term("field", filledPattern));
		TopDocs docs = Searcher.search(wq, 25);
		Assert.AreEqual("Incorrect hits for pattern: " + pattern, numHits, docs.totalHits);
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestWildcards()
	  {
		  ;
		int num = atLeast(1);
		for (int i = 0; i < num; i++)
		{
		  AssertPatternHits("NNN", 1);
		  AssertPatternHits("?NN", 10);
		  AssertPatternHits("N?N", 10);
		  AssertPatternHits("NN?", 10);
		}

		for (int i = 0; i < num; i++)
		{
		  AssertPatternHits("??N", 100);
		  AssertPatternHits("N??", 100);
		  AssertPatternHits("???", 1000);

		  AssertPatternHits("NN*", 10);
		  AssertPatternHits("N*", 100);
		  AssertPatternHits("*", 1000);

		  AssertPatternHits("*NN", 10);
		  AssertPatternHits("*N", 100);

		  AssertPatternHits("N*N", 10);

		  // combo of ? and * operators
		  AssertPatternHits("?N*", 100);
		  AssertPatternHits("N?*", 100);

		  AssertPatternHits("*N?", 100);
		  AssertPatternHits("*??", 1000);
		  AssertPatternHits("*?N", 100);
		}
	  }
	}

}