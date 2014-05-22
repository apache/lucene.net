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

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;

	/// <summary>
	/// Similarity unit test.
	/// 
	/// 
	/// </summary>
	public class TestSimilarity : LuceneTestCase
	{

	  public class SimpleSimilarity : DefaultSimilarity
	  {
		public override float QueryNorm(float sumOfSquaredWeights)
		{
			return 1.0f;
		}
		public override float Coord(int overlap, int maxOverlap)
		{
			return 1.0f;
		}
		public override float LengthNorm(FieldInvertState state)
		{
			return state.Boost;
		}
		public override float Tf(float freq)
		{
			return freq;
		}
		public override float SloppyFreq(int distance)
		{
			return 2.0f;
		}
		public override float Idf(long docFreq, long numDocs)
		{
			return 1.0f;
		}
		public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] stats)
		{
		  return new Explanation(1.0f, "Inexplicable");
		}
	  }

	  public virtual void TestSimilarity()
	  {
		Directory store = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), store, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setSimilarity(new SimpleSimilarity()));

		Document d1 = new Document();
		d1.add(newTextField("field", "a c", Field.Store.YES));

		Document d2 = new Document();
		d2.add(newTextField("field", "a b c", Field.Store.YES));

		writer.addDocument(d1);
		writer.addDocument(d2);
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);
		searcher.Similarity = new SimpleSimilarity();

		Term a = new Term("field", "a");
		Term b = new Term("field", "b");
		Term c = new Term("field", "c");

		searcher.search(new TermQuery(b), new CollectorAnonymousInnerClassHelper(this));

		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(a), BooleanClause.Occur.SHOULD);
		bq.add(new TermQuery(b), BooleanClause.Occur.SHOULD);
		//System.out.println(bq.toString("field"));
		searcher.search(bq, new CollectorAnonymousInnerClassHelper2(this));

		PhraseQuery pq = new PhraseQuery();
		pq.add(a);
		pq.add(c);
		//System.out.println(pq.toString("field"));
		searcher.search(pq, new CollectorAnonymousInnerClassHelper3(this));

		pq.Slop = 2;
		//System.out.println(pq.toString("field"));
		searcher.search(pq, new CollectorAnonymousInnerClassHelper4(this));

		reader.close();
		store.close();
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestSimilarity OuterInstance;

		  public CollectorAnonymousInnerClassHelper(TestSimilarity outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override void Collect(int doc)
		  {
			Assert.AreEqual(1.0f, scorer.score(), 0);
		  }
		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
			  }
		  }
		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

	  private class CollectorAnonymousInnerClassHelper2 : Collector
	  {
		  private readonly TestSimilarity OuterInstance;

		  public CollectorAnonymousInnerClassHelper2(TestSimilarity outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  @base = 0;
		  }

		  private int @base;
		  private Scorer scorer;
		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override void Collect(int doc)
		  {
			//System.out.println("Doc=" + doc + " score=" + score);
			Assert.AreEqual((float)doc + @base+1, scorer.score(), 0);
		  }
		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				@base = value.docBase;
			  }
		  }
		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

	  private class CollectorAnonymousInnerClassHelper3 : Collector
	  {
		  private readonly TestSimilarity OuterInstance;

		  public CollectorAnonymousInnerClassHelper3(TestSimilarity outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override Scorer Scorer
		  {
			  set
			  {
			   this.scorer = value;
			  }
		  }
		  public override void Collect(int doc)
		  {
			//System.out.println("Doc=" + doc + " score=" + score);
			Assert.AreEqual(1.0f, scorer.score(), 0);
		  }
		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
			  }
		  }
		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

	  private class CollectorAnonymousInnerClassHelper4 : Collector
	  {
		  private readonly TestSimilarity OuterInstance;

		  public CollectorAnonymousInnerClassHelper4(TestSimilarity outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override void Collect(int doc)
		  {
			//System.out.println("Doc=" + doc + " score=" + score);
			Assert.AreEqual(2.0f, scorer.score(), 0);
		  }
		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
			  }
		  }
		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }
	}

}