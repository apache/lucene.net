namespace Lucene.Net.Analysis.Tokenattributes
{

    using Lucene.Net.Util;
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

    using Attribute = Lucene.Net.Util.Attribute;

	/// <summary>
	/// Determines how many positions this
	///  token spans.  Very few analyzer components actually
	///  produce this attribute, and indexing ignores it, but
	///  it's useful to express the graph structure naturally
	///  produced by decompounding, word splitting/joining,
	///  synonym filtering, etc.
	/// 
	/// <p>NOTE: this is optional, and most analyzers
	///  don't change the default value (1). 
	/// </summary>

	public interface IPositionLengthAttribute : IAttribute
	{
	  /// <summary>
	  /// Set the position length of this Token.
	  /// <p>
	  /// The default value is one. </summary>
	  /// <param name="positionLength"> how many positions this token
	  ///  spans. </param>
	  /// <exception cref="IllegalArgumentException"> if <code>positionLength</code> 
	  ///         is zero or negative. </exception>
	  /// <seealso cref= #getPositionLength() </seealso>
	  int PositionLength {set;get;}

	}


}