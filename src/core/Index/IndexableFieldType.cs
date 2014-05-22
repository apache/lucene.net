namespace Lucene.Net.Index
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

	using Analyzer = Lucene.Net.Analysis.Analyzer; // javadocs
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

	/// <summary>
	/// Describes the properties of a field.
	/// @lucene.experimental 
	/// </summary>
	public interface IndexableFieldType
	{

	  /// <summary>
	  /// True if this field should be indexed (inverted) </summary>
	  bool Indexed();

	  /// <summary>
	  /// True if the field's value should be stored </summary>
	  bool Stored();

	  /// <summary>
	  /// True if this field's value should be analyzed by the
	  /// <seealso cref="Analyzer"/>.
	  /// <p>
	  /// this has no effect if <seealso cref="#indexed()"/> returns false.
	  /// </summary>
	  bool Tokenized();

	  /// <summary>
	  /// True if this field's indexed form should be also stored 
	  /// into term vectors.
	  /// <p>
	  /// this builds a miniature inverted-index for this field which
	  /// can be accessed in a document-oriented way from 
	  /// <seealso cref="IndexReader#getTermVector(int,String)"/>.
	  /// <p>
	  /// this option is illegal if <seealso cref="#indexed()"/> returns false.
	  /// </summary>
	  bool StoreTermVectors();

	  /// <summary>
	  /// True if this field's token character offsets should also
	  /// be stored into term vectors.
	  /// <p>
	  /// this option is illegal if term vectors are not enabled for the field
	  /// (<seealso cref="#storeTermVectors()"/> is false)
	  /// </summary>
	  bool StoreTermVectorOffsets();

	  /// <summary>
	  /// True if this field's token positions should also be stored
	  /// into the term vectors.
	  /// <p>
	  /// this option is illegal if term vectors are not enabled for the field
	  /// (<seealso cref="#storeTermVectors()"/> is false). 
	  /// </summary>
	  bool StoreTermVectorPositions();

	  /// <summary>
	  /// True if this field's token payloads should also be stored
	  /// into the term vectors.
	  /// <p>
	  /// this option is illegal if term vector positions are not enabled 
	  /// for the field (<seealso cref="#storeTermVectors()"/> is false).
	  /// </summary>
	  bool StoreTermVectorPayloads();

	  /// <summary>
	  /// True if normalization values should be omitted for the field.
	  /// <p>
	  /// this saves memory, but at the expense of scoring quality (length normalization
	  /// will be disabled), and if you omit norms, you cannot use index-time boosts. 
	  /// </summary>
	  bool OmitNorms();

	  /// <summary>
	  /// <seealso cref="IndexOptions"/>, describing what should be
	  /// recorded into the inverted index 
	  /// </summary>
	  IndexOptions IndexOptions();

	  /// <summary>
	  /// DocValues <seealso cref="DocValuesType"/>: if non-null then the field's value
	  /// will be indexed into docValues.
	  /// </summary>
	  DocValuesType DocValueType();
	}

}