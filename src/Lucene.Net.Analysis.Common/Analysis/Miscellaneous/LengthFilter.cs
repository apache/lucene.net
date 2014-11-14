﻿using System;
using FilteringTokenFilter = Lucene.Net.Analysis.Util.FilteringTokenFilter;

namespace org.apache.lucene.analysis.miscellaneous
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

	using FilteringTokenFilter = FilteringTokenFilter;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Removes words that are too long or too short from the stream.
	/// <para>
	/// Note: Length is calculated as the number of UTF-16 code units.
	/// </para>
	/// </summary>
	public sealed class LengthFilter : FilteringTokenFilter
	{

	  private readonly int min;
	  private readonly int max;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

	  /// @deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4. 
	  [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
	  public LengthFilter(Version version, bool enablePositionIncrements, TokenStream @in, int min, int max) : base(version, enablePositionIncrements, @in)
	  {
		if (min < 0)
		{
		  throw new System.ArgumentException("minimum length must be greater than or equal to zero");
		}
		if (min > max)
		{
		  throw new System.ArgumentException("maximum length must not be greater than minimum length");
		}
		this.min = min;
		this.max = max;
	  }

	  /// <summary>
	  /// Create a new <seealso cref="LengthFilter"/>. This will filter out tokens whose
	  /// <seealso cref="CharTermAttribute"/> is either too short (<seealso cref="CharTermAttribute#length()"/>
	  /// &lt; min) or too long (<seealso cref="CharTermAttribute#length()"/> &gt; max). </summary>
	  /// <param name="version"> the Lucene match version </param>
	  /// <param name="in">      the <seealso cref="TokenStream"/> to consume </param>
	  /// <param name="min">     the minimum length </param>
	  /// <param name="max">     the maximum length </param>
	  public LengthFilter(Version version, TokenStream @in, int min, int max) : base(version, @in)
	  {
		if (min < 0)
		{
		  throw new System.ArgumentException("minimum length must be greater than or equal to zero");
		}
		if (min > max)
		{
		  throw new System.ArgumentException("maximum length must not be greater than minimum length");
		}
		this.min = min;
		this.max = max;
	  }

	  public override bool accept()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int len = termAtt.length();
		int len = termAtt.length();
		return (len >= min && len <= max);
	  }
	}

}