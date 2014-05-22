using System;
using System.Diagnostics;

// this file has been automatically generated, DO NOT EDIT

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
	/// Direct wrapping of 8-bits values to a backing array.
	/// @lucene.internal
	/// </summary>
	internal sealed class Direct8 : PackedInts.MutableImpl
	{
	  internal readonly sbyte[] Values;

	  internal Direct8(int valueCount) : base(valueCount, 8)
	  {
		Values = new sbyte[valueCount];
	  }

	  internal Direct8(int packedIntsVersion, DataInput @in, int valueCount) : this(valueCount)
	  {
		@in.ReadBytes(Values, 0, valueCount);
		// because packed ints have not always been byte-aligned
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int remaining = (int)(PackedInts.Format.PACKED.byteCount(packedIntsVersion, valueCount, 8) - 1L * valueCount);
		int remaining = (int)(PackedInts.Format.PACKED.byteCount(packedIntsVersion, valueCount, 8) - 1L * valueCount);
		for (int i = 0; i < remaining; ++i)
		{
		  @in.ReadByte();
		}
	  }

	  public override long Get(int index)
	  {
		return Values[index] & 0xFFL;
	  }

	  public override void Set(int index, long value)
	  {
		Values[index] = (sbyte)(value);
	  }

	  public override long RamBytesUsed()
	  {
		return RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator.NUM_BYTES_OBJECT_REF) + RamUsageEstimator.SizeOf(Values); // values ref -  valueCount,bitsPerValue
	  }

	  public override void Clear()
	  {
		Arrays.fill(Values, (sbyte) 0L);
	  }

	  public override object Array
	  {
		  get
		  {
			return Values;
		  }
	  }

	  public override bool HasArray()
	  {
		return true;
	  }

	  public override int Get(int index, long[] arr, int off, int len)
	  {
		Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
		Debug.Assert(index >= 0 && index < ValueCount);
		Debug.Assert(off + len <= arr.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int gets = Math.min(valueCount - index, len);
		int gets = Math.Min(ValueCount - index, len);
		for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
		{
		  arr[o] = Values[i] & 0xFFL;
		}
		return gets;
	  }

	  public override int Set(int index, long[] arr, int off, int len)
	  {
		Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
		Debug.Assert(index >= 0 && index < ValueCount);
		Debug.Assert(off + len <= arr.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int sets = Math.min(valueCount - index, len);
		int sets = Math.Min(ValueCount - index, len);
		for (int i = index, o = off, end = index + sets; i < end; ++i, ++o)
		{
		  Values[i] = (sbyte) arr[o];
		}
		return sets;
	  }

	  public override void Fill(int fromIndex, int toIndex, long val)
	  {
		Debug.Assert(val == (val & 0xFFL));
		Arrays.fill(Values, fromIndex, toIndex, (sbyte) val);
	  }
	}

}