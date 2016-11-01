﻿using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Ckb
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
    /// A <seealso cref="TokenFilter"/> that applies <seealso cref="SoraniNormalizer"/> to normalize the
    /// orthography.
    /// </summary>
    public sealed class SoraniNormalizationFilter : TokenFilter
    {
        private readonly SoraniNormalizer normalizer = new SoraniNormalizer();
        private readonly ICharTermAttribute termAtt;

        public SoraniNormalizationFilter(TokenStream input)
              : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                int newlen = normalizer.normalize(termAtt.Buffer(), termAtt.Length);
                termAtt.Length = newlen;
                return true;
            }
            return false;
        }
    }
}