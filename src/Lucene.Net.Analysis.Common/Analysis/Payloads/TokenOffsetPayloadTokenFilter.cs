﻿namespace org.apache.lucene.analysis.payloads
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


	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// <summary>
	/// Adds the <seealso cref="OffsetAttribute#startOffset()"/>
	/// and <seealso cref="OffsetAttribute#endOffset()"/>
	/// First 4 bytes are the start
	/// 
	/// 
	/// </summary>
	public class TokenOffsetPayloadTokenFilter : TokenFilter
	{
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PayloadAttribute payAtt = addAttribute(typeof(PayloadAttribute));

	  public TokenOffsetPayloadTokenFilter(TokenStream input) : base(input)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  sbyte[] data = new sbyte[8];
		  PayloadHelper.encodeInt(offsetAtt.startOffset(), data, 0);
		  PayloadHelper.encodeInt(offsetAtt.endOffset(), data, 4);
		  BytesRef payload = new BytesRef(data);
		  payAtt.Payload = payload;
		  return true;
		}
		else
		{
		return false;
		}
	  }
	}
}