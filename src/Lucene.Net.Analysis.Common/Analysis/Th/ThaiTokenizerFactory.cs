﻿using System.Collections.Generic;

namespace org.apache.lucene.analysis.th
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


	using TokenizerFactory = org.apache.lucene.analysis.util.TokenizerFactory;
	using AttributeSource = org.apache.lucene.util.AttributeSource;

	/// <summary>
	/// Factory for <seealso cref="ThaiTokenizer"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_thai" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.ThaiTokenizerFactory"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class ThaiTokenizerFactory : TokenizerFactory
	{

	  /// <summary>
	  /// Creates a new ThaiTokenizerFactory </summary>
	  public ThaiTokenizerFactory(IDictionary<string, string> args) : base(args)
	  {
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override Tokenizer create(AttributeSource.AttributeFactory factory, Reader reader)
	  {
		return new ThaiTokenizer(factory, reader);
	  }
	}


}