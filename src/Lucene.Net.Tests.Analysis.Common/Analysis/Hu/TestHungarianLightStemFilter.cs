﻿using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Hu
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

    /// <summary>
    /// Simple tests for <seealso cref="HungarianLightStemFilter"/>
    /// </summary>
    public class TestHungarianLightStemFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            public AnalyzerAnonymousInnerClassHelper()
            {
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(source, new HungarianLightStemFilter(source));
            }
        }

        /// <summary>
        /// Test against a vocabulary from the reference impl </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("hulighttestdata.zip"), "hulight.txt");
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("babakocsi"), false);
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, exclusionSet);
            CheckOneTerm(a, "babakocsi", "babakocsi");
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestHungarianLightStemFilter outerInstance;

            private CharArraySet exclusionSet;

            public AnalyzerAnonymousInnerClassHelper2(TestHungarianLightStemFilter outerInstance, CharArraySet exclusionSet)
            {
                this.outerInstance = outerInstance;
                this.exclusionSet = exclusionSet;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new HungarianLightStemFilter(sink));
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestHungarianLightStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper3(TestHungarianLightStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new HungarianLightStemFilter(tokenizer));
            }
        }
    }
}