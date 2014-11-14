﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.analysis.hunspell
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


	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ByteArrayDataInput = org.apache.lucene.store.ByteArrayDataInput;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using Version = org.apache.lucene.util.Version;
	using CharacterRunAutomaton = org.apache.lucene.util.automaton.CharacterRunAutomaton;

	/// <summary>
	/// Stemmer uses the affix rules declared in the Dictionary to generate one or more stems for a word.  It
	/// conforms to the algorithm in the original hunspell algorithm, including recursive suffix stripping.
	/// </summary>
	internal sealed class Stemmer
	{
	  private readonly Dictionary dictionary;
	  private readonly BytesRef scratch = new BytesRef();
	  private readonly StringBuilder segment = new StringBuilder();
	  private readonly ByteArrayDataInput affixReader;

	  // used for normalization
	  private readonly StringBuilder scratchSegment = new StringBuilder();
	  private char[] scratchBuffer = new char[32];

	  /// <summary>
	  /// Constructs a new Stemmer which will use the provided Dictionary to create its stems.
	  /// </summary>
	  /// <param name="dictionary"> Dictionary that will be used to create the stems </param>
	  public Stemmer(Dictionary dictionary)
	  {
		this.dictionary = dictionary;
		this.affixReader = new ByteArrayDataInput(dictionary.affixData);
	  }

	  /// <summary>
	  /// Find the stem(s) of the provided word.
	  /// </summary>
	  /// <param name="word"> Word to find the stems for </param>
	  /// <returns> List of stems for the word </returns>
	  public IList<CharsRef> stem(string word)
	  {
		return stem(word.ToCharArray(), word.Length);
	  }

	  /// <summary>
	  /// Find the stem(s) of the provided word
	  /// </summary>
	  /// <param name="word"> Word to find the stems for </param>
	  /// <returns> List of stems for the word </returns>
	  public IList<CharsRef> stem(char[] word, int length)
	  {

		if (dictionary.needsInputCleaning)
		{
		  scratchSegment.Length = 0;
		  scratchSegment.Append(word, 0, length);
		  CharSequence cleaned = dictionary.cleanInput(scratchSegment, segment);
		  scratchBuffer = ArrayUtil.grow(scratchBuffer, cleaned.length());
		  length = segment.Length;
		  segment.getChars(0, length, scratchBuffer, 0);
		  word = scratchBuffer;
		}

		IList<CharsRef> stems = new List<CharsRef>();
		IntsRef forms = dictionary.lookupWord(word, 0, length);
		if (forms != null)
		{
		  // TODO: some forms should not be added, e.g. ONLYINCOMPOUND
		  // just because it exists, does not make it valid...
		  for (int i = 0; i < forms.length; i++)
		  {
			stems.Add(newStem(word, length));
		  }
		}
		stems.AddRange(stem(word, length, -1, -1, -1, 0, true, true, false, false));
		return stems;
	  }

	  /// <summary>
	  /// Find the unique stem(s) of the provided word
	  /// </summary>
	  /// <param name="word"> Word to find the stems for </param>
	  /// <returns> List of stems for the word </returns>
	  public IList<CharsRef> uniqueStems(char[] word, int length)
	  {
		IList<CharsRef> stems = stem(word, length);
		if (stems.Count < 2)
		{
		  return stems;
		}
		CharArraySet terms = new CharArraySet(Version.LUCENE_CURRENT, 8, dictionary.ignoreCase);
		IList<CharsRef> deduped = new List<CharsRef>();
		foreach (CharsRef s in stems)
		{
		  if (!terms.contains(s))
		  {
			deduped.Add(s);
			terms.add(s);
		  }
		}
		return deduped;
	  }

	  private CharsRef newStem(char[] buffer, int length)
	  {
		if (dictionary.needsOutputCleaning)
		{
		  scratchSegment.Length = 0;
		  scratchSegment.Append(buffer, 0, length);
		  try
		  {
			Dictionary.applyMappings(dictionary.oconv, scratchSegment);
		  }
		  catch (IOException bogus)
		  {
			throw new Exception(bogus);
		  }
		  char[] cleaned = new char[scratchSegment.Length];
		  scratchSegment.getChars(0, cleaned.Length, cleaned, 0);
		  return new CharsRef(cleaned, 0, cleaned.Length);
		}
		else
		{
		  return new CharsRef(buffer, 0, length);
		}
	  }

	  // ================================================= Helper Methods ================================================

	  /// <summary>
	  /// Generates a list of stems for the provided word
	  /// </summary>
	  /// <param name="word"> Word to generate the stems for </param>
	  /// <param name="previous"> previous affix that was removed (so we dont remove same one twice) </param>
	  /// <param name="prevFlag"> Flag from a previous stemming step that need to be cross-checked with any affixes in this recursive step </param>
	  /// <param name="prefixFlag"> flag of the most inner removed prefix, so that when removing a suffix, its also checked against the word </param>
	  /// <param name="recursionDepth"> current recursiondepth </param>
	  /// <param name="doPrefix"> true if we should remove prefixes </param>
	  /// <param name="doSuffix"> true if we should remove suffixes </param>
	  /// <param name="previousWasPrefix"> true if the previous removal was a prefix:
	  ///        if we are removing a suffix, and it has no continuation requirements, its ok.
	  ///        but two prefixes (COMPLEXPREFIXES) or two suffixes must have continuation requirements to recurse. </param>
	  /// <param name="circumfix"> true if the previous prefix removal was signed as a circumfix
	  ///        this means inner most suffix must also contain circumfix flag. </param>
	  /// <returns> List of stems, or empty list if no stems are found </returns>
	  private IList<CharsRef> stem(char[] word, int length, int previous, int prevFlag, int prefixFlag, int recursionDepth, bool doPrefix, bool doSuffix, bool previousWasPrefix, bool circumfix)
	  {

		// TODO: allow this stuff to be reused by tokenfilter
		IList<CharsRef> stems = new List<CharsRef>();

		if (doPrefix && dictionary.prefixes != null)
		{
		  for (int i = length - 1; i >= 0; i--)
		  {
			IntsRef prefixes = dictionary.lookupPrefix(word, 0, i);
			if (prefixes == null)
			{
			  continue;
			}

			for (int j = 0; j < prefixes.length; j++)
			{
			  int prefix = prefixes.ints[prefixes.offset + j];
			  if (prefix == previous)
			  {
				continue;
			  }
			  affixReader.Position = 8 * prefix;
			  char flag = (char)(affixReader.readShort() & 0xffff);
			  char stripOrd = (char)(affixReader.readShort() & 0xffff);
			  int condition = (char)(affixReader.readShort() & 0xffff);
			  bool crossProduct = (condition & 1) == 1;
			  condition = (int)((uint)condition >> 1);
			  char append = (char)(affixReader.readShort() & 0xffff);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean compatible;
			  bool compatible;
			  if (recursionDepth == 0)
			  {
				compatible = true;
			  }
			  else if (crossProduct)
			  {
				// cross check incoming continuation class (flag of previous affix) against list.
				dictionary.flagLookup.get(append, scratch);
				char[] appendFlags = Dictionary.decodeFlags(scratch);
				Debug.Assert(prevFlag >= 0);
				compatible = hasCrossCheckedFlag((char)prevFlag, appendFlags, false);
			  }
			  else
			  {
				compatible = false;
			  }

			  if (compatible)
			  {
				int deAffixedStart = i;
				int deAffixedLength = length - deAffixedStart;

				int stripStart = dictionary.stripOffsets[stripOrd];
				int stripEnd = dictionary.stripOffsets[stripOrd + 1];
				int stripLength = stripEnd - stripStart;

				if (!checkCondition(condition, dictionary.stripData, stripStart, stripLength, word, deAffixedStart, deAffixedLength))
				{
				  continue;
				}

				char[] strippedWord = new char[stripLength + deAffixedLength];
				Array.Copy(dictionary.stripData, stripStart, strippedWord, 0, stripLength);
				Array.Copy(word, deAffixedStart, strippedWord, stripLength, deAffixedLength);

				IList<CharsRef> stemList = applyAffix(strippedWord, strippedWord.Length, prefix, -1, recursionDepth, true, circumfix);

				stems.AddRange(stemList);
			  }
			}
		  }
		}

		if (doSuffix && dictionary.suffixes != null)
		{
		  for (int i = 0; i < length; i++)
		  {
			IntsRef suffixes = dictionary.lookupSuffix(word, i, length - i);
			if (suffixes == null)
			{
			  continue;
			}

			for (int j = 0; j < suffixes.length; j++)
			{
			  int suffix = suffixes.ints[suffixes.offset + j];
			  if (suffix == previous)
			  {
				continue;
			  }
			  affixReader.Position = 8 * suffix;
			  char flag = (char)(affixReader.readShort() & 0xffff);
			  char stripOrd = (char)(affixReader.readShort() & 0xffff);
			  int condition = (char)(affixReader.readShort() & 0xffff);
			  bool crossProduct = (condition & 1) == 1;
			  condition = (int)((uint)condition >> 1);
			  char append = (char)(affixReader.readShort() & 0xffff);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean compatible;
			  bool compatible;
			  if (recursionDepth == 0)
			  {
				compatible = true;
			  }
			  else if (crossProduct)
			  {
				// cross check incoming continuation class (flag of previous affix) against list.
				dictionary.flagLookup.get(append, scratch);
				char[] appendFlags = Dictionary.decodeFlags(scratch);
				Debug.Assert(prevFlag >= 0);
				compatible = hasCrossCheckedFlag((char)prevFlag, appendFlags, previousWasPrefix);
			  }
			  else
			  {
				compatible = false;
			  }

			  if (compatible)
			  {
				int appendLength = length - i;
				int deAffixedLength = length - appendLength;

				int stripStart = dictionary.stripOffsets[stripOrd];
				int stripEnd = dictionary.stripOffsets[stripOrd + 1];
				int stripLength = stripEnd - stripStart;

				if (!checkCondition(condition, word, 0, deAffixedLength, dictionary.stripData, stripStart, stripLength))
				{
				  continue;
				}

				char[] strippedWord = new char[stripLength + deAffixedLength];
				Array.Copy(word, 0, strippedWord, 0, deAffixedLength);
				Array.Copy(dictionary.stripData, stripStart, strippedWord, deAffixedLength, stripLength);

				IList<CharsRef> stemList = applyAffix(strippedWord, strippedWord.Length, suffix, prefixFlag, recursionDepth, false, circumfix);

				stems.AddRange(stemList);
			  }
			}
		  }
		}

		return stems;
	  }

	  /// <summary>
	  /// checks condition of the concatenation of two strings </summary>
	  // note: this is pretty stupid, we really should subtract strip from the condition up front and just check the stem
	  // but this is a little bit more complicated.
	  private bool checkCondition(int condition, char[] c1, int c1off, int c1len, char[] c2, int c2off, int c2len)
	  {
		if (condition != 0)
		{
		  CharacterRunAutomaton pattern = dictionary.patterns[condition];
		  int state = pattern.InitialState;
		  for (int i = c1off; i < c1off + c1len; i++)
		  {
			state = pattern.step(state, c1[i]);
			if (state == -1)
			{
			  return false;
			}
		  }
		  for (int i = c2off; i < c2off + c2len; i++)
		  {
			state = pattern.step(state, c2[i]);
			if (state == -1)
			{
			  return false;
			}
		  }
		  return pattern.isAccept(state);
		}
		return true;
	  }

	  /// <summary>
	  /// Applies the affix rule to the given word, producing a list of stems if any are found
	  /// </summary>
	  /// <param name="strippedWord"> Word the affix has been removed and the strip added </param>
	  /// <param name="length"> valid length of stripped word </param>
	  /// <param name="affix"> HunspellAffix representing the affix rule itself </param>
	  /// <param name="prefixFlag"> when we already stripped a prefix, we cant simply recurse and check the suffix, unless both are compatible
	  ///                   so we must check dictionary form against both to add it as a stem! </param>
	  /// <param name="recursionDepth"> current recursion depth </param>
	  /// <param name="prefix"> true if we are removing a prefix (false if its a suffix) </param>
	  /// <returns> List of stems for the word, or an empty list if none are found </returns>
	  internal IList<CharsRef> applyAffix(char[] strippedWord, int length, int affix, int prefixFlag, int recursionDepth, bool prefix, bool circumfix)
	  {
		// TODO: just pass this in from before, no need to decode it twice
		affixReader.Position = 8 * affix;
		char flag = (char)(affixReader.readShort() & 0xffff);
		affixReader.skipBytes(2); // strip
		int condition = (char)(affixReader.readShort() & 0xffff);
		bool crossProduct = (condition & 1) == 1;
		condition = (int)((uint)condition >> 1);
		char append = (char)(affixReader.readShort() & 0xffff);

		IList<CharsRef> stems = new List<CharsRef>();

		IntsRef forms = dictionary.lookupWord(strippedWord, 0, length);
		if (forms != null)
		{
		  for (int i = 0; i < forms.length; i++)
		  {
			dictionary.flagLookup.get(forms.ints[forms.offset + i], scratch);
			char[] wordFlags = Dictionary.decodeFlags(scratch);
			if (Dictionary.hasFlag(wordFlags, flag))
			{
			  // confusing: in this one exception, we already chained the first prefix against the second,
			  // so it doesnt need to be checked against the word
			  bool chainedPrefix = dictionary.complexPrefixes && recursionDepth == 1 && prefix;
			  if (chainedPrefix == false && prefixFlag >= 0 && !Dictionary.hasFlag(wordFlags, (char)prefixFlag))
			  {
				// see if we can chain prefix thru the suffix continuation class (only if it has any!)
				dictionary.flagLookup.get(append, scratch);
				char[] appendFlags = Dictionary.decodeFlags(scratch);
				if (!hasCrossCheckedFlag((char)prefixFlag, appendFlags, false))
				{
				  continue;
				}
			  }

			  // if circumfix was previously set by a prefix, we must check this suffix,
			  // to ensure it has it, and vice versa
			  if (dictionary.circumfix != -1)
			  {
				dictionary.flagLookup.get(append, scratch);
				char[] appendFlags = Dictionary.decodeFlags(scratch);
				bool suffixCircumfix = Dictionary.hasFlag(appendFlags, (char)dictionary.circumfix);
				if (circumfix != suffixCircumfix)
				{
				  continue;
				}
			  }
			  stems.Add(newStem(strippedWord, length));
			}
		  }
		}

		// if a circumfix flag is defined in the dictionary, and we are a prefix, we need to check if we have that flag
		if (dictionary.circumfix != -1 && !circumfix && prefix)
		{
		  dictionary.flagLookup.get(append, scratch);
		  char[] appendFlags = Dictionary.decodeFlags(scratch);
		  circumfix = Dictionary.hasFlag(appendFlags, (char)dictionary.circumfix);
		}

		if (crossProduct)
		{
		  if (recursionDepth == 0)
		  {
			if (prefix)
			{
			  // we took away the first prefix.
			  // COMPLEXPREFIXES = true:  combine with a second prefix and another suffix 
			  // COMPLEXPREFIXES = false: combine with a suffix
			  stems.AddRange(stem(strippedWord, length, affix, flag, flag, ++recursionDepth, dictionary.complexPrefixes && dictionary.twoStageAffix, true, true, circumfix));
			}
			else if (dictionary.complexPrefixes == false && dictionary.twoStageAffix)
			{
			  // we took away a suffix.
			  // COMPLEXPREFIXES = true: we don't recurse! only one suffix allowed
			  // COMPLEXPREFIXES = false: combine with another suffix
			  stems.AddRange(stem(strippedWord, length, affix, flag, prefixFlag, ++recursionDepth, false, true, false, circumfix));
			}
		  }
		  else if (recursionDepth == 1)
		  {
			if (prefix && dictionary.complexPrefixes)
			{
			  // we took away the second prefix: go look for another suffix
			  stems.AddRange(stem(strippedWord, length, affix, flag, flag, ++recursionDepth, false, true, true, circumfix));
			}
			else if (prefix == false && dictionary.complexPrefixes == false && dictionary.twoStageAffix)
			{
			  // we took away a prefix, then a suffix: go look for another suffix
			  stems.AddRange(stem(strippedWord, length, affix, flag, prefixFlag, ++recursionDepth, false, true, false, circumfix));
			}
		  }
		}

		return stems;
	  }

	  /// <summary>
	  /// Checks if the given flag cross checks with the given array of flags
	  /// </summary>
	  /// <param name="flag"> Flag to cross check with the array of flags </param>
	  /// <param name="flags"> Array of flags to cross check against.  Can be {@code null} </param>
	  /// <returns> {@code true} if the flag is found in the array or the array is {@code null}, {@code false} otherwise </returns>
	  private bool hasCrossCheckedFlag(char flag, char[] flags, bool matchEmpty)
	  {
		return (flags.Length == 0 && matchEmpty) || Arrays.binarySearch(flags, flag) >= 0;
	  }
	}

}