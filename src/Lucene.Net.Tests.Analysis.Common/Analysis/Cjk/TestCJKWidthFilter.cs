﻿using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Cjk
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
    /// Tests for <seealso cref="CJKWidthFilter"/>
    /// </summary>
    public class TestCJKWidthFilter : BaseTokenStreamTestCase
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
                return new TokenStreamComponents(source, new CJKWidthFilter(source));
            }
        }

        /// <summary>
        /// Full-width ASCII forms normalized to half-width (basic latin)
        /// </summary>
        [Test]
        public virtual void TestFullWidthASCII()
        {
            AssertAnalyzesTo(analyzer, "Ｔｅｓｔ １２３４", new string[] { "Test", "1234" }, new int[] { 0, 5 }, new int[] { 4, 9 });
        }

        /// <summary>
        /// Half-width katakana forms normalized to standard katakana.
        /// A bit trickier in some cases, since half-width forms are decomposed
        /// and voice marks need to be recombined with a preceding base form. 
        /// </summary>
        [Test]
        public virtual void TestHalfWidthKana()
        {
            AssertAnalyzesTo(analyzer, "ｶﾀｶﾅ", new string[] { "カタカナ" });
            AssertAnalyzesTo(analyzer, "ｳﾞｨｯﾂ", new string[] { "ヴィッツ" });
            AssertAnalyzesTo(analyzer, "ﾊﾟﾅｿﾆｯｸ", new string[] { "パナソニック" });
        }

        [Test]
        public virtual void TestRandomData()
        {
            CheckRandomData(Random(), analyzer, 1000 * RANDOM_MULTIPLIER);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestCJKWidthFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestCJKWidthFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new CJKWidthFilter(tokenizer));
            }
        }
    }
}