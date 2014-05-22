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
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using Document = Lucene.Net.Document.Document;

	/// <summary>
	/// Tests <seealso cref="PrefixQuery"/> class.
	/// 
	/// </summary>
	public class TestPrefixQuery : LuceneTestCase
	{
	  public virtual void TestPrefixQuery()
	  {
		Directory directory = newDirectory();

		string[] categories = new string[] {"/Computers", "/Computers/Mac", "/Computers/Windows"};
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		for (int i = 0; i < categories.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("category", categories[i], Field.Store.YES));
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;

		PrefixQuery query = new PrefixQuery(new Term("category", "/Computers"));
		IndexSearcher searcher = newSearcher(reader);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("All documents in /Computers category and below", 3, hits.Length);

		query = new PrefixQuery(new Term("category", "/Computers/Mac"));
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("One in /Computers/Mac", 1, hits.Length);

		query = new PrefixQuery(new Term("category", ""));
		Terms terms = MultiFields.getTerms(searcher.IndexReader, "category");
		Assert.IsFalse(query.getTermsEnum(terms) is PrefixTermsEnum);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("everything", 3, hits.Length);
		writer.close();
		reader.close();
		directory.close();
	  }
	}

}