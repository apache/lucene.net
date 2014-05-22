using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs
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


	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using Fields = Lucene.Net.Index.Fields;
	using MergeState = Lucene.Net.Index.MergeState;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using DataInput = Lucene.Net.Store.DataInput;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;

	/// <summary>
	/// Codec API for writing term vectors:
	/// <p>
	/// <ol>
	///   <li>For every document, <seealso cref="#startDocument(int)"/> is called,
	///       informing the Codec how many fields will be written.
	///   <li><seealso cref="#startField(FieldInfo, int, boolean, boolean, boolean)"/> is called for 
	///       each field in the document, informing the codec how many terms
	///       will be written for that field, and whether or not positions,
	///       offsets, or payloads are enabled.
	///   <li>Within each field, <seealso cref="#startTerm(BytesRef, int)"/> is called
	///       for each term.
	///   <li>If offsets and/or positions are enabled, then 
	///       <seealso cref="#addPosition(int, int, int, BytesRef)"/> will be called for each term
	///       occurrence.
	///   <li>After all documents have been written, <seealso cref="#finish(FieldInfos, int)"/> 
	///       is called for verification/sanity-checks.
	///   <li>Finally the writer is closed (<seealso cref="#close()"/>)
	/// </ol>
	/// 
	/// @lucene.experimental
	/// </summary>
	public abstract class TermVectorsWriter : IDisposable
	{

	  /// <summary>
	  /// Sole constructor. (For invocation by subclass 
	  ///  constructors, typically implicit.) 
	  /// </summary>
	  protected internal TermVectorsWriter()
	  {
	  }

	  /// <summary>
	  /// Called before writing the term vectors of the document.
	  ///  <seealso cref="#startField(FieldInfo, int, boolean, boolean, boolean)"/> will 
	  ///  be called <code>numVectorFields</code> times. Note that if term 
	  ///  vectors are enabled, this is called even if the document 
	  ///  has no vector fields, in this case <code>numVectorFields</code> 
	  ///  will be zero. 
	  /// </summary>
	  public abstract void StartDocument(int numVectorFields);

	  /// <summary>
	  /// Called after a doc and all its fields have been added. </summary>
	  public virtual void FinishDocument()
	  {
	  }

	  /// <summary>
	  /// Called before writing the terms of the field.
	  ///  <seealso cref="#startTerm(BytesRef, int)"/> will be called <code>numTerms</code> times. 
	  /// </summary>
	  public abstract void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads);

	  /// <summary>
	  /// Called after a field and all its terms have been added. </summary>
	  public virtual void FinishField()
	  {
	  }

	  /// <summary>
	  /// Adds a term and its term frequency <code>freq</code>.
	  /// If this field has positions and/or offsets enabled, then
	  /// <seealso cref="#addPosition(int, int, int, BytesRef)"/> will be called 
	  /// <code>freq</code> times respectively.
	  /// </summary>
	  public abstract void StartTerm(BytesRef term, int freq);

	  /// <summary>
	  /// Called after a term and all its positions have been added. </summary>
	  public virtual void FinishTerm()
	  {
	  }

	  /// <summary>
	  /// Adds a term position and offsets </summary>
	  public abstract void AddPosition(int position, int startOffset, int endOffset, BytesRef payload);

	  /// <summary>
	  /// Aborts writing entirely, implementation should remove
	  ///  any partially-written files, etc. 
	  /// </summary>
	  public abstract void Abort();

	  /// <summary>
	  /// Called before <seealso cref="#close()"/>, passing in the number
	  ///  of documents that were written. Note that this is 
	  ///  intentionally redundant (equivalent to the number of
	  ///  calls to <seealso cref="#startDocument(int)"/>, but a Codec should
	  ///  check that this is the case to detect the JRE bug described 
	  ///  in LUCENE-1282. 
	  /// </summary>
	  public abstract void Finish(FieldInfos fis, int numDocs);

	  /// <summary>
	  /// Called by IndexWriter when writing new segments.
	  /// <p>
	  /// this is an expert API that allows the codec to consume 
	  /// positions and offsets directly from the indexer.
	  /// <p>
	  /// The default implementation calls <seealso cref="#addPosition(int, int, int, BytesRef)"/>,
	  /// but subclasses can override this if they want to efficiently write 
	  /// all the positions, then all the offsets, for example.
	  /// <p>
	  /// NOTE: this API is extremely expert and subject to change or removal!!!
	  /// @lucene.internal
	  /// </summary>
	  // TODO: we should probably nuke this and make a more efficient 4.x format
	  // PreFlex-RW could then be slow and buffer (its only used in tests...)
	  public virtual void AddProx(int numProx, DataInput positions, DataInput offsets)
	  {
		int position = 0;
		int lastOffset = 0;
		BytesRef payload = null;

		for (int i = 0; i < numProx; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startOffset;
		  int startOffset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset;
		  int endOffset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef thisPayload;
		  BytesRef thisPayload;

		  if (positions == null)
		  {
			position = -1;
			thisPayload = null;
		  }
		  else
		  {
			int code = positions.ReadVInt();
			position += (int)((uint)code >> 1);
			if ((code & 1) != 0)
			{
			  // this position has a payload
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int payloadLength = positions.readVInt();
			  int payloadLength = positions.ReadVInt();

			  if (payload == null)
			  {
				payload = new BytesRef();
				payload.Bytes = new sbyte[payloadLength];
			  }
			  else if (payload.Bytes.Length < payloadLength)
			  {
				payload.Grow(payloadLength);
			  }

			  positions.ReadBytes(payload.Bytes, 0, payloadLength);
			  payload.Length = payloadLength;
			  thisPayload = payload;
			}
			else
			{
			  thisPayload = null;
			}
		  }

		  if (offsets == null)
		  {
			startOffset = endOffset = -1;
		  }
		  else
		  {
			startOffset = lastOffset + offsets.ReadVInt();
			endOffset = startOffset + offsets.ReadVInt();
			lastOffset = endOffset;
		  }
		  AddPosition(position, startOffset, endOffset, thisPayload);
		}
	  }

	  /// <summary>
	  /// Merges in the term vectors from the readers in 
	  ///  <code>mergeState</code>. The default implementation skips
	  ///  over deleted documents, and uses <seealso cref="#startDocument(int)"/>,
	  ///  <seealso cref="#startField(FieldInfo, int, boolean, boolean, boolean)"/>, 
	  ///  <seealso cref="#startTerm(BytesRef, int)"/>, <seealso cref="#addPosition(int, int, int, BytesRef)"/>,
	  ///  and <seealso cref="#finish(FieldInfos, int)"/>,
	  ///  returning the number of documents that were written.
	  ///  Implementations can override this method for more sophisticated
	  ///  merging (bulk-byte copying, etc). 
	  /// </summary>
	  public virtual int Merge(MergeState mergeState)
	  {
		int docCount = 0;
		for (int i = 0; i < mergeState.Readers.Count; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.AtomicReader reader = mergeState.readers.get(i);
		  AtomicReader reader = mergeState.Readers[i];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = reader.maxDoc();
		  int maxDoc = reader.MaxDoc();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Bits liveDocs = reader.getLiveDocs();
		  Bits liveDocs = reader.LiveDocs;

		  for (int docID = 0; docID < maxDoc; docID++)
		  {
			if (liveDocs != null && !liveDocs.Get(docID))
			{
			  // skip deleted docs
			  continue;
			}
			// NOTE: it's very important to first assign to vectors then pass it to
			// termVectorsWriter.addAllDocVectors; see LUCENE-1282
			Fields vectors = reader.GetTermVectors(docID);
			AddAllDocVectors(vectors, mergeState);
			docCount++;
			mergeState.CheckAbort.work(300);
		  }
		}
		Finish(mergeState.FieldInfos, docCount);
		return docCount;
	  }

	  /// <summary>
	  /// Safe (but, slowish) default method to write every
	  ///  vector field in the document. 
	  /// </summary>
	  protected internal void AddAllDocVectors(Fields vectors, MergeState mergeState)
	  {
		if (vectors == null)
		{
		  StartDocument(0);
		  FinishDocument();
		  return;
		}

		int numFields = vectors.Size();
		if (numFields == -1)
		{
		  // count manually! TODO: Maybe enforce that Fields.size() returns something valid?
		  numFields = 0;
		  for (final IEnumerator<string> it = vectors.Iterator(); it.hasNext();)
		  {
			it.next();
			numFields++;
		  }
		}
		StartDocument(numFields);

		string lastFieldName = null;

		TermsEnum termsEnum = null;
		DocsAndPositionsEnum docsAndPositionsEnum = null;

		int fieldCount = 0;
		foreach (string fieldName in vectors)
		{
		  fieldCount++;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.FieldInfo fieldInfo = mergeState.fieldInfos.fieldInfo(fieldName);
		  FieldInfo fieldInfo = mergeState.FieldInfos.fieldInfo(fieldName);

		  Debug.Assert(lastFieldName == null || fieldName.CompareTo(lastFieldName) > 0, "lastFieldName=" + lastFieldName + " fieldName=" + fieldName);
		  lastFieldName = fieldName;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Terms terms = vectors.terms(fieldName);
		  Terms terms = vectors.Terms(fieldName);
		  if (terms == null)
		  {
			// FieldsEnum shouldn't lie...
			continue;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean hasPositions = terms.hasPositions();
		  bool hasPositions = terms.HasPositions();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean hasOffsets = terms.hasOffsets();
		  bool hasOffsets = terms.HasOffsets();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean hasPayloads = terms.hasPayloads();
		  bool hasPayloads = terms.HasPayloads();
		  Debug.Assert(!hasPayloads || hasPositions);

		  int numTerms = (int) terms.Size();
		  if (numTerms == -1)
		  {
			// count manually. It is stupid, but needed, as Terms.size() is not a mandatory statistics function
			numTerms = 0;
			termsEnum = terms.Iterator(termsEnum);
			while (termsEnum.Next() != null)
			{
			  numTerms++;
			}
		  }

		  StartField(fieldInfo, numTerms, hasPositions, hasOffsets, hasPayloads);
		  termsEnum = terms.Iterator(termsEnum);

		  int termCount = 0;
		  while (termsEnum.Next() != null)
		  {
			termCount++;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int freq = (int) termsEnum.totalTermFreq();
			int freq = (int) termsEnum.TotalTermFreq();

			StartTerm(termsEnum.Term(), freq);

			if (hasPositions || hasOffsets)
			{
			  docsAndPositionsEnum = termsEnum.DocsAndPositions(null, docsAndPositionsEnum);
			  Debug.Assert(docsAndPositionsEnum != null);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docID = docsAndPositionsEnum.nextDoc();
			  int docID = docsAndPositionsEnum.NextDoc();
			  Debug.Assert(docID != DocIdSetIterator.NO_MORE_DOCS);
			  Debug.Assert(docsAndPositionsEnum.Freq() == freq);

			  for (int posUpto = 0; posUpto < freq; posUpto++)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = docsAndPositionsEnum.nextPosition();
				int pos = docsAndPositionsEnum.NextPosition();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startOffset = docsAndPositionsEnum.StartOffset();
				int startOffset = docsAndPositionsEnum.StartOffset();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset = docsAndPositionsEnum.EndOffset();
				int endOffset = docsAndPositionsEnum.EndOffset();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef payload = docsAndPositionsEnum.getPayload();
				BytesRef payload = docsAndPositionsEnum.Payload;

				Debug.Assert(!hasPositions || pos >= 0);
				AddPosition(pos, startOffset, endOffset, payload);
			  }
			}
			FinishTerm();
		  }
		  Debug.Assert(termCount == numTerms);
		  FinishField();
		}
		Debug.Assert(fieldCount == numFields);
		FinishDocument();
	  }

	  /// <summary>
	  /// Return the BytesRef Comparator used to sort terms
	  ///  before feeding to this API. 
	  /// </summary>
	  public abstract IComparer<BytesRef> Comparator {get;}

	  public override abstract void Close();
	}

}