﻿namespace org.apache.lucene.analysis.no
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

	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using KeywordAttribute = org.apache.lucene.analysis.tokenattributes.KeywordAttribute;

	/// <summary>
	/// A <seealso cref="TokenFilter"/> that applies <seealso cref="NorwegianLightStemmer"/> to stem Norwegian
	/// words.
	/// <para>
	/// To prevent terms from being stemmed use an instance of
	/// <seealso cref="SetKeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
	/// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
	/// </para>
	/// </summary>
	public sealed class NorwegianLightStemFilter : TokenFilter
	{
	  private readonly NorwegianLightStemmer stemmer;
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly KeywordAttribute keywordAttr = addAttribute(typeof(KeywordAttribute));

	  /// <summary>
	  /// Calls {@link #NorwegianLightStemFilter(TokenStream, int) 
	  /// NorwegianLightStemFilter(input, BOKMAAL)}
	  /// </summary>
	  public NorwegianLightStemFilter(TokenStream input) : this(input, NorwegianLightStemmer.BOKMAAL)
	  {
	  }

	  /// <summary>
	  /// Creates a new NorwegianLightStemFilter </summary>
	  /// <param name="flags"> set to <seealso cref="NorwegianLightStemmer#BOKMAAL"/>, 
	  ///                     <seealso cref="NorwegianLightStemmer#NYNORSK"/>, or both. </param>
	  public NorwegianLightStemFilter(TokenStream input, int flags) : base(input)
	  {
		stemmer = new NorwegianLightStemmer(flags);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  if (!keywordAttr.Keyword)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newlen = stemmer.stem(termAtt.buffer(), termAtt.length());
			int newlen = stemmer.stem(termAtt.buffer(), termAtt.length());
			termAtt.Length = newlen;
		  }
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	}

}