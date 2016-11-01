﻿using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Hunspell
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
    /// TokenFilter that uses hunspell affix rules and words to stem tokens.  Since hunspell supports a word having multiple
    /// stems, this filter can emit multiple tokens for each consumed token
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
    /// @lucene.experimental
    /// </summary>
    public sealed class HunspellStemFilter : TokenFilter
    {

        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IKeywordAttribute keywordAtt;
        private readonly Stemmer stemmer;

        private List<CharsRef> buffer;
        private State savedState;

        private readonly bool dedup;
        private readonly bool longestOnly;

        /// <summary>
        /// Create a <seealso cref="HunspellStemFilter"/> outputting all possible stems. </summary>
        ///  <seealso cref= #HunspellStemFilter(TokenStream, Dictionary, boolean)  </seealso>
        public HunspellStemFilter(TokenStream input, Dictionary dictionary)
              : this(input, dictionary, true)
        {
        }

        /// <summary>
        /// Create a <seealso cref="HunspellStemFilter"/> outputting all possible stems. </summary>
        ///  <seealso cref= #HunspellStemFilter(TokenStream, Dictionary, boolean, boolean)  </seealso>
        public HunspellStemFilter(TokenStream input, Dictionary dictionary, bool dedup)
              : this(input, dictionary, dedup, false)
        {
        }

        /// <summary>
        /// Creates a new HunspellStemFilter that will stem tokens from the given TokenStream using affix rules in the provided
        /// Dictionary
        /// </summary>
        /// <param name="input"> TokenStream whose tokens will be stemmed </param>
        /// <param name="dictionary"> HunspellDictionary containing the affix rules and words that will be used to stem the tokens </param>
        /// <param name="longestOnly"> true if only the longest term should be output. </param>
        public HunspellStemFilter(TokenStream input, Dictionary dictionary, bool dedup, bool longestOnly) :
              base(input)
        {
            this.dedup = dedup && longestOnly == false; // don't waste time deduping if longestOnly is set
            this.stemmer = new Stemmer(dictionary);
            this.longestOnly = longestOnly;
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (buffer != null && buffer.Count > 0)
            {
                CharsRef nextStem = buffer[0];
                buffer.RemoveAt(0);
                RestoreState(savedState);
                posIncAtt.PositionIncrement = 0;
                termAtt.SetEmpty().Append(nextStem);
                return true;
            }

            if (!input.IncrementToken())
            {
                return false;
            }

            if (keywordAtt.Keyword)
            {
                return true;
            }

            buffer = new List<CharsRef>(dedup ? stemmer.UniqueStems(termAtt.Buffer(), termAtt.Length) : stemmer.Stem(termAtt.Buffer(), termAtt.Length));

            if (buffer.Count == 0) // we do not know this word, return it unchanged
            {
                return true;
            }

            if (longestOnly && buffer.Count > 1)
            {
                buffer.Sort(lengthComparator);
            }

            CharsRef stem = buffer[0];
            buffer.RemoveAt(0);
            termAtt.SetEmpty().Append(stem);

            if (longestOnly)
            {
                buffer.Clear();
            }
            else
            {
                if (buffer.Count > 0)
                {
                    savedState = CaptureState();
                }
            }

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            buffer = null;
        }

        internal static readonly IComparer<CharsRef> lengthComparator = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<CharsRef>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(CharsRef o1, CharsRef o2)
            {
                if (o2.Length == o1.Length)
                {
                    // tie break on text
                    return o2.CompareTo(o1);
                }
                else
                {
                    return o2.Length < o1.Length ? -1 : 1;
                }
            }
        }
    }
}