namespace Lucene.Net.Codecs
{

    using System;
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


    using Fields = Lucene.Net.Index.Fields;

	/// <summary>
	/// Abstract API that produces terms, doc, freq, prox, offset and
	///  payloads postings.  
	/// 
	/// @lucene.experimental
	/// </summary>

	public abstract class FieldsProducer : Fields, IDisposable
	{
	  /// <summary>
	  /// Sole constructor. (For invocation by subclass 
	  ///  constructors, typically implicit.) 
	  /// </summary>
	  protected internal FieldsProducer()
	  {
	  }

	  public abstract void Dispose();

	  /// <summary>
	  /// Returns approximate RAM bytes used </summary>
	  public abstract long RamBytesUsed();

	  /// <summary>
	  /// Checks consistency of this reader.
	  /// <p>
	  /// Note that this may be costly in terms of I/O, e.g. 
	  /// may involve computing a checksum value against large data files.
	  /// @lucene.internal
	  /// </summary>
	  public abstract void CheckIntegrity();
	}

}