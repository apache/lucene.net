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

	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestNGramPhraseQuery : LuceneTestCase
	{

	  private static IndexReader Reader;
	  private static Directory Directory;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory);
		writer.close();
		Reader = DirectoryReader.open(Directory);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		Reader = null;
		Directory.close();
		Directory = null;
	  }

	  public virtual void TestRewrite()
	  {
		// bi-gram test ABC => AB/BC => AB/BC
		PhraseQuery pq1 = new NGramPhraseQuery(2);
		pq1.add(new Term("f", "AB"));
		pq1.add(new Term("f", "BC"));

		Query q = pq1.rewrite(Reader);
		Assert.IsTrue(q is NGramPhraseQuery);
		assertSame(pq1, q);
		pq1 = (NGramPhraseQuery)q;
		assertArrayEquals(new Term[]{new Term("f", "AB"), new Term("f", "BC")}, pq1.Terms);
		assertArrayEquals(new int[]{0, 1}, pq1.Positions);

		// bi-gram test ABCD => AB/BC/CD => AB//CD
		PhraseQuery pq2 = new NGramPhraseQuery(2);
		pq2.add(new Term("f", "AB"));
		pq2.add(new Term("f", "BC"));
		pq2.add(new Term("f", "CD"));

		q = pq2.rewrite(Reader);
		Assert.IsTrue(q is PhraseQuery);
		Assert.AreNotSame(pq2, q);
		pq2 = (PhraseQuery)q;
		assertArrayEquals(new Term[]{new Term("f", "AB"), new Term("f", "CD")}, pq2.Terms);
		assertArrayEquals(new int[]{0, 2}, pq2.Positions);

		// tri-gram test ABCDEFGH => ABC/BCD/CDE/DEF/EFG/FGH => ABC///DEF//FGH
		PhraseQuery pq3 = new NGramPhraseQuery(3);
		pq3.add(new Term("f", "ABC"));
		pq3.add(new Term("f", "BCD"));
		pq3.add(new Term("f", "CDE"));
		pq3.add(new Term("f", "DEF"));
		pq3.add(new Term("f", "EFG"));
		pq3.add(new Term("f", "FGH"));

		q = pq3.rewrite(Reader);
		Assert.IsTrue(q is PhraseQuery);
		Assert.AreNotSame(pq3, q);
		pq3 = (PhraseQuery)q;
		assertArrayEquals(new Term[]{new Term("f", "ABC"), new Term("f", "DEF"), new Term("f", "FGH")}, pq3.Terms);
		assertArrayEquals(new int[]{0, 3, 5}, pq3.Positions);

		// LUCENE-4970: boosting test
		PhraseQuery pq4 = new NGramPhraseQuery(2);
		pq4.add(new Term("f", "AB"));
		pq4.add(new Term("f", "BC"));
		pq4.add(new Term("f", "CD"));
		pq4.Boost = 100.0F;

		q = pq4.rewrite(Reader);
		Assert.AreNotSame(pq4, q);
		Assert.AreEqual(pq4.Boost, q.Boost, 0.1f);
	  }

	}

}