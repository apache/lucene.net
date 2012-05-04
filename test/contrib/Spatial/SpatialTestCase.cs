﻿/*
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

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Contrib.Spatial.Test
{
	public class SpatialTestCase : LuceneTestCase
	{
		private DirectoryReader indexReader;
		private IndexWriter indexWriter;
		private Directory directory;
		private IndexSearcher indexSearcher;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			Random random = random();

			directory = newDirectory();

			IndexWriterConfig writerConfig = newIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random));
			indexWriter = new IndexWriter(directory, writerConfig);
		}

		[TearDown]
		public override void TearDown()
		{
			if (indexWriter != null)
			{
				indexWriter.Close();
			}
			if (indexReader != null)
			{
				indexReader.Close();
			}
			if (directory != null)
			{
				directory.Close();
			}
			base.TearDown();
		}
		// ================================================= Helper Methods ================================================

		protected void addDocument(Document doc)
		{
			indexWriter.AddDocument(doc);
		}

		protected void addDocumentsAndCommit(List<Document> documents)
		{
			foreach (var document in documents)
			{
				indexWriter.AddDocument(document);
			}
			commit();
		}

		protected void deleteAll()
		{
			indexWriter.DeleteAll();
		}

		protected void commit()
		{
			indexWriter.Commit();
			if (indexReader == null)
			{
				indexReader = DirectoryReader.Open(directory);
			}
			else
			{
				indexReader = DirectoryReader.OpenIfChanged(indexReader);
			}
			indexSearcher = newSearcher(indexReader);
		}

		protected void verifyDocumentsIndexed(int numDocs)
		{
			Assert.AreEqual(numDocs, indexReader.NumDocs());
		}

		protected SearchResults executeQuery(Query query, int numDocs)
		{
			try
			{
				TopDocs topDocs = indexSearcher.Search(query, numDocs);

				var results = new List<SearchResult>();
				foreach (ScoreDoc scoreDoc in topDocs.scoreDocs)
				{
					results.Add(new SearchResult(scoreDoc.score, indexSearcher.Doc(scoreDoc.doc)));
				}
				return new SearchResults(topDocs.totalHits, results);
			}
			catch (IOException ioe)
			{
				throw new Exception("IOException thrown while executing query", ioe);
			}
		}

		// ================================================= Inner Classes =================================================

		protected class SearchResults
		{

			public int numFound;
			public List<SearchResult> results;

			public SearchResults(int numFound, List<SearchResult> results)
			{
				this.numFound = numFound;
				this.results = results;
			}
		}

		protected class SearchResult
		{

			public float score;
			public Document document;

			public SearchResult(float score, Document document)
			{
				this.score = score;
				this.document = document;
			}
		}

	}
}
