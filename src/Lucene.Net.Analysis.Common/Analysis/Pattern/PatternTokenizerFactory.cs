﻿using System.Collections.Generic;
using TokenizerFactory = Lucene.Net.Analysis.Util.TokenizerFactory;

namespace org.apache.lucene.analysis.pattern
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


	using TokenizerFactory = TokenizerFactory;
	using AttributeFactory = org.apache.lucene.util.AttributeSource.AttributeFactory;

	/// <summary>
	/// Factory for <seealso cref="PatternTokenizer"/>.
	/// This tokenizer uses regex pattern matching to construct distinct tokens
	/// for the input stream.  It takes two arguments:  "pattern" and "group".
	/// <p/>
	/// <ul>
	/// <li>"pattern" is the regular expression.</li>
	/// <li>"group" says which group to extract into tokens.</li>
	///  </ul>
	/// <para>
	/// group=-1 (the default) is equivalent to "split".  In this case, the tokens will
	/// be equivalent to the output from (without empty tokens):
	/// <seealso cref="String#split(java.lang.String)"/>
	/// </para>
	/// <para>
	/// Using group >= 0 selects the matching group as the token.  For example, if you have:<br/>
	/// <pre>
	///  pattern = \'([^\']+)\'
	///  group = 0
	///  input = aaa 'bbb' 'ccc'
	/// </pre>
	/// the output will be two tokens: 'bbb' and 'ccc' (including the ' marks).  With the same input
	/// but using group=1, the output would be: bbb and ccc (no ' marks)
	/// </para>
	/// <para>NOTE: This Tokenizer does not output tokens that are of zero length.</para>
	/// 
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptn" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.PatternTokenizerFactory" pattern="\'([^\']+)\'" group="1"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre> 
	/// </summary>
	/// <seealso cref= PatternTokenizer
	/// @since solr1.2 </seealso>
	public class PatternTokenizerFactory : TokenizerFactory
	{
	  public const string PATTERN = "pattern";
	  public const string GROUP = "group";

	  protected internal readonly Pattern pattern;
	  protected internal readonly int group;

	  /// <summary>
	  /// Creates a new PatternTokenizerFactory </summary>
	  public PatternTokenizerFactory(IDictionary<string, string> args) : base(args)
	  {
		pattern = getPattern(args, PATTERN);
		group = getInt(args, GROUP, -1);
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  /// <summary>
	  /// Split the input using configured pattern
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override public PatternTokenizer create(final org.apache.lucene.util.AttributeSource.AttributeFactory factory, final java.io.Reader in)
	  public override PatternTokenizer create(AttributeFactory factory, Reader @in)
	  {
		return new PatternTokenizer(factory, @in, pattern, group);
	  }
	}

}