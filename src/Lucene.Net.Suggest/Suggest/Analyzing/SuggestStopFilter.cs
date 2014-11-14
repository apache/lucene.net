﻿using System.Diagnostics;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Search.Suggest.Analyzing
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
    /// Like <seealso cref="StopFilter"/> except it will not remove the
    ///  last token if that token was not followed by some token
    ///  separator.  For example, a query 'find the' would
    ///  preserve the 'the' since it was not followed by a space or
    ///  punctuation or something, and mark it KEYWORD so future
    ///  stemmers won't touch it either while a query like "find
    ///  the popsicle' would remove 'the' as a stopword.
    /// 
    ///  <para>Normally you'd use the ordinary <seealso cref="StopFilter"/>
    ///  in your indexAnalyzer and then this class in your
    ///  queryAnalyzer, when using one of the analyzing suggesters. 
    /// </para>
    /// </summary>

    public sealed class SuggestStopFilter : TokenFilter
    {

        private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
        private readonly PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
        private readonly KeywordAttribute keywordAtt = addAttribute(typeof(KeywordAttribute));
        private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
        private readonly CharArraySet stopWords;

        private State endState;

        /// <summary>
        /// Sole constructor. </summary>
        public SuggestStopFilter(TokenStream input, CharArraySet stopWords)
            : base(input)
        {
            this.stopWords = stopWords;
        }

        public override void Reset()
        {
            base.Reset();
            endState = null;
        }

        public override void End()
        {
            if (endState == null)
            {
                base.End();
            }
            else
            {
                // NOTE: we already called .end() from our .next() when
                // the stream was complete, so we do not call
                // super.end() here
                RestoreState(endState);
            }
        }

        public override bool IncrementToken()
        {
            if (endState != null)
            {
                return false;
            }

            if (!input.IncrementToken())
            {
                return false;
            }

            int skippedPositions = 0;
            while (true)
            {
                if (stopWords.Contains(termAtt.Buffer(), 0, termAtt.Length))
                {
                    int posInc = posIncAtt.PositionIncrement;
                    int endOffset = offsetAtt.EndOffset();
                    // This token may be a stopword, if it's not end:
                    State sav = CaptureState();
                    if (input.IncrementToken())
                    {
                        // It was a stopword; skip it
                        skippedPositions += posInc;
                    }
                    else
                    {
                        ClearAttributes();
                        input.End();
                        endState = CaptureState();
                        int finalEndOffset = offsetAtt.EndOffset();
                        Debug.Assert(finalEndOffset >= endOffset);
                        if (finalEndOffset > endOffset)
                        {
                            // OK there was a token separator after the
                            // stopword, so it was a stopword
                            return false;
                        }
                        else
                        {
                            // No token separator after final token that
                            // looked like a stop-word; don't filter it:
                            RestoreState(sav);
                            posIncAtt.PositionIncrement = skippedPositions + posIncAtt.PositionIncrement;
                            keywordAtt.Keyword = true;
                            return true;
                        }
                    }
                }
                else
                {
                    // Not a stopword; return the current token:
                    posIncAtt.PositionIncrement = skippedPositions + posIncAtt.PositionIncrement;
                    return true;
                }
            }
        }
    }
}