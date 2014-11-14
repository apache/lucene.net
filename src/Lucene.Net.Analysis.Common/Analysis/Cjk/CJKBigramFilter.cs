﻿namespace org.apache.lucene.analysis.cjk
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

	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using PositionLengthAttribute = org.apache.lucene.analysis.tokenattributes.PositionLengthAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;

	/// <summary>
	/// Forms bigrams of CJK terms that are generated from StandardTokenizer
	/// or ICUTokenizer.
	/// <para>
	/// CJK types are set by these tokenizers, but you can also use 
	/// <seealso cref="#CJKBigramFilter(TokenStream, int)"/> to explicitly control which
	/// of the CJK scripts are turned into bigrams.
	/// </para>
	/// <para>
	/// By default, when a CJK character has no adjacent characters to form
	/// a bigram, it is output in unigram form. If you want to always output
	/// both unigrams and bigrams, set the <code>outputUnigrams</code>
	/// flag in <seealso cref="CJKBigramFilter#CJKBigramFilter(TokenStream, int, boolean)"/>.
	/// This can be used for a combined unigram+bigram approach.
	/// </para>
	/// <para>
	/// In all cases, all non-CJK input is passed thru unmodified.
	/// </para>
	/// </summary>
	public sealed class CJKBigramFilter : TokenFilter
	{
	  // configuration
	  /// <summary>
	  /// bigram flag for Han Ideographs </summary>
	  public const int HAN = 1;
	  /// <summary>
	  /// bigram flag for Hiragana </summary>
	  public const int HIRAGANA = 2;
	  /// <summary>
	  /// bigram flag for Katakana </summary>
	  public const int KATAKANA = 4;
	  /// <summary>
	  /// bigram flag for Hangul </summary>
	  public const int HANGUL = 8;

	  /// <summary>
	  /// when we emit a bigram, its then marked as this type </summary>
	  public const string DOUBLE_TYPE = "<DOUBLE>";
	  /// <summary>
	  /// when we emit a unigram, its then marked as this type </summary>
	  public const string SINGLE_TYPE = "<SINGLE>";

	  // the types from standardtokenizer
	  private static readonly string HAN_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC];
	  private static readonly string HIRAGANA_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HIRAGANA];
	  private static readonly string KATAKANA_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.KATAKANA];
	  private static readonly string HANGUL_TYPE = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HANGUL];

	  // sentinel value for ignoring a script 
	  private static readonly object NO = new object();

	  // these are set to either their type or NO if we want to pass them thru
	  private readonly object doHan;
	  private readonly object doHiragana;
	  private readonly object doKatakana;
	  private readonly object doHangul;

	  // true if we should output unigram tokens always
	  private readonly bool outputUnigrams;
	  private bool ngramState; // false = output unigram, true = output bigram

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly PositionLengthAttribute posLengthAtt = addAttribute(typeof(PositionLengthAttribute));

	  // buffers containing codepoint and offsets in parallel
	  internal int[] buffer = new int[8];
	  internal int[] startOffset = new int[8];
	  internal int[] endOffset = new int[8];
	  // length of valid buffer
	  internal int bufferLen;
	  // current buffer index
	  internal int index;

	  // the last end offset, to determine if we should bigram across tokens
	  internal int lastEndOffset;

	  private bool exhausted;

	  /// <summary>
	  /// Calls {@link CJKBigramFilter#CJKBigramFilter(TokenStream, int)
	  ///       CJKBigramFilter(in, HAN | HIRAGANA | KATAKANA | HANGUL)}
	  /// </summary>
	  public CJKBigramFilter(TokenStream @in) : this(@in, HAN | HIRAGANA | KATAKANA | HANGUL)
	  {
	  }

	  /// <summary>
	  /// Calls {@link CJKBigramFilter#CJKBigramFilter(TokenStream, int, boolean)
	  ///       CJKBigramFilter(in, flags, false)}
	  /// </summary>
	  public CJKBigramFilter(TokenStream @in, int flags) : this(@in, flags, false)
	  {
	  }

	  /// <summary>
	  /// Create a new CJKBigramFilter, specifying which writing systems should be bigrammed,
	  /// and whether or not unigrams should also be output. </summary>
	  /// <param name="flags"> OR'ed set from <seealso cref="CJKBigramFilter#HAN"/>, <seealso cref="CJKBigramFilter#HIRAGANA"/>, 
	  ///        <seealso cref="CJKBigramFilter#KATAKANA"/>, <seealso cref="CJKBigramFilter#HANGUL"/> </param>
	  /// <param name="outputUnigrams"> true if unigrams for the selected writing systems should also be output.
	  ///        when this is false, this is only done when there are no adjacent characters to form
	  ///        a bigram. </param>
	  public CJKBigramFilter(TokenStream @in, int flags, bool outputUnigrams) : base(@in)
	  {
		doHan = (flags & HAN) == 0 ? NO : HAN_TYPE;
		doHiragana = (flags & HIRAGANA) == 0 ? NO : HIRAGANA_TYPE;
		doKatakana = (flags & KATAKANA) == 0 ? NO : KATAKANA_TYPE;
		doHangul = (flags & HANGUL) == 0 ? NO : HANGUL_TYPE;
		this.outputUnigrams = outputUnigrams;
	  }

	  /*
	   * much of this complexity revolves around handling the special case of a 
	   * "lone cjk character" where cjktokenizer would output a unigram. this 
	   * is also the only time we ever have to captureState.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		while (true)
		{
		  if (hasBufferedBigram())
		  {

			// case 1: we have multiple remaining codepoints buffered,
			// so we can emit a bigram here.

			if (outputUnigrams)
			{

			  // when also outputting unigrams, we output the unigram first,
			  // then rewind back to revisit the bigram.
			  // so an input of ABC is A + (rewind)AB + B + (rewind)BC + C
			  // the logic in hasBufferedUnigram ensures we output the C, 
			  // even though it did actually have adjacent CJK characters.

			  if (ngramState)
			  {
				flushBigram();
			  }
			  else
			  {
				flushUnigram();
				index--;
			  }
			  ngramState = !ngramState;
			}
			else
			{
			  flushBigram();
			}
			return true;
		  }
		  else if (doNext())
		  {

			// case 2: look at the token type. should we form any n-grams?

			string type = typeAtt.type();
			if (type == doHan || type == doHiragana || type == doKatakana || type == doHangul)
			{

			  // acceptable CJK type: we form n-grams from these.
			  // as long as the offsets are aligned, we just add these to our current buffer.
			  // otherwise, we clear the buffer and start over.

			  if (offsetAtt.startOffset() != lastEndOffset) // unaligned, clear queue
			  {
				if (hasBufferedUnigram())
				{

				  // we have a buffered unigram, and we peeked ahead to see if we could form
				  // a bigram, but we can't, because the offsets are unaligned. capture the state 
				  // of this peeked data to be revisited next time thru the loop, and dump our unigram.

				  loneState = captureState();
				  flushUnigram();
				  return true;
				}
				index = 0;
				bufferLen = 0;
			  }
			  refill();
			}
			else
			{

			  // not a CJK type: we just return these as-is.

			  if (hasBufferedUnigram())
			  {

				// we have a buffered unigram, and we peeked ahead to see if we could form
				// a bigram, but we can't, because its not a CJK type. capture the state 
				// of this peeked data to be revisited next time thru the loop, and dump our unigram.

				loneState = captureState();
				flushUnigram();
				return true;
			  }
			  return true;
			}
		  }
		  else
		  {

			// case 3: we have only zero or 1 codepoints buffered, 
			// so not enough to form a bigram. But, we also have no
			// more input. So if we have a buffered codepoint, emit
			// a unigram, otherwise, its end of stream.

			if (hasBufferedUnigram())
			{
			  flushUnigram(); // flush our remaining unigram
			  return true;
			}
			return false;
		  }
		}
	  }

	  private State loneState; // rarely used: only for "lone cjk characters", where we emit unigrams

	  /// <summary>
	  /// looks at next input token, returning false is none is available 
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private boolean doNext() throws java.io.IOException
	  private bool doNext()
	  {
		if (loneState != null)
		{
		  restoreState(loneState);
		  loneState = null;
		  return true;
		}
		else
		{
		  if (exhausted)
		  {
			return false;
		  }
		  else if (input.incrementToken())
		  {
			return true;
		  }
		  else
		  {
			exhausted = true;
			return false;
		  }
		}
	  }

	  /// <summary>
	  /// refills buffers with new data from the current token.
	  /// </summary>
	  private void refill()
	  {
		// compact buffers to keep them smallish if they become large
		// just a safety check, but technically we only need the last codepoint
		if (bufferLen > 64)
		{
		  int last = bufferLen - 1;
		  buffer[0] = buffer[last];
		  startOffset[0] = startOffset[last];
		  endOffset[0] = endOffset[last];
		  bufferLen = 1;
		  index -= last;
		}

		char[] termBuffer = termAtt.buffer();
		int len = termAtt.length();
		int start = offsetAtt.startOffset();
		int end = offsetAtt.endOffset();

		int newSize = bufferLen + len;
		buffer = ArrayUtil.grow(buffer, newSize);
		startOffset = ArrayUtil.grow(startOffset, newSize);
		endOffset = ArrayUtil.grow(endOffset, newSize);
		lastEndOffset = end;

		if (end - start != len)
		{
		  // crazy offsets (modified by synonym or charfilter): just preserve
		  for (int i = 0, cp = 0; i < len; i += char.charCount(cp))
		  {
			cp = buffer[bufferLen] = char.codePointAt(termBuffer, i, len);
			startOffset[bufferLen] = start;
			endOffset[bufferLen] = end;
			bufferLen++;
		  }
		}
		else
		{
		  // normal offsets
		  for (int i = 0, cp = 0, cpLen = 0; i < len; i += cpLen)
		  {
			cp = buffer[bufferLen] = char.codePointAt(termBuffer, i, len);
			cpLen = char.charCount(cp);
			startOffset[bufferLen] = start;
			start = endOffset[bufferLen] = start + cpLen;
			bufferLen++;
		  }
		}
	  }

	  /// <summary>
	  /// Flushes a bigram token to output from our buffer 
	  /// This is the normal case, e.g. ABC -> AB BC
	  /// </summary>
	  private void flushBigram()
	  {
		clearAttributes();
		char[] termBuffer = termAtt.resizeBuffer(4); // maximum bigram length in code units (2 supplementaries)
		int len1 = char.toChars(buffer[index], termBuffer, 0);
		int len2 = len1 + char.toChars(buffer[index + 1], termBuffer, len1);
		termAtt.Length = len2;
		offsetAtt.setOffset(startOffset[index], endOffset[index + 1]);
		typeAtt.Type = DOUBLE_TYPE;
		// when outputting unigrams, all bigrams are synonyms that span two unigrams
		if (outputUnigrams)
		{
		  posIncAtt.PositionIncrement = 0;
		  posLengthAtt.PositionLength = 2;
		}
		index++;
	  }

	  /// <summary>
	  /// Flushes a unigram token to output from our buffer.
	  /// This happens when we encounter isolated CJK characters, either the whole
	  /// CJK string is a single character, or we encounter a CJK character surrounded 
	  /// by space, punctuation, english, etc, but not beside any other CJK.
	  /// </summary>
	  private void flushUnigram()
	  {
		clearAttributes();
		char[] termBuffer = termAtt.resizeBuffer(2); // maximum unigram length (2 surrogates)
		int len = char.toChars(buffer[index], termBuffer, 0);
		termAtt.Length = len;
		offsetAtt.setOffset(startOffset[index], endOffset[index]);
		typeAtt.Type = SINGLE_TYPE;
		index++;
	  }

	  /// <summary>
	  /// True if we have multiple codepoints sitting in our buffer
	  /// </summary>
	  private bool hasBufferedBigram()
	  {
		return bufferLen - index > 1;
	  }

	  /// <summary>
	  /// True if we have a single codepoint sitting in our buffer, where its future
	  /// (whether it is emitted as unigram or forms a bigram) depends upon not-yet-seen
	  /// inputs.
	  /// </summary>
	  private bool hasBufferedUnigram()
	  {
		if (outputUnigrams)
		{
		  // when outputting unigrams always
		  return bufferLen - index == 1;
		}
		else
		{
		  // otherwise its only when we have a lone CJK character
		  return bufferLen == 1 && index == 0;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		bufferLen = 0;
		index = 0;
		lastEndOffset = 0;
		loneState = null;
		exhausted = false;
		ngramState = false;
	  }
	}

}