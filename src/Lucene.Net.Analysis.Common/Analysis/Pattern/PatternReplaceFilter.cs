﻿using Lucene.Net.Analysis.Tokenattributes;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Pattern
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
    /// A TokenFilter which applies a Pattern to each token in the stream,
    /// replacing match occurances with the specified replacement string.
    /// 
    /// <para>
    /// <b>Note:</b> Depending on the input and the pattern used and the input
    /// TokenStream, this TokenFilter may produce Tokens whose text is the empty
    /// string.
    /// </para>
    /// </summary>
    /// <seealso cref= Pattern </seealso>
    public sealed class PatternReplaceFilter : TokenFilter
    {
        private readonly string replacement;
        private readonly bool all;
        private readonly ICharTermAttribute termAtt;
        private readonly Regex pattern;

        /// <summary>
        /// Constructs an instance to replace either the first, or all occurances
        /// </summary>
        /// <param name="in"> the TokenStream to process </param>
        /// <param name="pattern"> the pattern (a <seealso cref="Regex"/> object) to apply to each Token </param>
        /// <param name="replacement"> the "replacement string" to substitute, if null a
        ///        blank string will be used. Note that this is not the literal
        ///        string that will be used, '$' and '\' have special meaning. </param>
        /// <param name="all"> if true, all matches will be replaced otherwise just the first match. </param>
        /// <seealso cref= Matcher#quoteReplacement </seealso>
        public PatternReplaceFilter(TokenStream @in, Regex pattern, string replacement, bool all)
              : base(@in)
        {
            this.replacement = (null == replacement) ? "" : replacement;
            this.all = all;
            this.pattern = pattern;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            string transformed = all ?
                    pattern.Replace(termAtt.ToString(), replacement) :
                    pattern.Replace(termAtt.ToString(), replacement, 1);
            termAtt.SetEmpty().Append(transformed);

            return true;
        }
    }
}