using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene46
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

	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 4.6 FieldInfos writer.
	/// </summary>
	/// <seealso cref= Lucene46FieldInfosFormat
	/// @lucene.experimental </seealso>
	internal sealed class Lucene46FieldInfosWriter : FieldInfosWriter
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene46FieldInfosWriter()
	  {
	  }

	  public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = Lucene.Net.Index.IndexFileNames.segmentFileName(segmentName, segmentSuffix, Lucene46FieldInfosFormat.EXTENSION);
		string fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, Lucene46FieldInfosFormat.EXTENSION);
		IndexOutput output = directory.CreateOutput(fileName, context);
		bool success = false;
		try
		{
		  CodecUtil.WriteHeader(output, Lucene46FieldInfosFormat.CODEC_NAME, Lucene46FieldInfosFormat.FORMAT_CURRENT);
		  output.WriteVInt(infos.Size());
		  foreach (FieldInfo fi in infos)
		  {
			IndexOptions indexOptions = fi.IndexOptions;
			sbyte bits = 0x0;
			if (fi.HasVectors())
			{
				bits |= Lucene46FieldInfosFormat.STORE_TERMVECTOR;
			}
			if (fi.OmitsNorms())
			{
				bits |= Lucene46FieldInfosFormat.OMIT_NORMS;
			}
			if (fi.HasPayloads())
			{
				bits |= Lucene46FieldInfosFormat.STORE_PAYLOADS;
			}
			if (fi.Indexed)
			{
			  bits |= Lucene46FieldInfosFormat.IS_INDEXED;
			  Debug.Assert(indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.HasPayloads());
			  if (indexOptions == IndexOptions.DOCS_ONLY)
			  {
				bits |= Lucene46FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
			  {
				bits |= Lucene46FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
			  }
			  else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
			  {
				bits |= Lucene46FieldInfosFormat.OMIT_POSITIONS;
			  }
			}
			output.WriteString(fi.Name);
			output.WriteVInt(fi.Number);
			output.WriteByte(bits);

			// pack the DV types in one byte
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte dv = docValuesByte(fi.getDocValuesType());
			sbyte dv = DocValuesByte(fi.DocValuesType_e);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte nrm = docValuesByte(fi.getNormType());
			sbyte nrm = DocValuesByte(fi.NormType);
			assert(dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0;
			sbyte val = unchecked((sbyte)(0xff & ((nrm << 4) | dv)));
			output.WriteByte(val);
			output.WriteLong(fi.DocValuesGen);
			output.WriteStringStringMap(fi.Attributes());
		  }
		  CodecUtil.WriteFooter(output);
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			output.Close();
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(output);
		  }
		}
	  }

	  private static sbyte DocValuesByte(DocValuesType type)
	  {
		if (type == null)
		{
		  return 0;
		}
		else if (type == DocValuesType.NUMERIC)
		{
		  return 1;
		}
		else if (type == DocValuesType.BINARY)
		{
		  return 2;
		}
		else if (type == DocValuesType.SORTED)
		{
		  return 3;
		}
		else if (type == DocValuesType.SORTED_SET)
		{
		  return 4;
		}
		else
		{
		  throw new AssertionError();
		}
	  }
	}

}