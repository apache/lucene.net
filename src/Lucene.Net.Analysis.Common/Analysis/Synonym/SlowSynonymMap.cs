﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.apache.lucene.analysis.util;

namespace Lucene.Net.Analysis.Synonym
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
    /// <summary>
	/// Mapping rules for use with <seealso cref="SlowSynonymFilter"/> </summary>
	/// @deprecated (3.4) use <seealso cref="SynonymFilterFactory"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0 
	[Obsolete("(3.4) use <seealso cref="SynonymFilterFactory"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0")]
	internal class SlowSynonymMap
	{
	  /// <summary>
	  /// @lucene.internal </summary>
	  public CharArrayMap<SlowSynonymMap> submap; // recursive: Map<String, SynonymMap>
	  /// <summary>
	  /// @lucene.internal </summary>
	  public Token[] synonyms;
	  internal int flags;

	  internal const int INCLUDE_ORIG = 0x01;
	  internal const int IGNORE_CASE = 0x02;

	  public SlowSynonymMap()
	  {
	  }
	  public SlowSynonymMap(bool ignoreCase)
	  {
		if (ignoreCase_Renamed)
		{
			flags |= IGNORE_CASE;
		}
	  }

	  public virtual bool includeOrig()
	  {
		  return (flags & INCLUDE_ORIG) != 0;
	  }
	  public virtual bool ignoreCase()
	  {
		  return (flags & IGNORE_CASE) != 0;
	  }

	  /// <param name="singleMatch">  List<String>, the sequence of strings to match </param>
	  /// <param name="replacement">  List<Token> the list of tokens to use on a match </param>
	  /// <param name="includeOrig">  sets a flag on this mapping signaling the generation of matched tokens in addition to the replacement tokens </param>
	  /// <param name="mergeExisting"> merge the replacement tokens with any other mappings that exist </param>
	  public virtual void add(IList<string> singleMatch, IList<Token> replacement, bool includeOrig, bool mergeExisting)
	  {
		SlowSynonymMap currMap = this;
		foreach (string str in singleMatch)
		{
		  if (currMap.submap == null)
		  {
			// for now hardcode at 4.0, as its what the old code did.
			// would be nice to fix, but shouldn't store a version in each submap!!!
			currMap.submap = new CharArrayMap<>(Version.LUCENE_CURRENT, 1, ignoreCase());
		  }

		  SlowSynonymMap map = currMap.submap.get(str);
		  if (map == null)
		  {
			map = new SlowSynonymMap();
			map.flags |= flags & IGNORE_CASE;
			currMap.submap.put(str, map);
		  }

		  currMap = map;
		}

		if (currMap.synonyms != null && !mergeExisting)
		{
		  throw new System.ArgumentException("SynonymFilter: there is already a mapping for " + singleMatch);
		}
		IList<Token> superset = currMap.synonyms == null ? replacement : mergeTokens(currMap.synonyms, replacement);
		currMap.synonyms = superset.ToArray();
		if (includeOrig_Renamed)
		{
			currMap.flags |= INCLUDE_ORIG;
		}
	  }


	  public override string ToString()
	  {
		StringBuilder sb = new StringBuilder("<");
		if (synonyms != null)
		{
		  sb.Append("[");
		  for (int i = 0; i < synonyms.Length; i++)
		  {
			if (i != 0)
			{
				sb.Append(',');
			}
			sb.Append(synonyms[i]);
		  }
		  if ((flags & INCLUDE_ORIG) != 0)
		  {
			sb.Append(",ORIG");
		  }
		  sb.Append("],");
		}
		sb.Append(submap);
		sb.Append(">");
		return sb.ToString();
	  }



	  /// <summary>
	  /// Produces a List<Token> from a List<String> </summary>
	  public static IList<Token> makeTokens(IList<string> strings)
	  {
		IList<Token> ret = new List<Token>(strings.Count);
		foreach (string str in strings)
		{
		  //Token newTok = new Token(str,0,0,"SYNONYM");
		  Token newTok = new Token(str, 0,0,"SYNONYM");
		  ret.Add(newTok);
		}
		return ret;
	  }


	  /// <summary>
	  /// Merge two lists of tokens, producing a single list with manipulated positionIncrements so that
	  /// the tokens end up at the same position.
	  /// 
	  /// Example:  [a b] merged with [c d] produces [a/b c/d]  ('/' denotes tokens in the same position)
	  /// Example:  [a,5 b,2] merged with [c d,4 e,4] produces [c a,5/d b,2 e,2]  (a,n means a has posInc=n)
	  /// 
	  /// </summary>
	  public static IList<Token> mergeTokens(IList<Token> lst1, IList<Token> lst2)
	  {
		List<Token> result = new List<Token>();
		if (lst1 == null || lst2 == null)
		{
		  if (lst2 != null)
		  {
			  result.AddRange(lst2);
		  }
		  if (lst1 != null)
		  {
			  result.AddRange(lst1);
		  }
		  return result;
		}

		int pos = 0;
		IEnumerator<Token> iter1 = lst1.GetEnumerator();
		IEnumerator<Token> iter2 = lst2.GetEnumerator();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Token tok1 = iter1.hasNext() ? iter1.next() : null;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Token tok2 = iter2.hasNext() ? iter2.next() : null;
		int pos1 = tok1 != null ? tok1.PositionIncrement : 0;
		int pos2 = tok2 != null ? tok2.PositionIncrement : 0;
		while (tok1 != null || tok2 != null)
		{
		  while (tok1 != null && (pos1 <= pos2 || tok2 == null))
		  {
			Token tok = new Token(tok1.startOffset(), tok1.endOffset(), tok1.type());
			tok.copyBuffer(tok1.buffer(), 0, tok1.length());
			tok.PositionIncrement = pos1 - pos;
			result.Add(tok);
			pos = pos1;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			tok1 = iter1.hasNext() ? iter1.next() : null;
			pos1 += tok1 != null ? tok1.PositionIncrement : 0;
		  }
		  while (tok2 != null && (pos2 <= pos1 || tok1 == null))
		  {
			Token tok = new Token(tok2.startOffset(), tok2.endOffset(), tok2.type());
			tok.copyBuffer(tok2.buffer(), 0, tok2.length());
			tok.PositionIncrement = pos2 - pos;
			result.Add(tok);
			pos = pos2;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			tok2 = iter2.hasNext() ? iter2.next() : null;
			pos2 += tok2 != null ? tok2.PositionIncrement : 0;
		  }
		}
		return result;
	  }

	}

}