﻿using Lucene.Net.Analysis.Util;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.No
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
    /// Factory for <seealso cref="NorwegianLightStemFilter"/>.
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_svlgtstem" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
    ///     &lt;filter class="solr.NorwegianLightStemFilterFactory" variant="nb"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// </summary>
    public class NorwegianLightStemFilterFactory : TokenFilterFactory
    {

        private readonly int flags;

        /// <summary>
        /// Creates a new NorwegianLightStemFilterFactory </summary>
        public NorwegianLightStemFilterFactory(IDictionary<string, string> args)
              : base(args)
        {
            string variant = Get(args, "variant");
            if (variant == null || "nb".Equals(variant))
            {
                flags = NorwegianLightStemmer.BOKMAAL;
            }
            else if ("nn".Equals(variant))
            {
                flags = NorwegianLightStemmer.NYNORSK;
            }
            else if ("no".Equals(variant))
            {
                flags = NorwegianLightStemmer.BOKMAAL | NorwegianLightStemmer.NYNORSK;
            }
            else
            {
                throw new System.ArgumentException("invalid variant: " + variant);
            }
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new NorwegianLightStemFilter(input, flags);
        }
    }
}