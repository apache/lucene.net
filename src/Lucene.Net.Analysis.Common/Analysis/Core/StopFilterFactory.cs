﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Core
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
    // jdocs


    /// <summary>
    /// Factory for <seealso cref="StopFilter"/>.
    /// 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_stop" class="solr.TextField" positionIncrementGap="100" autoGeneratePhraseQueries="true"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.StopFilterFactory" ignoreCase="true"
    ///             words="stopwords.txt" format="wordset" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// 
    /// <para>
    /// All attributes are optional:
    /// </para>
    /// <ul>
    ///  <li><code>ignoreCase</code> defaults to <code>false</code></li>
    ///  <li><code>words</code> should be the name of a stopwords file to parse, if not 
    ///      specified the factory will use <seealso cref="StopAnalyzer#ENGLISH_STOP_WORDS_SET"/>
    ///  </li>
    ///  <li><code>format</code> defines how the <code>words</code> file will be parsed, 
    ///      and defaults to <code>wordset</code>.  If <code>words</code> is not specified, 
    ///      then <code>format</code> must not be specified.
    ///  </li>
    /// </ul>
    /// <para>
    /// The valid values for the <code>format</code> option are:
    /// </para>
    /// <ul>
    ///  <li><code>wordset</code> - This is the default format, which supports one word per 
    ///      line (including any intra-word whitespace) and allows whole line comments 
    ///      begining with the "#" character.  Blank lines are ignored.  See 
    ///      <seealso cref="WordlistLoader#getLines WordlistLoader.getLines"/> for details.
    ///  </li>
    ///  <li><code>snowball</code> - This format allows for multiple words specified on each 
    ///      line, and trailing comments may be specified using the vertical line ("&#124;"). 
    ///      Blank lines are ignored.  See 
    ///      <seealso cref="WordlistLoader#getSnowballWordSet WordlistLoader.getSnowballWordSet"/> 
    ///      for details.
    ///  </li>
    /// </ul>
    /// </summary>
    public class StopFilterFactory : TokenFilterFactory, ResourceLoaderAware
    {
        public const string FORMAT_WORDSET = "wordset";
        public const string FORMAT_SNOWBALL = "snowball";

        private CharArraySet stopWords;
        private readonly string stopWordFiles;
        private readonly string format;
        private readonly bool ignoreCase;
        private readonly bool enablePositionIncrements;

        /// <summary>
        /// Creates a new StopFilterFactory </summary>
        public StopFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            assureMatchVersion();
            stopWordFiles = get(args, "words");
            format = get(args, "format", (null == stopWordFiles ? null : FORMAT_WORDSET));
            ignoreCase = getBoolean(args, "ignoreCase", false);
            enablePositionIncrements = getBoolean(args, "enablePositionIncrements", true);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public virtual void Inform(ResourceLoader loader)
        {
            if (stopWordFiles != null)
            {
                if (FORMAT_WORDSET.Equals(format, StringComparison.CurrentCultureIgnoreCase))
                {
                    stopWords = GetWordSet(loader, stopWordFiles, ignoreCase);
                }
                else if (FORMAT_SNOWBALL.Equals(format, StringComparison.CurrentCultureIgnoreCase))
                {
                    stopWords = getSnowballWordSet(loader, stopWordFiles, ignoreCase);
                }
                else
                {
                    throw new System.ArgumentException("Unknown 'format' specified for 'words' file: " + format);
                }
            }
            else
            {
                if (null != format)
                {
                    throw new System.ArgumentException("'format' can not be specified w/o an explicit 'words' file: " + format);
                }
                stopWords = new CharArraySet(luceneMatchVersion, StopAnalyzer.ENGLISH_STOP_WORDS_SET, ignoreCase);
            }
        }

        public virtual bool EnablePositionIncrements
        {
            get
            {
                return enablePositionIncrements;
            }
        }

        public virtual bool IgnoreCase
        {
            get
            {
                return ignoreCase;
            }
        }

        public virtual CharArraySet StopWords
        {
            get
            {
                return stopWords;
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            StopFilter stopFilter = new StopFilter(luceneMatchVersion, input, stopWords);
            stopFilter.EnablePositionIncrements = enablePositionIncrements;
            return stopFilter;
        }
    }
}