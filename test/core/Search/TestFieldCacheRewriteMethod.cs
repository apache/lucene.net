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

	using Term = Lucene.Net.Index.Term;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;

	/// <summary>
	/// Tests the FieldcacheRewriteMethod with random regular expressions
	/// </summary>
	public class TestFieldCacheRewriteMethod : TestRegexpRandom2
	{

	  /// <summary>
	  /// Test fieldcache rewrite against filter rewrite </summary>
	  protected internal override void AssertSame(string regexp)
	  {
		RegexpQuery fieldCache = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
		fieldCache.RewriteMethod = new FieldCacheRewriteMethod();

		RegexpQuery filter = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
		filter.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;

		TopDocs fieldCacheDocs = Searcher1.search(fieldCache, 25);
		TopDocs filterDocs = Searcher2.search(filter, 25);

		CheckHits.checkEqual(fieldCache, fieldCacheDocs.scoreDocs, filterDocs.scoreDocs);
	  }

	  public virtual void TestEquals()
	  {
		RegexpQuery a1 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
		RegexpQuery a2 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
		RegexpQuery b = new RegexpQuery(new Term(FieldName, "[bB]"), RegExp.NONE);
		Assert.AreEqual(a1, a2);
		Assert.IsFalse(a1.Equals(b));

		a1.RewriteMethod = new FieldCacheRewriteMethod();
		a2.RewriteMethod = new FieldCacheRewriteMethod();
		b.RewriteMethod = new FieldCacheRewriteMethod();
		Assert.AreEqual(a1, a2);
		Assert.IsFalse(a1.Equals(b));
		QueryUtils.check(a1);
	  }
	}

}