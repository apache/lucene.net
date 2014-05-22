using System;
using System.Collections.Generic;

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
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests the maxTermFrequency statistic in FieldInvertState
	/// </summary>
	public class TestMaxTermFrequency : LuceneTestCase
	{
	  internal Directory Dir;
	  internal IndexReader Reader;
	  /* expected maxTermFrequency values for our documents */
	  internal List<int?> Expected = new List<int?>();

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.SIMPLE, true)).setMergePolicy(newLogMergePolicy());
		config.Similarity = new TestSimilarity(this);
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, config);
		Document doc = new Document();
		Field foo = newTextField("foo", "", Field.Store.NO);
		doc.add(foo);
		for (int i = 0; i < 100; i++)
		{
		  foo.StringValue = AddValue();
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {
		NumericDocValues fooNorms = MultiDocValues.getNormValues(Reader, "foo");
		for (int i = 0; i < Reader.maxDoc(); i++)
		{
		  Assert.AreEqual((int)Expected[i], fooNorms.get(i) & 0xff);
		}
	  }

	  /// <summary>
	  /// Makes a bunch of single-char tokens (the max freq will at most be 255).
	  /// shuffles them around, and returns the whole list with Arrays.toString().
	  /// this works fine because we use lettertokenizer.
	  /// puts the max-frequency term into expected, to be checked against the norm.
	  /// </summary>
	  private string AddValue()
	  {
		IList<string> terms = new List<string>();
		int maxCeiling = TestUtil.Next(random(), 0, 255);
		int max = 0;
		for (char ch = 'a'; ch <= 'z'; ch++)
		{
		  int num = TestUtil.Next(random(), 0, maxCeiling);
		  for (int i = 0; i < num; i++)
		  {
			terms.Add(char.ToString(ch));
		  }
		  max = Math.Max(max, num);
		}
		Expected.Add(max);
		Collections.shuffle(terms, random());
		return Arrays.ToString(terms.ToArray());
	  }

	  /// <summary>
	  /// Simple similarity that encodes maxTermFrequency directly as a byte
	  /// </summary>
	  internal class TestSimilarity : TFIDFSimilarity
	  {
		  private readonly TestMaxTermFrequency OuterInstance;

		  public TestSimilarity(TestMaxTermFrequency outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override float LengthNorm(FieldInvertState state)
		{
		  return state.MaxTermFrequency;
		}

		public override long EncodeNormValue(float f)
		{
		  return (sbyte) f;
		}

		public override float DecodeNormValue(long norm)
		{
		  return norm;
		}

		public override float Coord(int overlap, int maxOverlap)
		{
			return 0;
		}
		public override float QueryNorm(float sumOfSquaredWeights)
		{
			return 0;
		}
		public override float Tf(float freq)
		{
			return 0;
		}
		public override float Idf(long docFreq, long numDocs)
		{
			return 0;
		}
		public override float SloppyFreq(int distance)
		{
			return 0;
		}
		public override float ScorePayload(int doc, int start, int end, BytesRef payload)
		{
			return 0;
		}
	  }
	}

}