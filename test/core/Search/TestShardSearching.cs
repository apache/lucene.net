using System;
using System.Collections.Generic;

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


	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using Term = Lucene.Net.Index.Term;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TestUtil = Lucene.Net.Util.TestUtil;

	// TODO
	//   - other queries besides PrefixQuery & TermQuery (but:
	//     FuzzyQ will be problematic... the top N terms it
	//     takes means results will differ)
	//   - NRQ/F
	//   - BQ, negated clauses, negated prefix clauses
	//   - test pulling docs in 2nd round trip...
	//   - filter too

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestShardSearching extends ShardSearchingTestBase
	public class TestShardSearching : ShardSearchingTestBase
	{

	  private class PreviousSearchState
	  {
		public readonly long SearchTimeNanos;
		public readonly long[] Versions;
		public readonly ScoreDoc SearchAfterLocal;
		public readonly ScoreDoc SearchAfterShard;
		public readonly Sort Sort;
		public readonly Query Query;
		public readonly int NumHitsPaged;

		public PreviousSearchState(Query query, Sort sort, ScoreDoc searchAfterLocal, ScoreDoc searchAfterShard, long[] versions, int numHitsPaged)
		{
		  this.Versions = versions.clone();
		  this.SearchAfterLocal = searchAfterLocal;
		  this.SearchAfterShard = searchAfterShard;
		  this.Sort = sort;
		  this.Query = query;
		  this.NumHitsPaged = numHitsPaged;
		  SearchTimeNanos = System.nanoTime();
		}
	  }

	  public virtual void TestSimple()
	  {
		int numNodes = TestUtil.Next(random(), 1, 10);

		double runTimeSec = atLeast(3);

		int minDocsToMakeTerms = TestUtil.Next(random(), 5, 20);

		int maxSearcherAgeSeconds = TestUtil.Next(random(), 1, 3);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: numNodes=" + numNodes + " runTimeSec=" + runTimeSec + " maxSearcherAgeSeconds=" + maxSearcherAgeSeconds);
		}

		start(numNodes, runTimeSec, maxSearcherAgeSeconds);

		IList<PreviousSearchState> priorSearches = new List<PreviousSearchState>();
		IList<BytesRef> terms = null;
		while (System.nanoTime() < endTimeNanos)
		{

		  bool doFollowon = priorSearches.Count > 0 && random().Next(7) == 1;

		  // Pick a random node; we will run the query on this node:
		  int myNodeID = random().Next(numNodes);

		  NodeState.ShardIndexSearcher localShardSearcher;

		  PreviousSearchState prevSearchState;

		  if (doFollowon)
		  {
			// Pretend user issued a followon query:
			prevSearchState = priorSearches[random().Next(priorSearches.Count)];

			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: follow-on query age=" + ((System.nanoTime() - prevSearchState.SearchTimeNanos) / 1000000000.0));
			}

			try
			{
			  localShardSearcher = nodes[myNodeID].acquire(prevSearchState.Versions);
			}
			catch (SearcherExpiredException see)
			{
			  // Expected, sometimes; in a "real" app we would
			  // either forward this error to the user ("too
			  // much time has passed; please re-run your
			  // search") or sneakily just switch to newest
			  // searcher w/o telling them...
			  if (VERBOSE)
			  {
				Console.WriteLine("  searcher expired during local shard searcher init: " + see);
			  }
			  priorSearches.Remove(prevSearchState);
			  continue;
			}
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: fresh query");
			}
			// Do fresh query:
			localShardSearcher = nodes[myNodeID].acquire();
			prevSearchState = null;
		  }

		  IndexReader[] subs = new IndexReader[numNodes];

		  PreviousSearchState searchState = null;

		  try
		  {

			// Mock: now make a single reader (MultiReader) from all node
			// searchers.  In a real shard env you can't do this... we
			// do it to confirm results from the shard searcher
			// are correct:
			int docCount = 0;
			try
			{
			  for (int nodeID = 0;nodeID < numNodes;nodeID++)
			  {
				long subVersion = localShardSearcher.nodeVersions[nodeID];
				IndexSearcher sub = nodes[nodeID].searchers.acquire(subVersion);
				if (sub == null)
				{
				  nodeID--;
				  while (nodeID >= 0)
				  {
					subs[nodeID].decRef();
					subs[nodeID] = null;
					nodeID--;
				  }
				  throw new SearcherExpiredException("nodeID=" + nodeID + " version=" + subVersion);
				}
				subs[nodeID] = sub.IndexReader;
				docCount += subs[nodeID].maxDoc();
			  }
			}
			catch (SearcherExpiredException see)
			{
			  // Expected
			  if (VERBOSE)
			  {
				Console.WriteLine("  searcher expired during mock reader init: " + see);
			  }
			  continue;
			}

			IndexReader mockReader = new MultiReader(subs);
			IndexSearcher mockSearcher = new IndexSearcher(mockReader);

			Query query;
			Sort sort;

			if (prevSearchState != null)
			{
			  query = prevSearchState.Query;
			  sort = prevSearchState.Sort;
			}
			else
			{
			  if (terms == null && docCount > minDocsToMakeTerms)
			  {
				// TODO: try to "focus" on high freq terms sometimes too
				// TODO: maybe also periodically reset the terms...?
				TermsEnum termsEnum = MultiFields.getTerms(mockReader, "body").iterator(null);
				terms = new List<>();
				while (termsEnum.next() != null)
				{
				  terms.Add(BytesRef.deepCopyOf(termsEnum.term()));
				}
				if (VERBOSE)
				{
				  Console.WriteLine("TEST: init terms: " + terms.Count + " terms");
				}
				if (terms.Count == 0)
				{
				  terms = null;
				}
			  }

			  if (VERBOSE)
			  {
				Console.WriteLine("  maxDoc=" + mockReader.maxDoc());
			  }

			  if (terms != null)
			  {
				if (random().nextBoolean())
				{
				  query = new TermQuery(new Term("body", terms[random().Next(terms.Count)]));
				}
				else
				{
				  string t = terms[random().Next(terms.Count)].utf8ToString();
				  string prefix;
				  if (t.Length <= 1)
				  {
					prefix = t;
				  }
				  else
				  {
					prefix = t.Substring(0, TestUtil.Next(random(), 1, 2));
				  }
				  query = new PrefixQuery(new Term("body", prefix));
				}

				if (random().nextBoolean())
				{
				  sort = null;
				}
				else
				{
				  // TODO: sort by more than 1 field
				  int what = random().Next(3);
				  if (what == 0)
				  {
					sort = new Sort(SortField.FIELD_SCORE);
				  }
				  else if (what == 1)
				  {
					// TODO: this sort doesn't merge
					// correctly... it's tricky because you
					// could have > 2.1B docs across all shards: 
					//sort = new Sort(SortField.FIELD_DOC);
					sort = null;
				  }
				  else if (what == 2)
				  {
					sort = new Sort(new SortField[] {new SortField("docid", SortField.Type.INT, random().nextBoolean())});
				  }
				  else
				  {
					sort = new Sort(new SortField[] {new SortField("title", SortField.Type.STRING, random().nextBoolean())});
				  }
				}
			  }
			  else
			  {
				query = null;
				sort = null;
			  }
			}

			if (query != null)
			{

			  try
			  {
				searchState = AssertSame(mockSearcher, localShardSearcher, query, sort, prevSearchState);
			  }
			  catch (SearcherExpiredException see)
			  {
				// Expected; in a "real" app we would
				// either forward this error to the user ("too
				// much time has passed; please re-run your
				// search") or sneakily just switch to newest
				// searcher w/o telling them...
				if (VERBOSE)
				{
				  Console.WriteLine("  searcher expired during search: " + see);
				  see.printStackTrace(System.out);
				}
				// We can't do this in general: on a very slow
				// computer it's possible the local searcher
				// expires before we can finish our search:
				// assert prevSearchState != null;
				if (prevSearchState != null)
				{
				  priorSearches.Remove(prevSearchState);
				}
			  }
			}
		  }
		  finally
		  {
			nodes[myNodeID].release(localShardSearcher);
			foreach (IndexReader sub in subs)
			{
			  if (sub != null)
			  {
				sub.decRef();
			  }
			}
		  }

		  if (searchState != null && searchState.SearchAfterLocal != null && random().Next(5) == 3)
		  {
			priorSearches.Add(searchState);
			if (priorSearches.Count > 200)
			{
			  Collections.shuffle(priorSearches, random());
			  priorSearches.subList(100, priorSearches.Count).clear();
			}
		  }
		}

		finish();
	  }

	  private PreviousSearchState AssertSame(IndexSearcher mockSearcher, NodeState.ShardIndexSearcher shardSearcher, Query q, Sort sort, PreviousSearchState state)
	  {

		int numHits = TestUtil.Next(random(), 1, 100);
		if (state != null && state.SearchAfterLocal == null)
		{
		  // In addition to what we last searched:
		  numHits += state.NumHitsPaged;
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: query=" + q + " sort=" + sort + " numHits=" + numHits);
		  if (state != null)
		  {
			Console.WriteLine("  prev: searchAfterLocal=" + state.SearchAfterLocal + " searchAfterShard=" + state.SearchAfterShard + " numHitsPaged=" + state.NumHitsPaged);
		  }
		}

		// Single (mock local) searcher:
		TopDocs hits;
		if (sort == null)
		{
		  if (state != null && state.SearchAfterLocal != null)
		  {
			hits = mockSearcher.searchAfter(state.SearchAfterLocal, q, numHits);
		  }
		  else
		  {
			hits = mockSearcher.search(q, numHits);
		  }
		}
		else
		{
		  hits = mockSearcher.search(q, numHits, sort);
		}

		// Shard searcher
		TopDocs shardHits;
		if (sort == null)
		{
		  if (state != null && state.SearchAfterShard != null)
		  {
			shardHits = shardSearcher.searchAfter(state.SearchAfterShard, q, numHits);
		  }
		  else
		  {
			shardHits = shardSearcher.search(q, numHits);
		  }
		}
		else
		{
		  shardHits = shardSearcher.search(q, numHits, sort);
		}

		int numNodes = shardSearcher.nodeVersions.length;
		int[] @base = new int[numNodes];
		IList<IndexReaderContext> subs = mockSearcher.TopReaderContext.children();
		Assert.AreEqual(numNodes, subs.Count);

		for (int nodeID = 0;nodeID < numNodes;nodeID++)
		{
		  @base[nodeID] = subs[nodeID].docBaseInParent;
		}

		if (VERBOSE)
		{
		  /*
		  for(int shardID=0;shardID<shardSearchers.length;shardID++) {
		    System.out.println("  shard=" + shardID + " maxDoc=" + shardSearchers[shardID].searcher.getIndexReader().maxDoc());
		  }
		  */
		  Console.WriteLine("  single searcher: " + hits.totalHits + " totalHits maxScore=" + hits.MaxScore);
		  for (int i = 0;i < hits.scoreDocs.length;i++)
		  {
			ScoreDoc sd = hits.scoreDocs[i];
			Console.WriteLine("    doc=" + sd.doc + " score=" + sd.score);
		  }
		  Console.WriteLine("  shard searcher: " + shardHits.totalHits + " totalHits maxScore=" + shardHits.MaxScore);
		  for (int i = 0;i < shardHits.scoreDocs.length;i++)
		  {
			ScoreDoc sd = shardHits.scoreDocs[i];
			Console.WriteLine("    doc=" + sd.doc + " (rebased: " + (sd.doc + @base[sd.shardIndex]) + ") score=" + sd.score + " shard=" + sd.shardIndex);
		  }
		}

		int numHitsPaged;
		if (state != null && state.SearchAfterLocal != null)
		{
		  numHitsPaged = hits.scoreDocs.length;
		  if (state != null)
		  {
			numHitsPaged += state.NumHitsPaged;
		  }
		}
		else
		{
		  numHitsPaged = hits.scoreDocs.length;
		}

		bool moreHits;

		ScoreDoc bottomHit;
		ScoreDoc bottomHitShards;

		if (numHitsPaged < hits.totalHits)
		{
		  // More hits to page through
		  moreHits = true;
		  if (sort == null)
		  {
			bottomHit = hits.scoreDocs[hits.scoreDocs.length - 1];
			ScoreDoc sd = shardHits.scoreDocs[shardHits.scoreDocs.length - 1];
			// Must copy because below we rebase:
			bottomHitShards = new ScoreDoc(sd.doc, sd.score, sd.shardIndex);
			if (VERBOSE)
			{
			  Console.WriteLine("  save bottomHit=" + bottomHit);
			}
		  }
		  else
		  {
			bottomHit = null;
			bottomHitShards = null;
		  }

		}
		else
		{
		  Assert.AreEqual(hits.totalHits, numHitsPaged);
		  bottomHit = null;
		  bottomHitShards = null;
		  moreHits = false;
		}

		// Must rebase so Assert.AreEqual passes:
		for (int hitID = 0;hitID < shardHits.scoreDocs.length;hitID++)
		{
		  ScoreDoc sd = shardHits.scoreDocs[hitID];
		  sd.doc += @base[sd.shardIndex];
		}

		TestUtil.Assert.AreEqual(hits, shardHits);

		if (moreHits)
		{
		  // Return a continuation:
		  return new PreviousSearchState(q, sort, bottomHit, bottomHitShards, shardSearcher.nodeVersions, numHitsPaged);
		}
		else
		{
		  return null;
		}
	  }
	}

}