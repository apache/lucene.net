﻿using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.En
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
    /// A high-performance kstem filter for english.
    /// <p/>
    /// See <a href="http://ciir.cs.umass.edu/pubfiles/ir-35.pdf">
    /// "Viewing Morphology as an Inference Process"</a>
    /// (Krovetz, R., Proceedings of the Sixteenth Annual International ACM SIGIR
    /// Conference on Research and Development in Information Retrieval, 191-203, 1993).
    /// <p/>
    /// All terms must already be lowercased for this filter to work correctly.
    /// 
    /// <para>
    /// Note: This filter is aware of the <seealso cref="KeywordAttribute"/>. To prevent
    /// certain terms from being passed to the stemmer
    /// <seealso cref="KeywordAttribute#isKeyword()"/> should be set to <code>true</code>
    /// in a previous <seealso cref="TokenStream"/>.
    /// 
    /// Note: For including the original term as well as the stemmed version, see
    /// <seealso cref="org.apache.lucene.analysis.miscellaneous.KeywordRepeatFilterFactory"/>
    /// </para>
    /// 
    /// 
    /// </summary>
    public sealed class KStemFilter : TokenFilter
    {
        private readonly KStemmer stemmer = new KStemmer();
        private readonly ICharTermAttribute termAttribute;
        private readonly IKeywordAttribute keywordAtt;

        public KStemFilter(TokenStream @in) : base(@in)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        /// <summary>
        /// Returns the next, stemmed, input Token. </summary>
        ///  <returns> The stemmed form of a token. </returns>
        ///  <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            char[] term = termAttribute.Buffer();
            int len = termAttribute.Length;
            if ((!keywordAtt.Keyword) && stemmer.Stem(term, len))
            {
                termAttribute.SetEmpty().Append(stemmer.AsCharSequence());
            }

            return true;
        }
    }
}