using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Packed
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


	using DataInput = Lucene.Net.Store.DataInput;

	/// <summary>
	/// Space optimized random access capable array of values with a fixed number of
	/// bits/value. Values are packed contiguously.
	/// </p><p>
	/// The implementation strives to perform af fast as possible under the
	/// constraint of contiguous bits, by avoiding expensive operations. this comes
	/// at the cost of code clarity.
	/// </p><p>
	/// Technical details: this implementation is a refinement of a non-branching
	/// version. The non-branching get and set methods meant that 2 or 4 atomics in
	/// the underlying array were always accessed, even for the cases where only
	/// 1 or 2 were needed. Even with caching, this had a detrimental effect on
	/// performance.
	/// Related to this issue, the old implementation used lookup tables for shifts
	/// and masks, which also proved to be a bit slower than calculating the shifts
	/// and masks on the fly.
	/// See https://issues.apache.org/jira/browse/LUCENE-4062 for details.
	/// 
	/// </summary>
	internal class Packed64 : PackedInts.MutableImpl
	{
	  internal const int BLOCK_SIZE = 64; // 32 = int, 64 = long
	  internal const int BLOCK_BITS = 6; // The #bits representing BLOCK_SIZE
	  internal static readonly int MOD_MASK = BLOCK_SIZE - 1; // x % BLOCK_SIZE

	  /// <summary>
	  /// Values are stores contiguously in the blocks array.
	  /// </summary>
	  private readonly long[] Blocks;
	  /// <summary>
	  /// A right-aligned mask of width BitsPerValue used by <seealso cref="#get(int)"/>.
	  /// </summary>
	  private readonly long MaskRight;
	  /// <summary>
	  /// Optimization: Saves one lookup in <seealso cref="#get(int)"/>.
	  /// </summary>
	  private readonly int BpvMinusBlockSize;

	  /// <summary>
	  /// Creates an array with the internal structures adjusted for the given
	  /// limits and initialized to 0. </summary>
	  /// <param name="valueCount">   the number of elements. </param>
	  /// <param name="bitsPerValue"> the number of bits available for any given value. </param>
	  public Packed64(int valueCount, int bitsPerValue) : base(valueCount, bitsPerValue)
	  {
		const PackedInts.Format format = PackedInts.Format.PACKED;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int longCount = format.longCount(PackedInts.VERSION_CURRENT, valueCount, bitsPerValue);
		int longCount = format.longCount(PackedInts.VERSION_CURRENT, valueCount, bitsPerValue);
		this.Blocks = new long[longCount];
		MaskRight = ~0L << (int)((uint)(BLOCK_SIZE - bitsPerValue) >> (BLOCK_SIZE - bitsPerValue));
		BpvMinusBlockSize = bitsPerValue - BLOCK_SIZE;
	  }

	  /// <summary>
	  /// Creates an array with content retrieved from the given DataInput. </summary>
	  /// <param name="in">       a DataInput, positioned at the start of Packed64-content. </param>
	  /// <param name="valueCount">  the number of elements. </param>
	  /// <param name="bitsPerValue"> the number of bits available for any given value. </param>
	  /// <exception cref="java.io.IOException"> if the values for the backing array could not
	  ///                             be retrieved. </exception>
	  public Packed64(int packedIntsVersion, DataInput @in, int valueCount, int bitsPerValue) : base(valueCount, bitsPerValue)
	  {
		const PackedInts.Format format = PackedInts.Format.PACKED;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long byteCount = format.byteCount(packedIntsVersion, valueCount, bitsPerValue);
		long byteCount = format.byteCount(packedIntsVersion, valueCount, bitsPerValue); // to know how much to read
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int longCount = format.longCount(PackedInts.VERSION_CURRENT, valueCount, bitsPerValue);
		int longCount = format.longCount(PackedInts.VERSION_CURRENT, valueCount, bitsPerValue); // to size the array
		Blocks = new long[longCount];
		// read as many longs as we can
		for (int i = 0; i < byteCount / 8; ++i)
		{
		  Blocks[i] = @in.ReadLong();
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int remaining = (int)(byteCount % 8);
		int remaining = (int)(byteCount % 8);
		if (remaining != 0)
		{
		  // read the last bytes
		  long lastLong = 0;
		  for (int i = 0; i < remaining; ++i)
		  {
			lastLong |= (@in.ReadByte() & 0xFFL) << (56 - i * 8);
		  }
		  Blocks[Blocks.Length - 1] = lastLong;
		}
		MaskRight = ~0L << (int)((uint)(BLOCK_SIZE - bitsPerValue) >> (BLOCK_SIZE - bitsPerValue));
		BpvMinusBlockSize = bitsPerValue - BLOCK_SIZE;
	  }

	  /// <param name="index"> the position of the value. </param>
	  /// <returns> the value at the given index. </returns>
	  public override long Get(int index)
	  {
		// The abstract index in a bit stream
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long majorBitPos = (long)index * bitsPerValue;
		long majorBitPos = (long)index * BitsPerValue_Renamed;
		// The index in the backing long-array
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int elementPos = (int)(majorBitPos >>> BLOCK_BITS);
		int elementPos = (int)((long)((ulong)majorBitPos >> BLOCK_BITS));
		// The number of value-bits in the second long
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long endBits = (majorBitPos & MOD_MASK) + bpvMinusBlockSize;
		long endBits = (majorBitPos & MOD_MASK) + BpvMinusBlockSize;

		if (endBits <= 0) // Single block
		{
		  return ((long)((ulong)Blocks[elementPos] >> -endBits)) & MaskRight;
		}
		// Two blocks
		return ((Blocks[elementPos] << endBits) | ((long)((ulong)Blocks[elementPos + 1] >> (BLOCK_SIZE - endBits)))) & MaskRight;
	  }

	  public override int Get(int index, long[] arr, int off, int len)
	  {
		Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
		Debug.Assert(index >= 0 && index < ValueCount);
		len = Math.Min(len, ValueCount - index);
		Debug.Assert(off + len <= arr.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int originalIndex = index;
		int originalIndex = index;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final PackedInts.Decoder decoder = BulkOperation.of(PackedInts.Format.PACKED, bitsPerValue);
		PackedInts.Decoder decoder = BulkOperation.Of(PackedInts.Format.PACKED, BitsPerValue_Renamed);

		// go to the next block where the value does not span across two blocks
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offsetInBlocks = index % decoder.longValueCount();
		int offsetInBlocks = index % decoder.LongValueCount();
		if (offsetInBlocks != 0)
		{
		  for (int i = offsetInBlocks; i < decoder.LongValueCount() && len > 0; ++i)
		  {
			arr[off++] = Get(index++);
			--len;
		  }
		  if (len == 0)
		  {
			return index - originalIndex;
		  }
		}

		// bulk get
		Debug.Assert(index % decoder.LongValueCount() == 0);
		int blockIndex = (int)((int)((uint)((long) index * BitsPerValue_Renamed) >> BLOCK_BITS));
		assert(((long)index * BitsPerValue_Renamed) & MOD_MASK) == 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int iterations = len / decoder.longValueCount();
		int iterations = len / decoder.LongValueCount();
		decoder.Decode(Blocks, blockIndex, arr, off, iterations);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int gotValues = iterations * decoder.longValueCount();
		int gotValues = iterations * decoder.LongValueCount();
		index += gotValues;
		len -= gotValues;
		Debug.Assert(len >= 0);

		if (index > originalIndex)
		{
		  // stay at the block boundary
		  return index - originalIndex;
		}
		else
		{
		  // no progress so far => already at a block boundary but no full block to get
		  Debug.Assert(index == originalIndex);
		  return base.Get(index, arr, off, len);
		}
	  }

	  public override void Set(int index, long value)
	  {
		// The abstract index in a contiguous bit stream
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long majorBitPos = (long)index * bitsPerValue;
		long majorBitPos = (long)index * BitsPerValue_Renamed;
		// The index in the backing long-array
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int elementPos = (int)(majorBitPos >>> BLOCK_BITS);
		int elementPos = (int)((long)((ulong)majorBitPos >> BLOCK_BITS)); // / BLOCK_SIZE
		// The number of value-bits in the second long
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long endBits = (majorBitPos & MOD_MASK) + bpvMinusBlockSize;
		long endBits = (majorBitPos & MOD_MASK) + BpvMinusBlockSize;

		if (endBits <= 0) // Single block
		{
		  Blocks[elementPos] = Blocks[elementPos] & ~(MaskRight << -endBits) | (value << -endBits);
		  return;
		}
		// Two blocks
		Blocks[elementPos] = Blocks[elementPos] & ~((long)((ulong)MaskRight >> endBits)) | ((long)((ulong)value >> endBits));
		Blocks[elementPos + 1] = Blocks[elementPos + 1] & (~(int)((uint)0L >> endBits)) | (value << (BLOCK_SIZE - endBits));
	  }

	  public override int Set(int index, long[] arr, int off, int len)
	  {
		Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
		Debug.Assert(index >= 0 && index < ValueCount);
		len = Math.Min(len, ValueCount - index);
		Debug.Assert(off + len <= arr.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int originalIndex = index;
		int originalIndex = index;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final PackedInts.Encoder encoder = BulkOperation.of(PackedInts.Format.PACKED, bitsPerValue);
		PackedInts.Encoder encoder = BulkOperation.Of(PackedInts.Format.PACKED, BitsPerValue_Renamed);

		// go to the next block where the value does not span across two blocks
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offsetInBlocks = index % encoder.longValueCount();
		int offsetInBlocks = index % encoder.LongValueCount();
		if (offsetInBlocks != 0)
		{
		  for (int i = offsetInBlocks; i < encoder.LongValueCount() && len > 0; ++i)
		  {
			Set(index++, arr[off++]);
			--len;
		  }
		  if (len == 0)
		  {
			return index - originalIndex;
		  }
		}

		// bulk set
		Debug.Assert(index % encoder.LongValueCount() == 0);
		int blockIndex = (int)((int)((uint)((long) index * BitsPerValue_Renamed) >> BLOCK_BITS));
		assert(((long)index * BitsPerValue_Renamed) & MOD_MASK) == 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int iterations = len / encoder.longValueCount();
		int iterations = len / encoder.LongValueCount();
		encoder.Encode(arr, off, Blocks, blockIndex, iterations);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int setValues = iterations * encoder.longValueCount();
		int setValues = iterations * encoder.LongValueCount();
		index += setValues;
		len -= setValues;
		Debug.Assert(len >= 0);

		if (index > originalIndex)
		{
		  // stay at the block boundary
		  return index - originalIndex;
		}
		else
		{
		  // no progress so far => already at a block boundary but no full block to get
		  Debug.Assert(index == originalIndex);
		  return base.Set(index, arr, off, len);
		}
	  }

	  public override string ToString()
	  {
		return "Packed64(bitsPerValue=" + BitsPerValue_Renamed + ", size=" + Size() + ", elements.length=" + Blocks.Length + ")";
	  }

	  public override long RamBytesUsed()
	  {
		return RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 3 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator.NUM_BYTES_LONG + RamUsageEstimator.NUM_BYTES_OBJECT_REF) + RamUsageEstimator.SizeOf(Blocks); // blocks ref -  maskRight -  bpvMinusBlockSize,valueCount,bitsPerValue
	  }

	  public override void Fill(int fromIndex, int toIndex, long val)
	  {
		Debug.Assert(PackedInts.BitsRequired(val) <= BitsPerValue);
		Debug.Assert(fromIndex <= toIndex);

		// minimum number of values that use an exact number of full blocks
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nAlignedValues = 64 / gcd(64, bitsPerValue);
		int nAlignedValues = 64 / Gcd(64, BitsPerValue_Renamed);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int span = toIndex - fromIndex;
		int span = toIndex - fromIndex;
		if (span <= 3 * nAlignedValues)
		{
		  // there needs be at least 2 * nAlignedValues aligned values for the
		  // block approach to be worth trying
		  base.Fill(fromIndex, toIndex, val);
		  return;
		}

		// fill the first values naively until the next block start
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int fromIndexModNAlignedValues = fromIndex % nAlignedValues;
		int fromIndexModNAlignedValues = fromIndex % nAlignedValues;
		if (fromIndexModNAlignedValues != 0)
		{
		  for (int i = fromIndexModNAlignedValues; i < nAlignedValues; ++i)
		  {
			Set(fromIndex++, val);
		  }
		}
		Debug.Assert(fromIndex % nAlignedValues == 0);

		// compute the long[] blocks for nAlignedValues consecutive values and
		// use them to set as many values as possible without applying any mask
		// or shift
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nAlignedBlocks = (nAlignedValues * bitsPerValue) >> 6;
		int nAlignedBlocks = (nAlignedValues * BitsPerValue_Renamed) >> 6;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long[] nAlignedValuesBlocks;
		long[] nAlignedValuesBlocks;
		{
		  Packed64 values = new Packed64(nAlignedValues, BitsPerValue_Renamed);
		  for (int i = 0; i < nAlignedValues; ++i)
		  {
			values.Set(i, val);
		  }
		  nAlignedValuesBlocks = values.Blocks;
		  Debug.Assert(nAlignedBlocks <= nAlignedValuesBlocks.Length);
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startBlock = (int)(((long) fromIndex * bitsPerValue) >>> 6);
		int startBlock = (int)((int)((uint)((long) fromIndex * BitsPerValue_Renamed) >> 6));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endBlock = (int)(((long) toIndex * bitsPerValue) >>> 6);
		int endBlock = (int)((int)((uint)((long) toIndex * BitsPerValue_Renamed) >> 6));
		for (int block = startBlock; block < endBlock; ++block)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long blockValue = nAlignedValuesBlocks[block % nAlignedBlocks];
		  long blockValue = nAlignedValuesBlocks[block % nAlignedBlocks];
		  Blocks[block] = blockValue;
		}

		// fill the gap
		for (int i = (int)(((long) endBlock << 6) / BitsPerValue_Renamed); i < toIndex; ++i)
		{
		  Set(i, val);
		}
	  }

	  private static int Gcd(int a, int b)
	  {
		if (a < b)
		{
		  return Gcd(b, a);
		}
		else if (b == 0)
		{
		  return a;
		}
		else
		{
		  return Gcd(b, a % b);
		}
	  }

	  public override void Clear()
	  {
		Arrays.fill(Blocks, 0L);
	  }
	}

}