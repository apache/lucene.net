using System;
using System.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util.Packed;
using Lucene.Net.Support;

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

    /*
	using DataInput = Lucene.Net.Store.DataInput;
	using DataOutput = Lucene.Net.Store.DataOutput;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using Decoder = Lucene.Net.Util.Packed.PackedInts.Decoder;
	using FormatAndBits = Lucene.Net.Util.Packed.PackedInts.FormatAndBits;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    */

	/// <summary>
	/// Encode all values in normal area with fixed bit width, 
	/// which is determined by the max value in this block.
	/// </summary>
	internal sealed class ForUtil
	{

	  /// <summary>
	  /// Special number of bits per value used whenever all values to encode are equal.
	  /// </summary>
	  private const int ALL_VALUES_EQUAL = 0;

	  /// <summary>
	  /// Upper limit of the number of bytes that might be required to stored
	  /// <code>BLOCK_SIZE</code> encoded values.
	  /// </summary>
	  internal static readonly int MAX_ENCODED_SIZE = Lucene41PostingsFormat.BLOCK_SIZE * 4;

	  /// <summary>
	  /// Upper limit of the number of values that might be decoded in a single call to
	  /// <seealso cref="#readBlock(IndexInput, byte[], int[])"/>. Although values after
	  /// <code>BLOCK_SIZE</code> are garbage, it is necessary to allocate value buffers
	  /// whose size is >= MAX_DATA_SIZE to avoid <seealso cref="ArrayIndexOutOfBoundsException"/>s.
	  /// </summary>
	  internal static readonly int MAX_DATA_SIZE;
	  static ForUtil()
	  {
		int maxDataSize = 0;
		for (int version = PackedInts.VERSION_START;version <= PackedInts.VERSION_CURRENT;version++)
		{
		  foreach (PackedInts.Format format in Enum.GetValues(typeof(PackedInts.Format)))
		  {
			for (int bpv = 1; bpv <= 32; ++bpv)
			{
			  if (!format.IsSupported(bpv))
			  {
				continue;
			  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Decoder decoder = Lucene.Net.Util.Packed.PackedInts.getDecoder(format, version, bpv);
			  PackedInts.Decoder decoder = PackedInts.GetDecoder(format, version, bpv);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int iterations = computeIterations(decoder);
			  int iterations = ComputeIterations(decoder);
			  maxDataSize = Math.Max(maxDataSize, iterations * decoder.ByteValueCount());
			}
		  }
		}
		MAX_DATA_SIZE = maxDataSize;
	  }

	  /// <summary>
	  /// Compute the number of iterations required to decode <code>BLOCK_SIZE</code>
	  /// values with the provided <seealso cref="Decoder"/>.
	  /// </summary>
      private static int ComputeIterations(PackedInts.Decoder decoder)
	  {
		return (int) Math.Ceiling((float) Lucene41PostingsFormat.BLOCK_SIZE / decoder.ByteValueCount());
	  }

	  /// <summary>
	  /// Compute the number of bytes required to encode a block of values that require
	  /// <code>bitsPerValue</code> bits per value with format <code>format</code>.
	  /// </summary>
	  private static int EncodedSize(PackedInts.Format format, int packedIntsVersion, int bitsPerValue)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long byteCount = format.byteCount(packedIntsVersion, BLOCK_SIZE, bitsPerValue);
		long byteCount = format.ByteCount(packedIntsVersion, Lucene41PostingsFormat.BLOCK_SIZE, bitsPerValue);
		Debug.Assert(byteCount >= 0 && byteCount <= int.MaxValue, byteCount.ToString());
		return (int) byteCount;
	  }

	  private readonly int[] EncodedSizes;
	  private readonly PackedInts.Encoder[] Encoders;
      private readonly PackedInts.Decoder[] Decoders;
	  private readonly int[] Iterations;

	  /// <summary>
	  /// Create a new <seealso cref="ForUtil"/> instance and save state into <code>out</code>.
	  /// </summary>
	  internal ForUtil(float acceptableOverheadRatio, DataOutput @out)
	  {
		@out.WriteVInt(PackedInts.VERSION_CURRENT);
		EncodedSizes = new int[33];
		Encoders = new PackedInts.Encoder[33];
        Decoders = new PackedInts.Decoder[33];
		Iterations = new int[33];

		for (int bpv = 1; bpv <= 32; ++bpv)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.FormatAndBits formatAndBits = Lucene.Net.Util.Packed.PackedInts.fastestFormatAndBits(BLOCK_SIZE, bpv, acceptableOverheadRatio);
		  PackedInts.FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(Lucene41PostingsFormat.BLOCK_SIZE, bpv, acceptableOverheadRatio);
		  Debug.Assert(formatAndBits.Format.isSupported(formatAndBits.BitsPerValue));
		  Debug.Assert(formatAndBits.BitsPerValue <= 32);
		  EncodedSizes[bpv] = EncodedSize(formatAndBits.Format, PackedInts.VERSION_CURRENT, formatAndBits.BitsPerValue);
		  Encoders[bpv] = PackedInts.GetEncoder(formatAndBits.Format, PackedInts.VERSION_CURRENT, formatAndBits.BitsPerValue);
		  Decoders[bpv] = PackedInts.GetDecoder(formatAndBits.Format, PackedInts.VERSION_CURRENT, formatAndBits.BitsPerValue);
		  Iterations[bpv] = ComputeIterations(Decoders[bpv]);

		  @out.WriteVInt(formatAndBits.Format.Id << 5 | (formatAndBits.BitsPerValue - 1));
		}
	  }

	  /// <summary>
	  /// Restore a <seealso cref="ForUtil"/> from a <seealso cref="DataInput"/>.
	  /// </summary>
	  internal ForUtil(DataInput @in)
	  {
		int packedIntsVersion = @in.ReadVInt();
		PackedInts.CheckVersion(packedIntsVersion);
		EncodedSizes = new int[33];
		Encoders = new PackedInts.Encoder[33];
        Decoders = new PackedInts.Decoder[33];
		Iterations = new int[33];

		for (int bpv = 1; bpv <= 32; ++bpv)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = in.readVInt();
		  int code = @in.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int formatId = code >>> 5;
		  int formatId = (int)((uint)code >> 5);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerValue = (code & 31) + 1;
		  int bitsPerValue = (code & 31) + 1;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Format format = Lucene.Net.Util.Packed.PackedInts.Format.byId(formatId);
		  PackedInts.Format format = PackedInts.Format.ById(formatId);
		  Debug.Assert(format.IsSupported(bitsPerValue));
		  EncodedSizes[bpv] = EncodedSize(format, packedIntsVersion, bitsPerValue);
		  Encoders[bpv] = PackedInts.GetEncoder(format, packedIntsVersion, bitsPerValue);
		  Decoders[bpv] = PackedInts.GetDecoder(format, packedIntsVersion, bitsPerValue);
		  Iterations[bpv] = ComputeIterations(Decoders[bpv]);
		}
	  }

	  /// <summary>
	  /// Write a block of data (<code>For</code> format).
	  /// </summary>
	  /// <param name="data">     the data to write </param>
	  /// <param name="encoded">  a buffer to use to encode data </param>
	  /// <param name="out">      the destination output </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
	  internal void WriteBlock(int[] data, sbyte[] encoded, IndexOutput @out)
	  {
		if (IsAllEqual(data))
		{
		  @out.WriteByte((sbyte) ALL_VALUES_EQUAL);
		  @out.WriteVInt(data[0]);
		  return;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numBits = bitsRequired(data);
		int numBits = BitsRequired(data);
		Debug.Assert(numBits > 0 && numBits <= 32, numBits.ToString());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Encoder encoder = encoders[numBits];
		PackedInts.Encoder encoder = Encoders[numBits];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int iters = iterations[numBits];
		int iters = Iterations[numBits];
		Debug.Assert(iters * encoder.ByteValueCount() >= Lucene41PostingsFormat.BLOCK_SIZE);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int encodedSize = encodedSizes[numBits];
		int encodedSize = EncodedSizes[numBits];
		Debug.Assert(iters * encoder.ByteBlockCount() >= encodedSize);

		@out.WriteByte((sbyte) numBits);

		encoder.Encode(data, 0, encoded, 0, iters);
		@out.WriteBytes(encoded, encodedSize);
	  }

	  /// <summary>
	  /// Read the next block of data (<code>For</code> format).
	  /// </summary>
	  /// <param name="in">        the input to use to read data </param>
	  /// <param name="encoded">   a buffer that can be used to store encoded data </param>
	  /// <param name="decoded">   where to write decoded data </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
	  internal void ReadBlock(IndexInput @in, sbyte[] encoded, int[] decoded)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numBits = in.readByte();
		int numBits = @in.ReadByte();
		Debug.Assert(numBits <= 32, numBits.ToString());

		if (numBits == ALL_VALUES_EQUAL)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int value = in.readVInt();
		  int value = @in.ReadVInt();
          CollectionsHelper.Fill(decoded, 0, Lucene41PostingsFormat.BLOCK_SIZE, value);
		  return;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int encodedSize = encodedSizes[numBits];
		int encodedSize = EncodedSizes[numBits];
		@in.ReadBytes(encoded, 0, encodedSize);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Decoder decoder = decoders[numBits];
        PackedInts.Decoder decoder = Decoders[numBits];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int iters = iterations[numBits];
		int iters = Iterations[numBits];
        Debug.Assert(iters * decoder.ByteValueCount() >= Lucene41PostingsFormat.BLOCK_SIZE);

		decoder.Decode(encoded, 0, decoded, 0, iters);
	  }

	  /// <summary>
	  /// Skip the next block of data.
	  /// </summary>
	  /// <param name="in">      the input where to read data </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
	  internal void SkipBlock(IndexInput @in)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numBits = in.readByte();
		int numBits = @in.ReadByte();
		if (numBits == ALL_VALUES_EQUAL)
		{
		  @in.ReadVInt();
		  return;
		}
		Debug.Assert(numBits > 0 && numBits <= 32, numBits.ToString());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int encodedSize = encodedSizes[numBits];
		int encodedSize = EncodedSizes[numBits];
		@in.Seek(@in.FilePointer + encodedSize);
	  }

	  private static bool IsAllEqual(int[] data)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int v = data[0];
		int v = data[0];
		for (int i = 1; i < Lucene41PostingsFormat.BLOCK_SIZE; ++i)
		{
		  if (data[i] != v)
		  {
			return false;
		  }
		}
		return true;
	  }

	  /// <summary>
	  /// Compute the number of bits required to serialize any of the longs in
	  /// <code>data</code>.
	  /// </summary>
	  private static int BitsRequired(int[] data)
	  {
		long or = 0;
		for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; ++i)
		{
		  Debug.Assert(data[i] >= 0);
		  or |= data[i];
		}
		return PackedInts.BitsRequired(or);
	  }

	}

}