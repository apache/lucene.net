namespace Lucene.Net.Codecs.Lucene40
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

	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using SegmentInfo = Lucene.Net.Index.SegmentInfo;
	using DataOutput = Lucene.Net.Store.DataOutput; // javadocs
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;

	/// <summary>
	/// Lucene 4.0 Stored Fields Format.
	/// <p>Stored fields are represented by two files:</p>
	/// <ol>
	/// <li><a name="field_index" id="field_index"></a>
	/// <p>The field index, or <tt>.fdx</tt> file.</p>
	/// <p>this is used to find the location within the field data file of the fields
	/// of a particular document. Because it contains fixed-length data, this file may
	/// be easily randomly accessed. The position of document <i>n</i> 's field data is
	/// the <seealso cref="DataOutput#writeLong Uint64"/> at <i>n*8</i> in this file.</p>
	/// <p>this contains, for each document, a pointer to its field data, as
	/// follows:</p>
	/// <ul>
	/// <li>FieldIndex (.fdx) --&gt; &lt;Header&gt;, &lt;FieldValuesPosition&gt; <sup>SegSize</sup></li>
	/// <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	/// <li>FieldValuesPosition --&gt; <seealso cref="DataOutput#writeLong Uint64"/></li>
	/// </ul>
	/// </li>
	/// <li>
	/// <p><a name="field_data" id="field_data"></a>The field data, or <tt>.fdt</tt> file.</p>
	/// <p>this contains the stored fields of each document, as follows:</p>
	/// <ul>
	/// <li>FieldData (.fdt) --&gt; &lt;Header&gt;, &lt;DocFieldData&gt; <sup>SegSize</sup></li>
	/// <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	/// <li>DocFieldData --&gt; FieldCount, &lt;FieldNum, Bits, Value&gt;
	/// <sup>FieldCount</sup></li>
	/// <li>FieldCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
	/// <li>FieldNum --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
	/// <li>Bits --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
	/// <ul>
	/// <li>low order bit reserved.</li>
	/// <li>second bit is one for fields containing binary data</li>
	/// <li>third bit reserved.</li>
	/// <li>4th to 6th bit (mask: 0x7&lt;&lt;3) define the type of a numeric field:
	/// <ul>
	/// <li>all bits in mask are cleared if no numeric field at all</li>
	/// <li>1&lt;&lt;3: Value is Int</li>
	/// <li>2&lt;&lt;3: Value is Long</li>
	/// <li>3&lt;&lt;3: Value is Int as Float (as of <seealso cref="Float#intBitsToFloat(int)"/></li>
	/// <li>4&lt;&lt;3: Value is Long as Double (as of <seealso cref="Double#longBitsToDouble(long)"/></li>
	/// </ul>
	/// </li>
	/// </ul>
	/// <li>Value --&gt; String | BinaryValue | Int | Long (depending on Bits)</li>
	/// <li>BinaryValue --&gt; ValueSize, &lt;<seealso cref="DataOutput#writeByte Byte"/>&gt;^ValueSize</li>
	/// <li>ValueSize --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
	/// </li>
	/// </ul>
	/// </ol>
	/// @lucene.experimental 
	/// </summary>
	public class Lucene40StoredFieldsFormat : StoredFieldsFormat
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene40StoredFieldsFormat()
	  {
	  }

	  public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
	  {
		return new Lucene40StoredFieldsReader(directory, si, fn, context);
	  }

	  public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
	  {
		return new Lucene40StoredFieldsWriter(directory, si.Name, context);
	  }
	}

}