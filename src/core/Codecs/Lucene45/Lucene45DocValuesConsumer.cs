using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene45
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


	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using MathUtil = Lucene.Net.Util.MathUtil;
	using StringHelper = Lucene.Net.Util.StringHelper;
	using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
	using MonotonicBlockPackedWriter = Lucene.Net.Util.Packed.MonotonicBlockPackedWriter;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

	/// <summary>
	/// writer for <seealso cref="Lucene45DocValuesFormat"/> </summary>
	public class Lucene45DocValuesConsumer : DocValuesConsumer, IDisposable
	{

	  internal const int BLOCK_SIZE = 16384;
	  internal const int ADDRESS_INTERVAL = 16;
	  internal static readonly Number MISSING_ORD = Convert.ToInt64(-1);

	  /// <summary>
	  /// Compressed using packed blocks of ints. </summary>
	  public const int DELTA_COMPRESSED = 0;
	  /// <summary>
	  /// Compressed by computing the GCD. </summary>
	  public const int GCD_COMPRESSED = 1;
	  /// <summary>
	  /// Compressed by giving IDs to unique values. </summary>
	  public const int TABLE_COMPRESSED = 2;

	  /// <summary>
	  /// Uncompressed binary, written directly (fixed length). </summary>
	  public const int BINARY_FIXED_UNCOMPRESSED = 0;
	  /// <summary>
	  /// Uncompressed binary, written directly (variable length). </summary>
	  public const int BINARY_VARIABLE_UNCOMPRESSED = 1;
	  /// <summary>
	  /// Compressed binary with shared prefixes </summary>
	  public const int BINARY_PREFIX_COMPRESSED = 2;

	  /// <summary>
	  /// Standard storage for sorted set values with 1 level of indirection:
	  ///  docId -> address -> ord. 
	  /// </summary>
	  public const int SORTED_SET_WITH_ADDRESSES = 0;
	  /// <summary>
	  /// Single-valued sorted set values, encoded as sorted values, so no level
	  ///  of indirection: docId -> ord. 
	  /// </summary>
	  public const int SORTED_SET_SINGLE_VALUED_SORTED = 1;

	  internal IndexOutput Data, Meta;
	  internal readonly int MaxDoc;

	  /// <summary>
	  /// expert: Creates a new writer </summary>
	  public Lucene45DocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
	  {
		bool success = false;
		try
		{
		  string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.name, state.SegmentSuffix, dataExtension);
		  Data = state.Directory.createOutput(dataName, state.Context);
		  CodecUtil.WriteHeader(Data, dataCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
		  string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.name, state.SegmentSuffix, metaExtension);
		  Meta = state.Directory.createOutput(metaName, state.Context);
		  CodecUtil.WriteHeader(Meta, metaCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
		  MaxDoc = state.SegmentInfo.DocCount;
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(this);
		  }
		}
	  }

	  public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		AddNumericField(field, values, true);
	  }

	  internal virtual void AddNumericField(FieldInfo field, IEnumerable<Number> values, bool optimizeStorage)
	  {
		long count = 0;
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		long gcd = 0;
		bool missing = false;
		// TODO: more efficient?
		HashSet<long?> uniqueValues = null;
		if (optimizeStorage)
		{
		  uniqueValues = new HashSet<>();

		  foreach (Number nv in values)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long v;
			long v;
			if (nv == null)
			{
			  v = 0;
			  missing = true;
			}
			else
			{
			  v = (long)nv;
			}

			if (gcd != 1)
			{
			  if (v < long.MinValue / 2 || v > long.MaxValue / 2)
			  {
				// in that case v - minValue might overflow and make the GCD computation return
				// wrong results. Since these extreme values are unlikely, we just discard
				// GCD computation for them
				gcd = 1;
			  } // minValue needs to be set first
			  else if (count != 0)
			  {
				gcd = MathUtil.Gcd(gcd, v - minValue);
			  }
			}

			minValue = Math.Min(minValue, v);
			maxValue = Math.Max(maxValue, v);

			if (uniqueValues != null)
			{
			  if (uniqueValues.Add(v))
			  {
				if (uniqueValues.Count > 256)
				{
				  uniqueValues = null;
				}
			  }
			}

			++count;
		  }
		}
		else
		{
		  for (@SuppressWarnings("unused") Number nv : values)
		  {
			++count;
		  }
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long delta = maxValue - minValue;
		long delta = maxValue - minValue;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int format;
		int format;
		if (uniqueValues != null && (delta < 0L || PackedInts.BitsRequired(uniqueValues.Count - 1) < PackedInts.BitsRequired(delta)) && count <= int.MaxValue)
		{
		  format = TABLE_COMPRESSED;
		}
		else if (gcd != 0 && gcd != 1)
		{
		  format = GCD_COMPRESSED;
		}
		else
		{
		  format = DELTA_COMPRESSED;
		}
		Meta.WriteVInt(field.Number);
		Meta.WriteByte(Lucene45DocValuesFormat.NUMERIC);
		Meta.WriteVInt(format);
		if (missing)
		{
		  Meta.WriteLong(Data.FilePointer);
		  WriteMissingBitset(values);
		}
		else
		{
		  Meta.WriteLong(-1L);
		}
		Meta.WriteVInt(PackedInts.VERSION_CURRENT);
		Meta.WriteLong(Data.FilePointer);
		Meta.WriteVLong(count);
		Meta.WriteVInt(BLOCK_SIZE);

		switch (format)
		{
		  case GCD_COMPRESSED:
			Meta.WriteLong(minValue);
			Meta.WriteLong(gcd);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.BlockPackedWriter quotientWriter = new Lucene.Net.Util.Packed.BlockPackedWriter(data, BLOCK_SIZE);
			BlockPackedWriter quotientWriter = new BlockPackedWriter(Data, BLOCK_SIZE);
			foreach (Number nv in values)
			{
			  long value = nv == null ? 0 : (long)nv;
			  quotientWriter.Add((value - minValue) / gcd);
			}
			quotientWriter.Finish();
			break;
		  case DELTA_COMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.BlockPackedWriter writer = new Lucene.Net.Util.Packed.BlockPackedWriter(data, BLOCK_SIZE);
			BlockPackedWriter writer = new BlockPackedWriter(Data, BLOCK_SIZE);
			foreach (Number nv in values)
			{
			  writer.Add(nv == null ? 0 : (long)nv);
			}
			writer.Finish();
			break;
		  case TABLE_COMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Long[] decode = uniqueValues.toArray(new Long[uniqueValues.size()]);
			long?[] decode = uniqueValues.toArray(new long?[uniqueValues.Count]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.HashMap<Long,Integer> encode = new java.util.HashMap<>();
			Dictionary<long?, int?> encode = new Dictionary<long?, int?>();
			Meta.WriteVInt(decode.Length);
			for (int i = 0; i < decode.Length; i++)
			{
			  Meta.WriteLong(decode[i]);
			  encode[decode[i]] = i;
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsRequired = Lucene.Net.Util.Packed.PackedInts.bitsRequired(uniqueValues.size() - 1);
			int bitsRequired = PackedInts.BitsRequired(uniqueValues.Count - 1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Writer ordsWriter = Lucene.Net.Util.Packed.PackedInts.getWriterNoHeader(data, Lucene.Net.Util.Packed.PackedInts.Format.PACKED, (int) count, bitsRequired, Lucene.Net.Util.Packed.PackedInts.DEFAULT_BUFFER_SIZE);
			PackedInts.Writer ordsWriter = PackedInts.GetWriterNoHeader(Data, PackedInts.Format.PACKED, (int) count, bitsRequired, PackedInts.DEFAULT_BUFFER_SIZE);
			foreach (Number nv in values)
			{
			  ordsWriter.Add(encode[nv == null ? 0 : (long)nv]);
			}
			ordsWriter.Finish();
			break;
		  default:
			throw new AssertionError();
		}
	  }

	  // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
	  // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
	  internal virtual void writeMissingBitset<T1>(IEnumerable<T1> values)
	  {
		sbyte bits = 0;
		int count = 0;
		foreach (object v in values)
		{
		  if (count == 8)
		  {
			Data.WriteByte(bits);
			count = 0;
			bits = 0;
		  }
		  if (v != null)
		  {
			bits |= (sbyte)(1 << (count & 7));
		  }
		  count++;
		}
		if (count > 0)
		{
		  Data.WriteByte(bits);
		}
	  }

	  public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		// write the byte[] data
		Meta.WriteVInt(field.Number);
		Meta.WriteByte(Lucene45DocValuesFormat.BINARY);
		int minLength = int.MaxValue;
		int maxLength = int.MinValue;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long startFP = data.getFilePointer();
		long startFP = Data.FilePointer;
		long count = 0;
		bool missing = false;
		foreach (BytesRef v in values)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length;
		  int length;
		  if (v == null)
		  {
			length = 0;
			missing = true;
		  }
		  else
		  {
			length = v.Length;
		  }
		  minLength = Math.Min(minLength, length);
		  maxLength = Math.Max(maxLength, length);
		  if (v != null)
		  {
			Data.WriteBytes(v.Bytes, v.Offset, v.Length);
		  }
		  count++;
		}
		Meta.WriteVInt(minLength == maxLength ? BINARY_FIXED_UNCOMPRESSED : BINARY_VARIABLE_UNCOMPRESSED);
		if (missing)
		{
		  Meta.WriteLong(Data.FilePointer);
		  WriteMissingBitset(values);
		}
		else
		{
		  Meta.WriteLong(-1L);
		}
		Meta.WriteVInt(minLength);
		Meta.WriteVInt(maxLength);
		Meta.WriteVLong(count);
		Meta.WriteLong(startFP);

		// if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
		// otherwise, we need to record the length fields...
		if (minLength != maxLength)
		{
		  Meta.WriteLong(Data.FilePointer);
		  Meta.WriteVInt(PackedInts.VERSION_CURRENT);
		  Meta.WriteVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.MonotonicBlockPackedWriter writer = new Lucene.Net.Util.Packed.MonotonicBlockPackedWriter(data, BLOCK_SIZE);
		  MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(Data, BLOCK_SIZE);
		  long addr = 0;
		  foreach (BytesRef v in values)
		  {
			if (v != null)
			{
			  addr += v.Length;
			}
			writer.Add(addr);
		  }
		  writer.Finish();
		}
	  }

	  /// <summary>
	  /// expert: writes a value dictionary for a sorted/sortedset field </summary>
	  protected internal virtual void AddTermsDict(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		// first check if its a "fixed-length" terms dict
		int minLength = int.MaxValue;
		int maxLength = int.MinValue;
		foreach (BytesRef v in values)
		{
		  minLength = Math.Min(minLength, v.Length);
		  maxLength = Math.Max(maxLength, v.Length);
		}
		if (minLength == maxLength)
		{
		  // no index needed: direct addressing by mult
		  AddBinaryField(field, values);
		}
		else
		{
		  // header
		  Meta.WriteVInt(field.Number);
		  Meta.WriteByte(Lucene45DocValuesFormat.BINARY);
		  Meta.WriteVInt(BINARY_PREFIX_COMPRESSED);
		  Meta.WriteLong(-1L);
		  // now write the bytes: sharing prefixes within a block
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long startFP = data.getFilePointer();
		  long startFP = Data.FilePointer;
		  // currently, we have to store the delta from expected for every 1/nth term
		  // we could avoid this, but its not much and less overall RAM than the previous approach!
		  RAMOutputStream addressBuffer = new RAMOutputStream();
		  MonotonicBlockPackedWriter termAddresses = new MonotonicBlockPackedWriter(addressBuffer, BLOCK_SIZE);
		  BytesRef lastTerm = new BytesRef();
		  long count = 0;
		  foreach (BytesRef v in values)
		  {
			if (count % ADDRESS_INTERVAL == 0)
			{
			  termAddresses.Add(Data.FilePointer - startFP);
			  // force the first term in a block to be abs-encoded
			  lastTerm.Length = 0;
			}

			// prefix-code
			int sharedPrefix = StringHelper.BytesDifference(lastTerm, v);
			Data.WriteVInt(sharedPrefix);
			Data.WriteVInt(v.Length - sharedPrefix);
			Data.WriteBytes(v.Bytes, v.Offset + sharedPrefix, v.Length - sharedPrefix);
			lastTerm.CopyBytes(v);
			count++;
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long indexStartFP = data.getFilePointer();
		  long indexStartFP = Data.FilePointer;
		  // write addresses of indexed terms
		  termAddresses.Finish();
		  addressBuffer.WriteTo(Data);
		  addressBuffer = null;
		  termAddresses = null;
		  Meta.WriteVInt(minLength);
		  Meta.WriteVInt(maxLength);
		  Meta.WriteVLong(count);
		  Meta.WriteLong(startFP);
		  Meta.WriteVInt(ADDRESS_INTERVAL);
		  Meta.WriteLong(indexStartFP);
		  Meta.WriteVInt(PackedInts.VERSION_CURRENT);
		  Meta.WriteVInt(BLOCK_SIZE);
		}
	  }

	  public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		Meta.WriteVInt(field.Number);
		Meta.WriteByte(Lucene45DocValuesFormat.SORTED);
		AddTermsDict(field, values);
		AddNumericField(field, docToOrd, false);
	  }

	  private static bool IsSingleValued(IEnumerable<Number> docToOrdCount)
	  {
		foreach (Number ordCount in docToOrdCount)
		{
		  if ((long)ordCount > 1)
		  {
			return false;
		  }
		}
		return true;
	  }

	  public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		Meta.WriteVInt(field.Number);
		Meta.WriteByte(Lucene45DocValuesFormat.SORTED_SET);

		if (IsSingleValued(docToOrdCount))
		{
		  Meta.WriteVInt(SORTED_SET_SINGLE_VALUED_SORTED);
		  // The field is single-valued, we can encode it as SORTED
		  AddSortedField(field, values, new IterableAnonymousInnerClassHelper(this, docToOrdCount, ords));
		  return;
		}

		Meta.WriteVInt(SORTED_SET_WITH_ADDRESSES);

		// write the ord -> byte[] as a binary field
		AddTermsDict(field, values);

		// write the stream of ords as a numeric field
		// NOTE: we could return an iterator that delta-encodes these within a doc
		AddNumericField(field, ords, false);

		// write the doc -> ord count as a absolute index to the stream
		Meta.WriteVInt(field.Number);
		Meta.WriteByte(Lucene45DocValuesFormat.NUMERIC);
		Meta.WriteVInt(DELTA_COMPRESSED);
		Meta.WriteLong(-1L);
		Meta.WriteVInt(PackedInts.VERSION_CURRENT);
		Meta.WriteLong(Data.FilePointer);
		Meta.WriteVLong(MaxDoc);
		Meta.WriteVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.MonotonicBlockPackedWriter writer = new Lucene.Net.Util.Packed.MonotonicBlockPackedWriter(data, BLOCK_SIZE);
		MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(Data, BLOCK_SIZE);
		long addr = 0;
		foreach (Number v in docToOrdCount)
		{
		  addr += (long)v;
		  writer.Add(addr);
		}
		writer.Finish();
	  }

	  private class IterableAnonymousInnerClassHelper : IEnumerable<Number>
	  {
		  private readonly Lucene45DocValuesConsumer OuterInstance;

		  private IEnumerable<Number> DocToOrdCount;
		  private IEnumerable<Number> Ords;

		  public IterableAnonymousInnerClassHelper(Lucene45DocValuesConsumer outerInstance, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
		  {
			  this.OuterInstance = outerInstance;
			  this.DocToOrdCount = docToOrdCount;
			  this.Ords = ords;
		  }


		  public virtual IEnumerator<Number> GetEnumerator()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Iterator<Number> docToOrdCountIt = docToOrdCount.iterator();
			IEnumerator<Number> docToOrdCountIt = DocToOrdCount.GetEnumerator();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Iterator<Number> ordsIt = ords.iterator();
			IEnumerator<Number> ordsIt = Ords.GetEnumerator();
			return new IteratorAnonymousInnerClassHelper(this, docToOrdCountIt, ordsIt);
		  }

		  private class IteratorAnonymousInnerClassHelper : IEnumerator<Number>
		  {
			  private readonly IterableAnonymousInnerClassHelper OuterInstance;

			  private IEnumerator<Number> DocToOrdCountIt;
			  private IEnumerator<Number> OrdsIt;

			  public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance, IEnumerator<Number> docToOrdCountIt, IEnumerator<Number> ordsIt)
			  {
				  this.outerInstance = outerInstance;
				  this.DocToOrdCountIt = docToOrdCountIt;
				  this.OrdsIt = ordsIt;
			  }


			  public virtual bool HasNext()
			  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				Debug.Assert(OrdsIt.hasNext() ? DocToOrdCountIt.hasNext(), true);
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				return DocToOrdCountIt.hasNext();
			  }

			  public virtual Number Next()
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Number ordCount = docToOrdCountIt.next();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				Number ordCount = DocToOrdCountIt.next();
				if ((long)ordCount == 0)
				{
				  return MISSING_ORD;
				}
				else
				{
				  Debug.Assert((long)ordCount == 1);
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				  return OrdsIt.next();
				}
			  }

			  public virtual void Remove()
			  {
				throw new System.NotSupportedException();
			  }

		  }

	  }

	  public override void Close()
	  {
		bool success = false;
		try
		{
		  if (Meta != null)
		  {
			Meta.WriteVInt(-1); // write EOF marker
			CodecUtil.WriteFooter(Meta); // write checksum
		  }
		  if (Data != null)
		  {
			CodecUtil.WriteFooter(Data); // write checksum
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(Data, Meta);
		  }
		  else
		  {
			IOUtils.CloseWhileHandlingException(Data, Meta);
		  }
		  Meta = Data = null;
		}
	  }
	}

}