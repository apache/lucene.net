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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FloatDocValuesField = Lucene.Net.Document.FloatDocValuesField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Tests the use of indexdocvalues in scoring.
	/// 
	/// In the example, a docvalues field is used as a per-document boost (separate from the norm)
	/// @lucene.experimental
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestDocValuesScoring extends Lucene.Net.Util.LuceneTestCase
	public class TestDocValuesScoring : LuceneTestCase
	{
	  private const float SCORE_EPSILON = 0.001f; // for comparing floats

	  public virtual void TestSimple()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		Field field = newTextField("foo", "", Field.Store.NO);
		doc.add(field);
		Field dvField = new FloatDocValuesField("foo_boost", 0.0F);
		doc.add(dvField);
		Field field2 = newTextField("bar", "", Field.Store.NO);
		doc.add(field2);

		field.StringValue = "quick brown fox";
		field2.StringValue = "quick brown fox";
		dvField.FloatValue = 2f; // boost x2
		iw.addDocument(doc);
		field.StringValue = "jumps over lazy brown dog";
		field2.StringValue = "jumps over lazy brown dog";
		dvField.FloatValue = 4f; // boost x4
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();

		// no boosting
		IndexSearcher searcher1 = newSearcher(ir, false);
		Similarity @base = searcher1.Similarity;
		// boosting
		IndexSearcher searcher2 = newSearcher(ir, false);
		searcher2.Similarity = new PerFieldSimilarityWrapperAnonymousInnerClassHelper(this, field, @base);

		// in this case, we searched on field "foo". first document should have 2x the score.
		TermQuery tq = new TermQuery(new Term("foo", "quick"));
		QueryUtils.check(random(), tq, searcher1);
		QueryUtils.check(random(), tq, searcher2);

		TopDocs noboost = searcher1.search(tq, 10);
		TopDocs boost = searcher2.search(tq, 10);
		Assert.AreEqual(1, noboost.totalHits);
		Assert.AreEqual(1, boost.totalHits);

		//System.out.println(searcher2.explain(tq, boost.scoreDocs[0].doc));
		Assert.AreEqual(boost.scoreDocs[0].score, noboost.scoreDocs[0].score * 2f, SCORE_EPSILON);

		// this query matches only the second document, which should have 4x the score.
		tq = new TermQuery(new Term("foo", "jumps"));
		QueryUtils.check(random(), tq, searcher1);
		QueryUtils.check(random(), tq, searcher2);

		noboost = searcher1.search(tq, 10);
		boost = searcher2.search(tq, 10);
		Assert.AreEqual(1, noboost.totalHits);
		Assert.AreEqual(1, boost.totalHits);

		Assert.AreEqual(boost.scoreDocs[0].score, noboost.scoreDocs[0].score * 4f, SCORE_EPSILON);

		// search on on field bar just for kicks, nothing should happen, since we setup
		// our sim provider to only use foo_boost for field foo.
		tq = new TermQuery(new Term("bar", "quick"));
		QueryUtils.check(random(), tq, searcher1);
		QueryUtils.check(random(), tq, searcher2);

		noboost = searcher1.search(tq, 10);
		boost = searcher2.search(tq, 10);
		Assert.AreEqual(1, noboost.totalHits);
		Assert.AreEqual(1, boost.totalHits);

		Assert.AreEqual(boost.scoreDocs[0].score, noboost.scoreDocs[0].score, SCORE_EPSILON);

		ir.close();
		dir.close();
	  }

	  private class PerFieldSimilarityWrapperAnonymousInnerClassHelper : PerFieldSimilarityWrapper
	  {
		  private readonly TestDocValuesScoring OuterInstance;

		  private Field Field;
		  private Similarity @base;

		  public PerFieldSimilarityWrapperAnonymousInnerClassHelper(TestDocValuesScoring outerInstance, Field field, Similarity @base)
		  {
			  this.OuterInstance = outerInstance;
			  this.Field = field;
			  this.@base = @base;
			  fooSim = new BoostingSimilarity(@base, "foo_boost");
		  }

		  internal readonly Similarity fooSim;

		  public override Similarity Get(string field)
		  {
			return "foo".Equals(field) ? fooSim : @base;
		  }

		  public override float Coord(int overlap, int maxOverlap)
		  {
			return @base.coord(overlap, maxOverlap);
		  }

		  public override float QueryNorm(float sumOfSquaredWeights)
		  {
			return @base.queryNorm(sumOfSquaredWeights);
		  }
	  }

	  /// <summary>
	  /// Similarity that wraps another similarity and boosts the final score
	  /// according to whats in a docvalues field.
	  /// 
	  /// @lucene.experimental
	  /// </summary>
	  internal class BoostingSimilarity : Similarity
	  {
		internal readonly Similarity Sim;
		internal readonly string BoostField;

		public BoostingSimilarity(Similarity sim, string boostField)
		{
		  this.Sim = sim;
		  this.BoostField = boostField;
		}

		public override long ComputeNorm(FieldInvertState state)
		{
		  return Sim.computeNorm(state);
		}

		public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
		{
		  return Sim.computeWeight(queryBoost, collectionStats, termStats);
		}

		public override SimScorer SimScorer(SimWeight stats, AtomicReaderContext context)
		{
		  SimScorer sub = Sim.simScorer(stats, context);
		  FieldCache.Floats values = FieldCache.DEFAULT.getFloats(context.reader(), BoostField, false);

		  return new SimScorerAnonymousInnerClassHelper(this, sub, values);
		}

		private class SimScorerAnonymousInnerClassHelper : SimScorer
		{
			private readonly BoostingSimilarity OuterInstance;

			private SimScorer Sub;
			private FieldCache.Floats Values;

			public SimScorerAnonymousInnerClassHelper(BoostingSimilarity outerInstance, SimScorer sub, FieldCache.Floats values)
			{
				this.OuterInstance = outerInstance;
				this.Sub = sub;
				this.Values = values;
			}

			public override float Score(int doc, float freq)
			{
			  return Values.get(doc) * Sub.score(doc, freq);
			}

			public override float ComputeSlopFactor(int distance)
			{
			  return Sub.computeSlopFactor(distance);
			}

			public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
			{
			  return Sub.computePayloadFactor(doc, start, end, payload);
			}

			public override Explanation Explain(int doc, Explanation freq)
			{
			  Explanation boostExplanation = new Explanation(Values.get(doc), "indexDocValue(" + OuterInstance.BoostField + ")");
			  Explanation simExplanation = Sub.explain(doc, freq);
			  Explanation expl = new Explanation(boostExplanation.Value * simExplanation.Value, "product of:");
			  expl.addDetail(boostExplanation);
			  expl.addDetail(simExplanation);
			  return expl;
			}
		}
	  }
	}

}