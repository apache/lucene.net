﻿namespace org.apache.lucene.analysis.en
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

	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	/// <summary>
	/// Transforms the token stream as per the Porter stemming algorithm.
	///    Note: the input to the stemming filter must already be in lower case,
	///    so you will need to use LowerCaseFilter or LowerCaseTokenizer farther
	///    down the Tokenizer chain in order for this to work properly!
	///    <P>
	///    To use this filter with other analyzers, you'll want to write an
	///    Analyzer class that sets up the TokenStream chain as you want it.
	///    To use this with LowerCaseTokenizer, for example, you'd write an
	///    analyzer like this:
	///    <P>
	///    <PRE class="prettyprint">
	///    class MyAnalyzer extends Analyzer {
	///      {@literal @Override}
	///      protected TokenStreamComponents createComponents(String fieldName, Reader reader) {
	///        Tokenizer source = new LowerCaseTokenizer(version, reader);
	///        return new TokenStreamComponents(source, new PorterStemFilter(source));
	///      }
	///    }
	///    </PRE>
	///    <para>
	///    Note: This filter is aware of the <seealso cref="KeywordAttribute"/>. To prevent
	///    certain terms from being passed to the stemmer
	///    <seealso cref="KeywordAttribute#isKeyword()"/> should be set to <code>true</code>
	///    in a previous <seealso cref="TokenStream"/>.
	/// 
	///    Note: For including the original term as well as the stemmed version, see
	///   <seealso cref="org.apache.lucene.analysis.miscellaneous.KeywordRepeatFilterFactory"/>
	///    </para>
	/// </summary>
	public sealed class PorterStemFilter : TokenFilter
	{
	  private readonly PorterStemmer stemmer = new PorterStemmer();
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

	  public PorterStemFilter(TokenStream @in) : base(@in)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (!input.incrementToken())
		{
		  return false;
		}

		if ((!keywordAttr.Keyword) && stemmer.stem(termAtt.buffer(), 0, termAtt.length()))
		{
		  termAtt.copyBuffer(stemmer.ResultBuffer, 0, stemmer.ResultLength);
		}
		return true;
	  }
	}

}