﻿using System;

/* The following code was generated by JFlex 1.5.1 */

namespace org.apache.lucene.analysis.standard
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
	
	WARNING: if you change ClassicTokenizerImpl.jflex and need to regenerate
	      the tokenizer, only use the trunk version of JFlex 1.5 at the moment!
	
	*/

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	/// <summary>
	/// This class implements the classic lucene StandardTokenizer up until 3.0 
	/// </summary>

	internal class ClassicTokenizerImpl : StandardTokenizerInterface
	{

	  /// <summary>
	  /// This character denotes the end of file </summary>
	  public const int StandardTokenizerInterface_Fields;

	  /// <summary>
	  /// initial size of the lookahead buffer </summary>
	  private const int ZZ_BUFFERSIZE = 4096;

	  /// <summary>
	  /// lexical states </summary>
	  public const int YYINITIAL = 0;

	  /// <summary>
	  /// ZZ_LEXSTATE[l] is the state in the DFA for the lexical state l
	  /// ZZ_LEXSTATE[l+1] is the state in the DFA for the lexical state l
	  ///                  at the beginning of a line
	  /// l is of the form l = 2*k, k a non negative integer
	  /// </summary>
	  private static readonly int[] ZZ_LEXSTATE = {};

	  /// <summary>
	  /// Translates characters to character classes
	  /// </summary>
	  private const string ZZ_CMAP_PACKED = "\x0026\0\x0001\x0005\x0001\x0003\x0004\0\x0001\x0009\x0001\x0007\x0001\x0004\x0001\x0009\x000A\x0002\x0006\0" + "\x0001\x0006\x001A\x000A\x0004\0\x0001\x0008\x0001\0\x001A\x000A\x002F\0\x0001\x000A\x000A\0\x0001\x000A" + "\x0004\0\x0001\x000A\x0005\0\x0017\x000A\x0001\0\x001F\x000A\x0001\0\u0128\x000A\x0002\0\x0012\x000A" + "\x001C\0\x005E\x000A\x0002\0\x0009\x000A\x0002\0\x0007\x000A\x000E\0\x0002\x000A\x000E\0\x0005\x000A" + "\x0009\0\x0001\x000A\x008B\0\x0001\x000A\x000B\0\x0001\x000A\x0001\0\x0003\x000A\x0001\0\x0001\x000A" + "\x0001\0\x0014\x000A\x0001\0\x002C\x000A\x0001\0\x0008\x000A\x0002\0\x001A\x000A\x000C\0\x0082\x000A" + "\x000A\0\x0039\x000A\x0002\0\x0002\x000A\x0002\0\x0002\x000A\x0003\0\x0026\x000A\x0002\0\x0002\x000A" + "\x0037\0\x0026\x000A\x0002\0\x0001\x000A\x0007\0\x0027\x000A\x0048\0\x001B\x000A\x0005\0\x0003\x000A" + "\x002E\0\x001A\x000A\x0005\0\x000B\x000A\x0015\0\x000A\x0002\x0007\0\x0063\x000A\x0001\0\x0001\x000A" + "\x000F\0\x0002\x000A\x0009\0\x000A\x0002\x0003\x000A\x0013\0\x0001\x000A\x0001\0\x001B\x000A\x0053\0" + "\x0026\x000A\u015f\0\x0035\x000A\x0003\0\x0001\x000A\x0012\0\x0001\x000A\x0007\0\x000A\x000A\x0004\0" + "\x000A\x0002\x0015\0\x0008\x000A\x0002\0\x0002\x000A\x0002\0\x0016\x000A\x0001\0\x0007\x000A\x0001\0" + "\x0001\x000A\x0003\0\x0004\x000A\x0022\0\x0002\x000A\x0001\0\x0003\x000A\x0004\0\x000A\x0002\x0002\x000A" + "\x0013\0\x0006\x000A\x0004\0\x0002\x000A\x0002\0\x0016\x000A\x0001\0\x0007\x000A\x0001\0\x0002\x000A" + "\x0001\0\x0002\x000A\x0001\0\x0002\x000A\x001F\0\x0004\x000A\x0001\0\x0001\x000A\x0007\0\x000A\x0002" + "\x0002\0\x0003\x000A\x0010\0\x0007\x000A\x0001\0\x0001\x000A\x0001\0\x0003\x000A\x0001\0\x0016\x000A" + "\x0001\0\x0007\x000A\x0001\0\x0002\x000A\x0001\0\x0005\x000A\x0003\0\x0001\x000A\x0012\0\x0001\x000A" + "\x000F\0\x0001\x000A\x0005\0\x000A\x0002\x0015\0\x0008\x000A\x0002\0\x0002\x000A\x0002\0\x0016\x000A" + "\x0001\0\x0007\x000A\x0001\0\x0002\x000A\x0002\0\x0004\x000A\x0003\0\x0001\x000A\x001E\0\x0002\x000A" + "\x0001\0\x0003\x000A\x0004\0\x000A\x0002\x0015\0\x0006\x000A\x0003\0\x0003\x000A\x0001\0\x0004\x000A" + "\x0003\0\x0002\x000A\x0001\0\x0001\x000A\x0001\0\x0002\x000A\x0003\0\x0002\x000A\x0003\0\x0003\x000A" + "\x0003\0\x0008\x000A\x0001\0\x0003\x000A\x002D\0\x0009\x0002\x0015\0\x0008\x000A\x0001\0\x0003\x000A" + "\x0001\0\x0017\x000A\x0001\0\x000A\x000A\x0001\0\x0005\x000A\x0026\0\x0002\x000A\x0004\0\x000A\x0002" + "\x0015\0\x0008\x000A\x0001\0\x0003\x000A\x0001\0\x0017\x000A\x0001\0\x000A\x000A\x0001\0\x0005\x000A" + "\x0024\0\x0001\x000A\x0001\0\x0002\x000A\x0004\0\x000A\x0002\x0015\0\x0008\x000A\x0001\0\x0003\x000A" + "\x0001\0\x0017\x000A\x0001\0\x0010\x000A\x0026\0\x0002\x000A\x0004\0\x000A\x0002\x0015\0\x0012\x000A" + "\x0003\0\x0018\x000A\x0001\0\x0009\x000A\x0001\0\x0001\x000A\x0002\0\x0007\x000A\x0039\0\x0001\x0001" + "\x0030\x000A\x0001\x0001\x0002\x000A\x000C\x0001\x0007\x000A\x0009\x0001\x000A\x0002\x0027\0\x0002\x000A\x0001\0" + "\x0001\x000A\x0002\0\x0002\x000A\x0001\0\x0001\x000A\x0002\0\x0001\x000A\x0006\0\x0004\x000A\x0001\0" + "\x0007\x000A\x0001\0\x0003\x000A\x0001\0\x0001\x000A\x0001\0\x0001\x000A\x0002\0\x0002\x000A\x0001\0" + "\x0004\x000A\x0001\0\x0002\x000A\x0009\0\x0001\x000A\x0002\0\x0005\x000A\x0001\0\x0001\x000A\x0009\0" + "\x000A\x0002\x0002\0\x0002\x000A\x0022\0\x0001\x000A\x001F\0\x000A\x0002\x0016\0\x0008\x000A\x0001\0" + "\x0022\x000A\x001D\0\x0004\x000A\x0074\0\x0022\x000A\x0001\0\x0005\x000A\x0001\0\x0002\x000A\x0015\0" + "\x000A\x0002\x0006\0\x0006\x000A\x004A\0\x0026\x000A\x000A\0\x0027\x000A\x0009\0\x005A\x000A\x0005\0" + "\x0044\x000A\x0005\0\x0052\x000A\x0006\0\x0007\x000A\x0001\0\x003F\x000A\x0001\0\x0001\x000A\x0001\0" + "\x0004\x000A\x0002\0\x0007\x000A\x0001\0\x0001\x000A\x0001\0\x0004\x000A\x0002\0\x0027\x000A\x0001\0" + "\x0001\x000A\x0001\0\x0004\x000A\x0002\0\x001F\x000A\x0001\0\x0001\x000A\x0001\0\x0004\x000A\x0002\0" + "\x0007\x000A\x0001\0\x0001\x000A\x0001\0\x0004\x000A\x0002\0\x0007\x000A\x0001\0\x0007\x000A\x0001\0" + "\x0017\x000A\x0001\0\x001F\x000A\x0001\0\x0001\x000A\x0001\0\x0004\x000A\x0002\0\x0007\x000A\x0001\0" + "\x0027\x000A\x0001\0\x0013\x000A\x000E\0\x0009\x0002\x002E\0\x0055\x000A\x000C\0\u026c\x000A\x0002\0" + "\x0008\x000A\x000A\0\x001A\x000A\x0005\0\x004B\x000A\x0095\0\x0034\x000A\x002C\0\x000A\x0002\x0026\0" + "\x000A\x0002\x0006\0\x0058\x000A\x0008\0\x0029\x000A\u0557\0\x009C\x000A\x0004\0\x005A\x000A\x0006\0" + "\x0016\x000A\x0002\0\x0006\x000A\x0002\0\x0026\x000A\x0002\0\x0006\x000A\x0002\0\x0008\x000A\x0001\0" + "\x0001\x000A\x0001\0\x0001\x000A\x0001\0\x0001\x000A\x0001\0\x001F\x000A\x0002\0\x0035\x000A\x0001\0" + "\x0007\x000A\x0001\0\x0001\x000A\x0003\0\x0003\x000A\x0001\0\x0007\x000A\x0003\0\x0004\x000A\x0002\0" + "\x0006\x000A\x0004\0\x000D\x000A\x0005\0\x0003\x000A\x0001\0\x0007\x000A\x0082\0\x0001\x000A\x0082\0" + "\x0001\x000A\x0004\0\x0001\x000A\x0002\0\x000A\x000A\x0001\0\x0001\x000A\x0003\0\x0005\x000A\x0006\0" + "\x0001\x000A\x0001\0\x0001\x000A\x0001\0\x0001\x000A\x0001\0\x0004\x000A\x0001\0\x0003\x000A\x0001\0" + "\x0007\x000A\u0ecb\0\x0002\x000A\x002A\0\x0005\x000A\x000A\0\x0001\x000B\x0054\x000B\x0008\x000B\x0002\x000B" + "\x0002\x000B\x005A\x000B\x0001\x000B\x0003\x000B\x0006\x000B\x0028\x000B\x0003\x000B\x0001\0\x005E\x000A\x0011\0" + "\x0018\x000A\x0038\0\x0010\x000B\u0100\0\x0080\x000B\x0080\0\u19b6\x000B\x000A\x000B\x0040\0\u51a6\x000B" + "\x005A\x000B\u048d\x000A\u0773\0\u2ba4\x000A\u215c\0\u012e\x000B\x00D2\x000B\x0007\x000A\x000C\0\x0005\x000A" + "\x0005\0\x0001\x000A\x0001\0\x000A\x000A\x0001\0\x000D\x000A\x0001\0\x0005\x000A\x0001\0\x0001\x000A" + "\x0001\0\x0002\x000A\x0001\0\x0002\x000A\x0001\0\x006C\x000A\x0021\0\u016b\x000A\x0012\0\x0040\x000A" + "\x0002\0\x0036\x000A\x0028\0\x000C\x000A\x0074\0\x0003\x000A\x0001\0\x0001\x000A\x0001\0\x0087\x000A" + "\x0013\0\x000A\x0002\x0007\0\x001A\x000A\x0006\0\x001A\x000A\x000A\0\x0001\x000B\x003A\x000B\x001F\x000A" + "\x0003\0\x0006\x000A\x0002\0\x0006\x000A\x0002\0\x0006\x000A\x0002\0\x0003\x000A\x0023\0";

	  /// <summary>
	  /// Translates characters to character classes
	  /// </summary>
	  private static readonly char[] ZZ_CMAP = zzUnpackCMap(ZZ_CMAP_PACKED);

	  /// <summary>
	  /// Translates DFA states to action switch labels.
	  /// </summary>
	  private static readonly int[] ZZ_ACTION = zzUnpackAction();

	  private const string ZZ_ACTION_PACKED_0 = "\x0001\0\x0001\x0001\x0003\x0002\x0001\x0003\x000B\0\x0001\x0002\x0003\x0004\x0002\0" + "\x0001\x0005\x0001\0\x0001\x0005\x0003\x0004\x0006\x0005\x0001\x0006\x0001\x0004\x0002\x0007" + "\x0001\x0008\x0001\0\x0001\x0008\x0003\0\x0002\x0008\x0001\x0009\x0001\x000A\x0001\x0004";

	  private static int [] zzUnpackAction()
	  {
		int[] result = new int[50];
		int offset = 0;
		offset = zzUnpackAction(ZZ_ACTION_PACKED_0, offset, result);
		return result;
	  }

	  private static int zzUnpackAction(string packed, int offset, int[] result)
	  {
		int i = 0; // index in packed string
		int j = offset; // index in unpacked array
		int l = packed.Length;
		while (i < l)
		{
		  int count = packed[i++];
		  int value = packed[i++];
		  do
		  {
			  result[j++] = value;
		  } while (--count > 0);
		}
		return j;
	  }


	  /// <summary>
	  /// Translates a state to a row index in the transition table
	  /// </summary>
	  private static readonly int[] ZZ_ROWMAP = zzUnpackRowMap();

	  private const string ZZ_ROWMAP_PACKED_0 = "\0\0\0\x000C\0\x0018\0\x0024\0\x0030\0\x000C\0\x003C\0\x0048" + "\0\x0054\0\x0060\0\x006C\0\x0078\0\x0084\0\x0090\0\x009C\0\x00A8" + "\0\x00B4\0\x00C0\0\x00CC\0\x00D8\0\x00E4\0\x00F0\0\x00FC\0\u0108" + "\0\u0114\0\u0120\0\u012c\0\u0138\0\u0144\0\u0150\0\u015c\0\u0168" + "\0\u0174\0\u0180\0\u018c\0\u0198\0\u01a4\0\x00A8\0\u01b0\0\u01bc" + "\0\u01c8\0\u01d4\0\u01e0\0\u01ec\0\u01f8\0\x003C\0\x006C\0\u0204" + "\0\u0210\0\u021c";

	  private static int [] zzUnpackRowMap()
	  {
		int[] result = new int[50];
		int offset = 0;
		offset = zzUnpackRowMap(ZZ_ROWMAP_PACKED_0, offset, result);
		return result;
	  }

	  private static int zzUnpackRowMap(string packed, int offset, int[] result)
	  {
		int i = 0; // index in packed string
		int j = offset; // index in unpacked array
		int l = packed.Length;
		while (i < l)
		{
		  int high = packed[i++] << 16;
		  result[j++] = high | packed[i++];
		}
		return j;
	  }

	  /// <summary>
	  /// The transition table of the DFA
	  /// </summary>
	  private static readonly int[] ZZ_TRANS = zzUnpackTrans();

	  private const string ZZ_TRANS_PACKED_0 = "\x0001\x0002\x0001\x0003\x0001\x0004\x0007\x0002\x0001\x0005\x0001\x0006\x000D\0\x0002\x0003" + "\x0001\0\x0001\x0007\x0001\0\x0001\x0008\x0002\x0009\x0001\x000A\x0001\x0003\x0002\0" + "\x0001\x0003\x0001\x0004\x0001\0\x0001\x000B\x0001\0\x0001\x0008\x0002\x000C\x0001\x000D" + "\x0001\x0004\x0002\0\x0001\x0003\x0001\x0004\x0001\x000E\x0001\x000F\x0001\x0010\x0001\x0011" + "\x0002\x0009\x0001\x000A\x0001\x0012\x0002\0\x0001\x0013\x0001\x0014\x0007\0\x0001\x0015" + "\x0002\0\x0002\x0016\x0007\0\x0001\x0016\x0002\0\x0001\x0017\x0001\x0018\x0007\0" + "\x0001\x0019\x0003\0\x0001\x001A\x0007\0\x0001\x000A\x0002\0\x0001\x001B\x0001\x001C" + "\x0007\0\x0001\x001D\x0002\0\x0001\x001E\x0001\x001F\x0007\0\x0001\x0020\x0002\0" + "\x0001\x0021\x0001\x0022\x0007\0\x0001\x0023\x000B\0\x0001\x0024\x0002\0\x0001\x0013" + "\x0001\x0014\x0007\0\x0001\x0025\x000B\0\x0001\x0026\x0002\0\x0002\x0016\x0007\0" + "\x0001\x0027\x0002\0\x0001\x0003\x0001\x0004\x0001\x000E\x0001\x0007\x0001\x0010\x0001\x0011" + "\x0002\x0009\x0001\x000A\x0001\x0012\x0002\0\x0002\x0013\x0001\0\x0001\x0028\x0001\0" + "\x0001\x0008\x0002\x0029\x0001\0\x0001\x0013\x0002\0\x0001\x0013\x0001\x0014\x0001\0" + "\x0001\x002A\x0001\0\x0001\x0008\x0002\x002B\x0001\x002C\x0001\x0014\x0002\0\x0001\x0013" + "\x0001\x0014\x0001\0\x0001\x0028\x0001\0\x0001\x0008\x0002\x0029\x0001\0\x0001\x0015" + "\x0002\0\x0002\x0016\x0001\0\x0001\x002D\x0002\0\x0001\x002D\x0002\0\x0001\x0016" + "\x0002\0\x0002\x0017\x0001\0\x0001\x0029\x0001\0\x0001\x0008\x0002\x0029\x0001\0" + "\x0001\x0017\x0002\0\x0001\x0017\x0001\x0018\x0001\0\x0001\x002B\x0001\0\x0001\x0008" + "\x0002\x002B\x0001\x002C\x0001\x0018\x0002\0\x0001\x0017\x0001\x0018\x0001\0\x0001\x0029" + "\x0001\0\x0001\x0008\x0002\x0029\x0001\0\x0001\x0019\x0003\0\x0001\x001A\x0001\0" + "\x0001\x002C\x0002\0\x0003\x002C\x0001\x001A\x0002\0\x0002\x001B\x0001\0\x0001\x002E" + "\x0001\0\x0001\x0008\x0002\x0009\x0001\x000A\x0001\x001B\x0002\0\x0001\x001B\x0001\x001C" + "\x0001\0\x0001\x002F\x0001\0\x0001\x0008\x0002\x000C\x0001\x000D\x0001\x001C\x0002\0" + "\x0001\x001B\x0001\x001C\x0001\0\x0001\x002E\x0001\0\x0001\x0008\x0002\x0009\x0001\x000A" + "\x0001\x001D\x0002\0\x0002\x001E\x0001\0\x0001\x0009\x0001\0\x0001\x0008\x0002\x0009" + "\x0001\x000A\x0001\x001E\x0002\0\x0001\x001E\x0001\x001F\x0001\0\x0001\x000C\x0001\0" + "\x0001\x0008\x0002\x000C\x0001\x000D\x0001\x001F\x0002\0\x0001\x001E\x0001\x001F\x0001\0" + "\x0001\x0009\x0001\0\x0001\x0008\x0002\x0009\x0001\x000A\x0001\x0020\x0002\0\x0002\x0021" + "\x0001\0\x0001\x000A\x0002\0\x0003\x000A\x0001\x0021\x0002\0\x0001\x0021\x0001\x0022" + "\x0001\0\x0001\x000D\x0002\0\x0003\x000D\x0001\x0022\x0002\0\x0001\x0021\x0001\x0022" + "\x0001\0\x0001\x000A\x0002\0\x0003\x000A\x0001\x0023\x0004\0\x0001\x000E\x0006\0" + "\x0001\x0024\x0002\0\x0001\x0013\x0001\x0014\x0001\0\x0001\x0030\x0001\0\x0001\x0008" + "\x0002\x0029\x0001\0\x0001\x0015\x0002\0\x0002\x0016\x0001\0\x0001\x002D\x0002\0" + "\x0001\x002D\x0002\0\x0001\x0027\x0002\0\x0002\x0013\x0007\0\x0001\x0013\x0002\0" + "\x0002\x0017\x0007\0\x0001\x0017\x0002\0\x0002\x001B\x0007\0\x0001\x001B\x0002\0" + "\x0002\x001E\x0007\0\x0001\x001E\x0002\0\x0002\x0021\x0007\0\x0001\x0021\x0002\0" + "\x0002\x0031\x0007\0\x0001\x0031\x0002\0\x0002\x0013\x0007\0\x0001\x0032\x0002\0" + "\x0002\x0031\x0001\0\x0001\x002D\x0002\0\x0001\x002D\x0002\0\x0001\x0031\x0002\0" + "\x0002\x0013\x0001\0\x0001\x0030\x0001\0\x0001\x0008\x0002\x0029\x0001\0\x0001\x0013" + "\x0001\0";

	  private static int [] zzUnpackTrans()
	  {
		int[] result = new int[552];
		int offset = 0;
		offset = zzUnpackTrans(ZZ_TRANS_PACKED_0, offset, result);
		return result;
	  }

	  private static int zzUnpackTrans(string packed, int offset, int[] result)
	  {
		int i = 0; // index in packed string
		int j = offset; // index in unpacked array
		int l = packed.Length;
		while (i < l)
		{
		  int count = packed[i++];
		  int value = packed[i++];
		  value--;
		  do
		  {
			  result[j++] = value;
		  } while (--count > 0);
		}
		return j;
	  }


	  /* error codes */
	  private const int ZZ_UNKNOWN_ERROR = 0;
	  private const int ZZ_NO_MATCH = 1;
	  private const int ZZ_PUSHBACK_2BIG = 2;

	  /* error messages for the codes above */
	  private static readonly string[] ZZ_ERROR_MSG = {};

	  /// <summary>
	  /// ZZ_ATTRIBUTE[aState] contains the attributes of state <code>aState</code>
	  /// </summary>
	  private static readonly int[] ZZ_ATTRIBUTE = zzUnpackAttribute();

	  private const string ZZ_ATTRIBUTE_PACKED_0 = "\x0001\0\x0001\x0009\x0003\x0001\x0001\x0009\x000B\0\x0004\x0001\x0002\0\x0001\x0001" + "\x0001\0\x000F\x0001\x0001\0\x0001\x0001\x0003\0\x0005\x0001";

	  private static int [] zzUnpackAttribute()
	  {
		int[] result = new int[50];
		int offset = 0;
		offset = zzUnpackAttribute(ZZ_ATTRIBUTE_PACKED_0, offset, result);
		return result;
	  }

	  private static int zzUnpackAttribute(string packed, int offset, int[] result)
	  {
		int i = 0; // index in packed string
		int j = offset; // index in unpacked array
		int l = packed.Length;
		while (i < l)
		{
		  int count = packed[i++];
		  int value = packed[i++];
		  do
		  {
			  result[j++] = value;
		  } while (--count > 0);
		}
		return j;
	  }

	  /// <summary>
	  /// the input device </summary>
	  private Reader zzReader;

	  /// <summary>
	  /// the current state of the DFA </summary>
	  private int zzState;

	  /// <summary>
	  /// the current lexical state </summary>
	  private int zzLexicalState = YYINITIAL;

	  /// <summary>
	  /// this buffer contains the current text to be matched and is
	  ///    the source of the yytext() string 
	  /// </summary>
	  private char[] zzBuffer = new char[ZZ_BUFFERSIZE];

	  /// <summary>
	  /// the textposition at the last accepting state </summary>
	  private int zzMarkedPos;

	  /// <summary>
	  /// the current text position in the buffer </summary>
	  private int zzCurrentPos;

	  /// <summary>
	  /// startRead marks the beginning of the yytext() string in the buffer </summary>
	  private int zzStartRead;

	  /// <summary>
	  /// endRead marks the last character in the buffer, that has been read
	  ///    from input 
	  /// </summary>
	  private int zzEndRead;

	  /// <summary>
	  /// number of newlines encountered up to the start of the matched text </summary>
	  private int yyline;

	  /// <summary>
	  /// the number of characters up to the start of the matched text </summary>
	  private int yychar_Renamed;

	  /// <summary>
	  /// the number of characters from the last newline up to the start of the 
	  /// matched text
	  /// </summary>
	  private int yycolumn;

	  /// <summary>
	  /// zzAtBOL == true <=> the scanner is currently at the beginning of a line
	  /// </summary>
	  private bool zzAtBOL = true;

	  /// <summary>
	  /// zzAtEOF == true <=> the scanner is at the EOF </summary>
	  private bool zzAtEOF;

	  /// <summary>
	  /// denotes if the user-EOF-code has already been executed </summary>
	  private bool zzEOFDone;

	  /* user code: */

	public const int ALPHANUM = StandardTokenizer.ALPHANUM;
	public const int APOSTROPHE = StandardTokenizer.APOSTROPHE;
	public const int ACRONYM = StandardTokenizer.ACRONYM;
	public const int COMPANY = StandardTokenizer.COMPANY;
	public const int EMAIL = StandardTokenizer.EMAIL;
	public const int HOST = StandardTokenizer.HOST;
	public const int NUM = StandardTokenizer.NUM;
	public const int CJ = StandardTokenizer.CJ;
	public const int ACRONYM_DEP = StandardTokenizer.ACRONYM_DEP;

	public static readonly string[] TOKEN_TYPES = StandardTokenizer.TOKEN_TYPES;

	public int yychar()
	{
		return yychar_Renamed;
	}

	/// <summary>
	/// Fills CharTermAttribute with the current token text.
	/// </summary>
	public void getText(CharTermAttribute t)
	{
	  t.copyBuffer(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
	}



	  /// <summary>
	  /// Creates a new scanner
	  /// </summary>
	  /// <param name="in">  the java.io.Reader to read input from. </param>
	  internal ClassicTokenizerImpl(Reader @in)
	  {
		this.zzReader = @in;
	  }


	  /// <summary>
	  /// Unpacks the compressed character translation table.
	  /// </summary>
	  /// <param name="packed">   the packed character translation table </param>
	  /// <returns>         the unpacked character translation table </returns>
	  private static char [] zzUnpackCMap(string packed)
	  {
		char[] map = new char[0x10000];
		int i = 0; // index in packed string
		int j = 0; // index in unpacked array
		while (i < 1138)
		{
		  int count = packed[i++];
		  char value = packed[i++];
		  do
		  {
			  map[j++] = value;
		  } while (--count > 0);
		}
		return map;
	  }


	  /// <summary>
	  /// Refills the input buffer.
	  /// </summary>
	  /// <returns>      <code>false</code>, iff there was new input.
	  /// </returns>
	  /// <exception cref="java.io.IOException">  if any I/O-Error occurs </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private boolean zzRefill() throws java.io.IOException
	  private bool zzRefill()
	  {

		/* first: make room (if you can) */
		if (zzStartRead > 0)
		{
		  Array.Copy(zzBuffer, zzStartRead, zzBuffer, 0, zzEndRead - zzStartRead);

		  /* translate stored positions */
		  zzEndRead -= zzStartRead;
		  zzCurrentPos -= zzStartRead;
		  zzMarkedPos -= zzStartRead;
		  zzStartRead = 0;
		}

		/* is the buffer big enough? */
		if (zzCurrentPos >= zzBuffer.Length)
		{
		  /* if not: blow it up */
		  char[] newBuffer = new char[zzCurrentPos * 2];
		  Array.Copy(zzBuffer, 0, newBuffer, 0, zzBuffer.Length);
		  zzBuffer = newBuffer;
		}

		/* finally: fill the buffer with new input */
		int numRead = zzReader.read(zzBuffer, zzEndRead, zzBuffer.Length - zzEndRead);

		if (numRead > 0)
		{
		  zzEndRead += numRead;
		  return false;
		}
		// unlikely but not impossible: read 0 characters, but not at end of stream    
		if (numRead == 0)
		{
		  int c = zzReader.read();
		  if (c == -1)
		  {
			return true;
		  }
		  else
		  {
			zzBuffer[zzEndRead++] = (char) c;
			return false;
		  }
		}

		// numRead < 0
		return true;
	  }


	  /// <summary>
	  /// Closes the input stream.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void yyclose() throws java.io.IOException
	  public void yyclose()
	  {
		zzAtEOF = true; // indicate end of file
		zzEndRead = zzStartRead; // invalidate buffer

		if (zzReader != null)
		{
		  zzReader.close();
		}
	  }


	  /// <summary>
	  /// Resets the scanner to read from a new input stream.
	  /// Does not close the old reader.
	  /// 
	  /// All internal variables are reset, the old input stream 
	  /// <b>cannot</b> be reused (internal buffer is discarded and lost).
	  /// Lexical state is set to <tt>ZZ_INITIAL</tt>.
	  /// 
	  /// Internal scan buffer is resized down to its initial length, if it has grown.
	  /// </summary>
	  /// <param name="reader">   the new input stream  </param>
	  public void yyreset(Reader reader)
	  {
		zzReader = reader;
		zzAtBOL = true;
		zzAtEOF = false;
		zzEOFDone = false;
		zzEndRead = zzStartRead = 0;
		zzCurrentPos = zzMarkedPos = 0;
		yyline = yychar_Renamed = yycolumn = 0;
		zzLexicalState = YYINITIAL;
		if (zzBuffer.Length > ZZ_BUFFERSIZE)
		{
		  zzBuffer = new char[ZZ_BUFFERSIZE];
		}
	  }


	  /// <summary>
	  /// Returns the current lexical state.
	  /// </summary>
	  public int yystate()
	  {
		return zzLexicalState;
	  }


	  /// <summary>
	  /// Enters a new lexical state
	  /// </summary>
	  /// <param name="newState"> the new lexical state </param>
	  public void yybegin(int newState)
	  {
		zzLexicalState = newState;
	  }


	  /// <summary>
	  /// Returns the text matched by the current regular expression.
	  /// </summary>
	  public string yytext()
	  {
		return new string(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
	  }


	  /// <summary>
	  /// Returns the character at position <tt>pos</tt> from the 
	  /// matched text. 
	  /// 
	  /// It is equivalent to yytext().charAt(pos), but faster
	  /// </summary>
	  /// <param name="pos"> the position of the character to fetch. 
	  ///            A value from 0 to yylength()-1.
	  /// </param>
	  /// <returns> the character at position pos </returns>
	  public char yycharat(int pos)
	  {
		return zzBuffer[zzStartRead + pos];
	  }


	  /// <summary>
	  /// Returns the length of the matched text region.
	  /// </summary>
	  public int yylength()
	  {
		return zzMarkedPos - zzStartRead;
	  }


	  /// <summary>
	  /// Reports an error that occured while scanning.
	  /// 
	  /// In a wellformed scanner (no or only correct usage of 
	  /// yypushback(int) and a match-all fallback rule) this method 
	  /// will only be called with things that "Can't Possibly Happen".
	  /// If this method is called, something is seriously wrong
	  /// (e.g. a JFlex bug producing a faulty scanner etc.).
	  /// 
	  /// Usual syntax/scanner level error handling should be done
	  /// in error fallback rules.
	  /// </summary>
	  /// <param name="errorCode">  the code of the errormessage to display </param>
	  private void zzScanError(int errorCode)
	  {
		string message;
		try
		{
		  message = ZZ_ERROR_MSG[errorCode];
		}
		catch (System.IndexOutOfRangeException)
		{
		  message = ZZ_ERROR_MSG[ZZ_UNKNOWN_ERROR];
		}

		throw new Exception(message);
	  }


	  /// <summary>
	  /// Pushes the specified amount of characters back into the input stream.
	  /// 
	  /// They will be read again by then next call of the scanning method
	  /// </summary>
	  /// <param name="number">  the number of characters to be read again.
	  ///                This number must not be greater than yylength()! </param>
	  public virtual void yypushback(int number)
	  {
		if (number > yylength())
		{
		  zzScanError(ZZ_PUSHBACK_2BIG);
		}

		zzMarkedPos -= number;
	  }


	  /// <summary>
	  /// Resumes scanning until the next regular expression is matched,
	  /// the end of input is encountered or an I/O-Error occurs.
	  /// </summary>
	  /// <returns>      the next token </returns>
	  /// <exception cref="java.io.IOException">  if any I/O-Error occurs </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int getNextToken() throws java.io.IOException
	  public virtual int NextToken
	  {
		  get
		  {
			int zzInput;
			int zzAction;
    
			// cached fields:
			int zzCurrentPosL;
			int zzMarkedPosL;
			int zzEndReadL = zzEndRead;
			char[] zzBufferL = zzBuffer;
			char[] zzCMapL = ZZ_CMAP;
    
			int[] zzTransL = ZZ_TRANS;
			int[] zzRowMapL = ZZ_ROWMAP;
			int[] zzAttrL = ZZ_ATTRIBUTE;
    
			while (true)
			{
			  zzMarkedPosL = zzMarkedPos;
    
			  yychar_Renamed += zzMarkedPosL - zzStartRead;
    
			  zzAction = -1;
    
			  zzCurrentPosL = zzCurrentPos = zzStartRead = zzMarkedPosL;
    
			  zzState = ZZ_LEXSTATE[zzLexicalState];
    
			  // set up zzAction for empty match case:
			  int zzAttributes = zzAttrL[zzState];
			  if ((zzAttributes & 1) == 1)
			  {
				zzAction = zzState;
			  }
    
    
			  {
				while (true)
				{
    
				  if (zzCurrentPosL < zzEndReadL)
				  {
					zzInput = zzBufferL[zzCurrentPosL++];
				  }
				  else if (zzAtEOF)
				  {
					zzInput = StandardTokenizerInterface_Fields.YYEOF;
					goto zzForActionBreak;
				  }
				  else
				  {
					// store back cached positions
					zzCurrentPos = zzCurrentPosL;
					zzMarkedPos = zzMarkedPosL;
					bool eof = zzRefill();
					// get translated positions and possibly new buffer
					zzCurrentPosL = zzCurrentPos;
					zzMarkedPosL = zzMarkedPos;
					zzBufferL = zzBuffer;
					zzEndReadL = zzEndRead;
					if (eof)
					{
					  zzInput = StandardTokenizerInterface_Fields.YYEOF;
					  goto zzForActionBreak;
					}
					else
					{
					  zzInput = zzBufferL[zzCurrentPosL++];
					}
				  }
				  int zzNext = zzTransL[zzRowMapL[zzState] + zzCMapL[zzInput]];
				  if (zzNext == -1)
				  {
					  goto zzForActionBreak;
				  }
				  zzState = zzNext;
    
				  zzAttributes = zzAttrL[zzState];
				  if ((zzAttributes & 1) == 1)
				  {
					zzAction = zzState;
					zzMarkedPosL = zzCurrentPosL;
					if ((zzAttributes & 8) == 8)
					{
						goto zzForActionBreak;
					}
				  }
    
				}
			  }
			  zzForActionBreak:
    
			  // store back cached position
			  zzMarkedPos = zzMarkedPosL;
    
			  switch (zzAction < 0 ? zzAction : ZZ_ACTION[zzAction])
			  {
				case 1:
				{ // Break so we don't hit fall-through warning:
		 break; // ignore
				}
					goto case 11;
				case 11:
					break;
				case 2:
				{
					  return ALPHANUM;
				}
				case 12:
					break;
				case 3:
				{
					  return CJ;
				}
				case 13:
					break;
				case 4:
				{
					  return HOST;
				}
				case 14:
					break;
				case 5:
				{
					  return NUM;
				}
				case 15:
					break;
				case 6:
				{
					  return APOSTROPHE;
				}
				case 16:
					break;
				case 7:
				{
					  return COMPANY;
				}
				case 17:
					break;
				case 8:
				{
					  return ACRONYM_DEP;
				}
				case 18:
					break;
				case 9:
				{
					  return ACRONYM;
				}
				case 19:
					break;
				case 10:
				{
					  return EMAIL;
				}
				case 20:
					break;
				default:
				  if (zzInput == StandardTokenizerInterface_Fields.YYEOF && zzStartRead == zzCurrentPos)
				  {
					zzAtEOF = true;
					return StandardTokenizerInterface_Fields.YYEOF;
				  }
				  else
				  {
					zzScanError(ZZ_NO_MATCH);
				  }
			  break;
			  }
			}
		  }
	  }


	}

}