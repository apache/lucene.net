namespace Lucene.Net.Codecs.Lucene41
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

	using CompressingStoredFieldsFormat = Lucene.Net.Codecs.compressing.CompressingStoredFieldsFormat;
	using CompressingStoredFieldsIndexWriter = Lucene.Net.Codecs.compressing.CompressingStoredFieldsIndexWriter;
	using CompressionMode = Lucene.Net.Codecs.compressing.CompressionMode;
	using Lucene40StoredFieldsFormat = Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsFormat;
	using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;
	using DataOutput = Lucene.Net.Store.DataOutput;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

	/// <summary>
	/// Lucene 4.1 stored fields format.
	/// 
	/// <p><b>Principle</b></p>
	/// <p>this <seealso cref="StoredFieldsFormat"/> compresses blocks of 16KB of documents in
	/// order to improve the compression ratio compared to document-level
	/// compression. It uses the <a href="http://code.google.com/p/lz4/">LZ4</a>
	/// compression algorithm, which is fast to compress and very fast to decompress
	/// data. Although the compression method that is used focuses more on speed
	/// than on compression ratio, it should provide interesting compression ratios
	/// for redundant inputs (such as log files, HTML or plain text).</p>
	/// <p><b>File formats</b></p>
	/// <p>Stored fields are represented by two files:</p>
	/// <ol>
	/// <li><a name="field_data" id="field_data"></a>
	/// <p>A fields data file (extension <tt>.fdt</tt>). this file stores a compact
	/// representation of documents in compressed blocks of 16KB or more. When
	/// writing a segment, documents are appended to an in-memory <tt>byte[]</tt>
	/// buffer. When its size reaches 16KB or more, some metadata about the documents
	/// is flushed to disk, immediately followed by a compressed representation of
	/// the buffer using the
	/// <a href="http://code.google.com/p/lz4/">LZ4</a>
	/// <a href="http://fastcompression.blogspot.fr/2011/05/lz4-explained.html">compression format</a>.</p>
	/// <p>Here is a more detailed description of the field data file format:</p>
	/// <ul>
	/// <li>FieldData (.fdt) --&gt; &lt;Header&gt;, PackedIntsVersion, &lt;Chunk&gt;<sup>ChunkCount</sup></li>
	/// <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	/// <li>PackedIntsVersion --&gt; <seealso cref="PackedInts#VERSION_CURRENT"/> as a <seealso cref="DataOutput#writeVInt VInt"/></li>
	/// <li>ChunkCount is not known in advance and is the number of chunks necessary to store all document of the segment</li>
	/// <li>Chunk --&gt; DocBase, ChunkDocs, DocFieldCounts, DocLengths, &lt;CompressedDocs&gt;</li>
	/// <li>DocBase --&gt; the ID of the first document of the chunk as a <seealso cref="DataOutput#writeVInt VInt"/></li>
	/// <li>ChunkDocs --&gt; the number of documents in the chunk as a <seealso cref="DataOutput#writeVInt VInt"/></li>
	/// <li>DocFieldCounts --&gt; the number of stored fields of every document in the chunk, encoded as followed:<ul>
	///   <li>if chunkDocs=1, the unique value is encoded as a <seealso cref="DataOutput#writeVInt VInt"/></li>
	///   <li>else read a <seealso cref="DataOutput#writeVInt VInt"/> (let's call it <tt>bitsRequired</tt>)<ul>
	///     <li>if <tt>bitsRequired</tt> is <tt>0</tt> then all values are equal, and the common value is the following <seealso cref="DataOutput#writeVInt VInt"/></li>
	///     <li>else <tt>bitsRequired</tt> is the number of bits required to store any value, and values are stored in a <seealso cref="PackedInts packed"/> array where every value is stored on exactly <tt>bitsRequired</tt> bits</li>
	///   </ul></li>
	/// </ul></li>
	/// <li>DocLengths --&gt; the lengths of all documents in the chunk, encoded with the same method as DocFieldCounts</li>
	/// <li>CompressedDocs --&gt; a compressed representation of &lt;Docs&gt; using the LZ4 compression format</li>
	/// <li>Docs --&gt; &lt;Doc&gt;<sup>ChunkDocs</sup></li>
	/// <li>Doc --&gt; &lt;FieldNumAndType, Value&gt;<sup>DocFieldCount</sup></li>
	/// <li>FieldNumAndType --&gt; a <seealso cref="DataOutput#writeVLong VLong"/>, whose 3 last bits are Type and other bits are FieldNum</li>
	/// <li>Type --&gt;<ul>
	///   <li>0: Value is String</li>
	///   <li>1: Value is BinaryValue</li>
	///   <li>2: Value is Int</li>
	///   <li>3: Value is Float</li>
	///   <li>4: Value is Long</li>
	///   <li>5: Value is Double</li>
	///   <li>6, 7: unused</li>
	/// </ul></li>
	/// <li>FieldNum --&gt; an ID of the field</li>
	/// <li>Value --&gt; <seealso cref="DataOutput#writeString(String) String"/> | BinaryValue | Int | Float | Long | Double depending on Type</li>
	/// <li>BinaryValue --&gt; ValueLength &lt;Byte&gt;<sup>ValueLength</sup></li>
	/// </ul>
	/// <p>Notes</p>
	/// <ul>
	/// <li>If documents are larger than 16KB then chunks will likely contain only
	/// one document. However, documents can never spread across several chunks (all
	/// fields of a single document are in the same chunk).</li>
	/// <li>When at least one document in a chunk is large enough so that the chunk
	/// is larger than 32KB, the chunk will actually be compressed in several LZ4
	/// blocks of 16KB. this allows <seealso cref="StoredFieldVisitor"/>s which are only
	/// interested in the first fields of a document to not have to decompress 10MB
	/// of data if the document is 10MB, but only 16KB.</li>
	/// <li>Given that the original lengths are written in the metadata of the chunk,
	/// the decompressor can leverage this information to stop decoding as soon as
	/// enough data has been decompressed.</li>
	/// <li>In case documents are incompressible, CompressedDocs will be less than
	/// 0.5% larger than Docs.</li>
	/// </ul>
	/// </li>
	/// <li><a name="field_index" id="field_index"></a>
	/// <p>A fields index file (extension <tt>.fdx</tt>).</p>
	/// <ul>
	/// <li>FieldsIndex (.fdx) --&gt; &lt;Header&gt;, &lt;ChunkIndex&gt;</li>
	/// <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	/// <li>ChunkIndex: See <seealso cref="CompressingStoredFieldsIndexWriter"/></li>
	/// </ul>
	/// </li>
	/// </ol>
	/// <p><b>Known limitations</b></p>
	/// <p>this <seealso cref="StoredFieldsFormat"/> does not support individual documents
	/// larger than (<tt>2<sup>31</sup> - 2<sup>14</sup></tt>) bytes. In case this
	/// is a problem, you should use another format, such as
	/// <seealso cref="Lucene40StoredFieldsFormat"/>.</p>
	/// @lucene.experimental
	/// </summary>
	public sealed class Lucene41StoredFieldsFormat : CompressingStoredFieldsFormat
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene41StoredFieldsFormat() : base("Lucene41StoredFields", CompressionMode.FAST, 1 << 14)
	  {
	  }

	}

}