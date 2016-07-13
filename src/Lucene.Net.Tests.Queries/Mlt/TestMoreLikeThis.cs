﻿using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Tests.Queries.Mlt
{
    public class TestMoreLikeThis : LuceneTestCase
    {
        private Directory directory;
        private IndexReader reader;
        private IndexSearcher searcher;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig());

            // Add series of docs with specific information for MoreLikeThis
            AddDoc(writer, "lucene");
            AddDoc(writer, "lucene release");

            reader = writer.Reader;
            writer.Dispose();
            searcher = NewSearcher(reader);
        }
        
        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }
        
        private void AddDoc(RandomIndexWriter writer, string text)
        {
            Document doc = new Document();
            doc.Add(NewTextField("text", text, Field.Store.YES));
            writer.AddDocument(doc);
        }
        
        [Test]
        public void TestBoostFactor()
        {
            IDictionary<string, float?> originalValues = OriginalValues;

            MoreLikeThis mlt = new MoreLikeThis(reader);
            mlt.Analyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            mlt.MinDocFreq = 1;
            mlt.MinTermFreq = 1;
            mlt.MinWordLen = 1;
            mlt.FieldNames = new[] { "text" };
            mlt.Boost = true;

            // this mean that every term boost factor will be multiplied by this
            // number
            float boostFactor = 5;
            mlt.BoostFactor = boostFactor;

            BooleanQuery query = (BooleanQuery)mlt.Like(new StringReader("lucene release"), "text");
            IList<BooleanClause> clauses = query.Clauses;

            assertEquals("Expected " + originalValues.Count + " clauses.", originalValues.Count, clauses.Count);

            foreach (BooleanClause clause in clauses)
            {
                TermQuery tq = (TermQuery)clause.Query;
                float? termBoost = originalValues[tq.Term.Text()];
                assertNotNull("Expected term " + tq.Term.Text(), termBoost);

                float totalBoost = (float) (termBoost * boostFactor);
                assertEquals("Expected boost of " + totalBoost + " for term '" + tq.Term.Text() + "' got " + tq.Boost, totalBoost, tq.Boost, 0.0001);
            }
        }
        
        private IDictionary<string, float?> OriginalValues
        {
            get
            {
                IDictionary<string, float?> originalValues = new Dictionary<string, float?>();
                MoreLikeThis mlt = new MoreLikeThis(reader);
                mlt.Analyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
                mlt.MinDocFreq = 1;
                mlt.MinTermFreq = 1;
                mlt.MinWordLen = 1;
                mlt.FieldNames = new[] { "text" };
                mlt.Boost = true;
                BooleanQuery query = (BooleanQuery)mlt.Like(new StringReader("lucene release"), "text");
                IList<BooleanClause> clauses = query.Clauses;

                foreach (BooleanClause clause in clauses)
                {
                    TermQuery tq = (TermQuery)clause.Query;
                    originalValues[tq.Term.Text()] = tq.Boost;
                }
                return originalValues;
            }
        }

        // LUCENE-3326
        [Test]
        public void TestMultiFields()
        {
            MoreLikeThis mlt = new MoreLikeThis(reader);
            mlt.Analyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            mlt.MinDocFreq = 1;
            mlt.MinTermFreq = 1;
            mlt.MinWordLen = 1;
            mlt.FieldNames = new[] { "text", "foobar" };
            mlt.Like(new StringReader("this is a test"), "foobar");
        }

        /// <summary>
        /// just basic equals/hashcode etc
        /// </summary>
        [Test]
        public void TestMoreLikeThisQuery()
        {
            Query query = new MoreLikeThisQuery("this is a test", new[] { "text" }, new MockAnalyzer(Random()), "text");
            QueryUtils.Check(Random(), query, searcher, NewSearcher);
        }

        // TODO: add tests for the MoreLikeThisQuery
    }
}