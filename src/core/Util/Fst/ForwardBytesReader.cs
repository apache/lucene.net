using System;

namespace Lucene.Net.Util.Fst
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

	// TODO: can we use just ByteArrayDataInput...?  need to
	// add a .skipBytes to DataInput.. hmm and .setPosition

	/// <summary>
	/// Reads from a single byte[]. </summary>
	internal sealed class ForwardBytesReader : FST.BytesReader
	{
	  private readonly byte[] Bytes;
	  private int Pos;

      public ForwardBytesReader(byte[] bytes)
	  {
		this.Bytes = bytes;
	  }

      public override byte ReadByte()
	  {
		return Bytes[Pos++];
	  }

      public override void ReadBytes(byte[] b, int offset, int len)
	  {
		Array.Copy(Bytes, Pos, b, offset, len);
		Pos += len;
	  }

	  public override void SkipBytes(int count)
	  {
		Pos += count;
	  }

	  public override long Position
	  {
		  get
		  {
			return Pos;
		  }
		  set
		  {
			this.Pos = (int) value;
		  }
	  }


	  public override bool Reversed()
	  {
		return false;
	  }
	}

}