using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search
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


	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using Term = Lucene.Net.Index.Term;
	using TermContext = Lucene.Net.Index.TermContext;
	using TermState = Lucene.Net.Index.TermState;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Lucene.Net.Util;
	using ToStringUtils = Lucene.Net.Util.ToStringUtils;

	/// <summary>
	/// MultiPhraseQuery is a generalized version of PhraseQuery, with an added
	/// method <seealso cref="#add(Term[])"/>.
	/// To use this class, to search for the phrase "Microsoft app*" first use
	/// add(Term) on the term "Microsoft", then find all terms that have "app" as
	/// prefix using IndexReader.terms(Term), and use MultiPhraseQuery.add(Term[]
	/// terms) to add them to the query.
	/// 
	/// </summary>
	public class MultiPhraseQuery : Query
	{
	  private string Field;
	  private List<Term[]> TermArrays_Renamed = new List<Term[]>();
	  private List<int?> Positions_Renamed = new List<int?>();

	  private int Slop_Renamed = 0;

	  /// <summary>
	  /// Sets the phrase slop for this query. </summary>
	  /// <seealso cref= PhraseQuery#setSlop(int) </seealso>
	  public virtual int Slop
	  {
		  set
		  {
			if (value < 0)
			{
			  throw new System.ArgumentException("slop value cannot be negative");
			}
			Slop_Renamed = value;
		  }
		  get
		  {
			  return Slop_Renamed;
		  }
	  }


	  /// <summary>
	  /// Add a single term at the next position in the phrase. </summary>
	  /// <seealso cref= PhraseQuery#add(Term) </seealso>
	  public virtual void Add(Term term)
	  {
		  Add(new Term[]{term});
	  }

	  /// <summary>
	  /// Add multiple terms at the next position in the phrase.  Any of the terms
	  /// may match.
	  /// </summary>
	  /// <seealso cref= PhraseQuery#add(Term) </seealso>
	  public virtual void Add(Term[] terms)
	  {
		int position = 0;
		if (Positions_Renamed.Count > 0)
		{
		  position = (int)Positions_Renamed[Positions_Renamed.Count - 1] + 1;
		}

		Add(terms, position);
	  }

	  /// <summary>
	  /// Allows to specify the relative position of terms within the phrase.
	  /// </summary>
	  /// <seealso cref= PhraseQuery#add(Term, int) </seealso>
	  public virtual void Add(Term[] terms, int position)
	  {
		if (TermArrays_Renamed.Count == 0)
		{
		  Field = terms[0].Field();
		}

		for (int i = 0; i < terms.Length; i++)
		{
		  if (!terms[i].Field().Equals(Field))
		  {
			throw new System.ArgumentException("All phrase terms must be in the same field (" + Field + "): " + terms[i]);
		  }
		}

		TermArrays_Renamed.Add(terms);
		Positions_Renamed.Add(Convert.ToInt32(position));
	  }

	  /// <summary>
	  /// Returns a List of the terms in the multiphrase.
	  /// Do not modify the List or its contents.
	  /// </summary>
	  public virtual IList<Term[]> TermArrays
	  {
		  get
		  {
			return Collections.unmodifiableList(TermArrays_Renamed);
		  }
	  }

	  /// <summary>
	  /// Returns the relative positions of terms in this phrase.
	  /// </summary>
	  public virtual int[] Positions
	  {
		  get
		  {
			int[] result = new int[Positions_Renamed.Count];
			for (int i = 0; i < Positions_Renamed.Count; i++)
			{
			  result[i] = (int)Positions_Renamed[i];
			}
			return result;
		  }
	  }

	  // inherit javadoc
	  public override void ExtractTerms(Set<Term> terms)
	  {
		foreach (Term[] arr in TermArrays_Renamed)
		{
		  foreach (Term term in arr)
		  {
			terms.add(term);
		  }
		}
	  }


	  private class MultiPhraseWeight : Weight
	  {
		  private readonly MultiPhraseQuery OuterInstance;

		internal readonly Similarity Similarity;
		internal readonly Similarity.SimWeight Stats;
		internal readonly IDictionary<Term, TermContext> TermContexts = new Dictionary<Term, TermContext>();

		public MultiPhraseWeight(MultiPhraseQuery outerInstance, IndexSearcher searcher)
		{
			this.OuterInstance = outerInstance;
		  this.Similarity = searcher.Similarity;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.IndexReaderContext context = searcher.getTopReaderContext();
		  IndexReaderContext context = searcher.TopReaderContext;

		  // compute idf
		  List<TermStatistics> allTermStats = new List<TermStatistics>();
		  foreach (Term[] terms in outerInstance.TermArrays_Renamed)
		  {
			foreach (Term term in terms)
			{
			  TermContext termContext = TermContexts[term];
			  if (termContext == null)
			  {
				termContext = TermContext.Build(context, term);
				TermContexts[term] = termContext;
			  }
			  allTermStats.Add(searcher.TermStatistics(term, termContext));
			}
		  }
		  Stats = Similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.Field), allTermStats.ToArray());
		}

		public override Query Query
		{
			get
			{
				return OuterInstance;
			}
		}
		public override float ValueForNormalization
		{
			get
			{
			  return Stats.ValueForNormalization;
			}
		}

		public override void Normalize(float queryNorm, float topLevelBoost)
		{
		  Stats.Normalize(queryNorm, topLevelBoost);
		}

		public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
		{
		  Debug.Assert(outerInstance.TermArrays_Renamed.Count > 0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.AtomicReader reader = context.reader();
		  AtomicReader reader = context.Reader();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Bits liveDocs = acceptDocs;
		  Bits liveDocs = acceptDocs;

		  PhraseQuery.PostingsAndFreq[] postingsFreqs = new PhraseQuery.PostingsAndFreq[outerInstance.TermArrays_Renamed.Count];

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Terms fieldTerms = reader.terms(field);
		  Terms fieldTerms = reader.Terms(outerInstance.Field);
		  if (fieldTerms == null)
		  {
			return null;
		  }

		  // Reuse single TermsEnum below:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermsEnum termsEnum = fieldTerms.iterator(null);
		  TermsEnum termsEnum = fieldTerms.Iterator(null);

		  for (int pos = 0; pos < postingsFreqs.Length; pos++)
		  {
			Term[] terms = outerInstance.TermArrays_Renamed[pos];

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.DocsAndPositionsEnum postingsEnum;
			DocsAndPositionsEnum postingsEnum;
			int docFreq;

			if (terms.Length > 1)
			{
			  postingsEnum = new UnionDocsAndPositionsEnum(liveDocs, context, terms, TermContexts, termsEnum);

			  // coarse -- this overcounts since a given doc can
			  // have more than one term:
			  docFreq = 0;
			  for (int termIdx = 0;termIdx < terms.Length;termIdx++)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Term term = terms[termIdx];
				Term term = terms[termIdx];
				TermState termState = TermContexts[term].Get(context.Ord);
				if (termState == null)
				{
				  // Term not in reader
				  continue;
				}
				termsEnum.SeekExact(term.Bytes(), termState);
				docFreq += termsEnum.DocFreq();
			  }

			  if (docFreq == 0)
			  {
				// None of the terms are in this reader
				return null;
			  }
			}
			else
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Term term = terms[0];
			  Term term = terms[0];
			  TermState termState = TermContexts[term].Get(context.Ord);
			  if (termState == null)
			  {
				// Term not in reader
				return null;
			  }
			  termsEnum.SeekExact(term.Bytes(), termState);
			  postingsEnum = termsEnum.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);

			  if (postingsEnum == null)
			  {
				// term does exist, but has no positions
				Debug.Assert(termsEnum.Docs(liveDocs, null, DocsEnum.FLAG_NONE) != null, "termstate found but no term exists in reader");
				throw new IllegalStateException("field \"" + term.Field() + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text() + ")");
			  }

			  docFreq = termsEnum.DocFreq();
			}

			postingsFreqs[pos] = new PhraseQuery.PostingsAndFreq(postingsEnum, docFreq, (int)outerInstance.Positions_Renamed[pos], terms);
		  }

		  // sort by increasing docFreq order
		  if (outerInstance.Slop_Renamed == 0)
		  {
			ArrayUtil.TimSort(postingsFreqs);
		  }

		  if (outerInstance.Slop_Renamed == 0)
		  {
			ExactPhraseScorer s = new ExactPhraseScorer(this, postingsFreqs, Similarity.SimScorer(Stats, context));
			if (s.NoDocs)
			{
			  return null;
			}
			else
			{
			  return s;
			}
		  }
		  else
		  {
			return new SloppyPhraseScorer(this, postingsFreqs, outerInstance.Slop_Renamed, Similarity.SimScorer(Stats, context));
		  }
		}

		public override Explanation Explain(AtomicReaderContext context, int doc)
		{
		  Scorer scorer = Scorer(context, context.Reader().LiveDocs);
		  if (scorer != null)
		  {
			int newDoc = scorer.Advance(doc);
			if (newDoc == doc)
			{
			  float freq = outerInstance.Slop_Renamed == 0 ? scorer.Freq() : ((SloppyPhraseScorer)scorer).SloppyFreq();
			  SimScorer docScorer = Similarity.SimScorer(Stats, context);
			  ComplexExplanation result = new ComplexExplanation();
			  result.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
			  Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
			  result.AddDetail(scoreExplanation);
			  result.Value = scoreExplanation.Value;
			  result.Match = true;
			  return result;
			}
		  }

		  return new ComplexExplanation(false, 0.0f, "no matching term");
		}
	  }

	  public override Query Rewrite(IndexReader reader)
	  {
		if (TermArrays_Renamed.Count == 0)
		{
		  BooleanQuery bq = new BooleanQuery();
		  bq.Boost = Boost;
		  return bq;
		} // optimize one-term case
		else if (TermArrays_Renamed.Count == 1)
		{
		  Term[] terms = TermArrays_Renamed[0];
		  BooleanQuery boq = new BooleanQuery(true);
		  for (int i = 0; i < terms.Length; i++)
		  {
			boq.Add(new TermQuery(terms[i]), BooleanClause.Occur.SHOULD);
		  }
		  boq.Boost = Boost;
		  return boq;
		}
		else
		{
		  return this;
		}
	  }

	  public override Weight CreateWeight(IndexSearcher searcher)
	  {
		return new MultiPhraseWeight(this, searcher);
	  }

	  /// <summary>
	  /// Prints a user-readable version of this query. </summary>
	  public override sealed string ToString(string f)
	  {
		StringBuilder buffer = new StringBuilder();
		if (Field == null || !Field.Equals(f))
		{
		  buffer.Append(Field);
		  buffer.Append(":");
		}

		buffer.Append("\"");
		int k = 0;
		IEnumerator<Term[]> i = TermArrays_Renamed.GetEnumerator();
		int lastPos = -1;
		bool first = true;
		while (i.MoveNext())
		{
		  Term[] terms = i.Current;
		  int position = Positions_Renamed[k];
		  if (first)
		  {
			first = false;
		  }
		  else
		  {
			buffer.Append(" ");
			for (int j = 1; j < (position - lastPos); j++)
			{
			  buffer.Append("? ");
			}
		  }
		  if (terms.Length > 1)
		  {
			buffer.Append("(");
			for (int j = 0; j < terms.Length; j++)
			{
			  buffer.Append(terms[j].Text());
			  if (j < terms.Length - 1)
			  {
				buffer.Append(" ");
			  }
			}
			buffer.Append(")");
		  }
		  else
		  {
			buffer.Append(terms[0].Text());
		  }
		  lastPos = position;
		  ++k;
		}
		buffer.Append("\"");

		if (Slop_Renamed != 0)
		{
		  buffer.Append("~");
		  buffer.Append(Slop_Renamed);
		}

		buffer.Append(ToStringUtils.Boost(Boost));

		return buffer.ToString();
	  }


	  /// <summary>
	  /// Returns true if <code>o</code> is equal to this. </summary>
	  public override bool Equals(object o)
	  {
		if (!(o is MultiPhraseQuery))
		{
			return false;
		}
		MultiPhraseQuery other = (MultiPhraseQuery)o;
		return this.Boost == other.Boost && this.Slop_Renamed == other.Slop_Renamed && TermArraysEquals(this.TermArrays_Renamed, other.TermArrays_Renamed) && this.Positions_Renamed.Equals(other.Positions_Renamed);
	  }

	  /// <summary>
	  /// Returns a hash code value for this object. </summary>
	  public override int HashCode()
	  {
		return float.floatToIntBits(Boost) ^ Slop_Renamed ^ TermArraysHashCode() ^ Positions_Renamed.HashCode() ^ 0x4AC65113;
	  }

	  // Breakout calculation of the termArrays hashcode
	  private int TermArraysHashCode()
	  {
		int hashCode = 1;
		foreach (Term[] termArray in TermArrays_Renamed)
		{
		  hashCode = 31 * hashCode + (termArray == null ? 0 : Arrays.GetHashCode(termArray));
		}
		return hashCode;
	  }

	  // Breakout calculation of the termArrays equals
	  private bool TermArraysEquals(IList<Term[]> termArrays1, IList<Term[]> termArrays2)
	  {
		if (termArrays1.Count != termArrays2.Count)
		{
		  return false;
		}
//JAVA TO C# CONVERTER WARNING: Unlike Java's ListIterator, enumerators in .NET do not allow altering the collection:
		IEnumerator<Term[]> iterator1 = termArrays1.GetEnumerator();
//JAVA TO C# CONVERTER WARNING: Unlike Java's ListIterator, enumerators in .NET do not allow altering the collection:
		IEnumerator<Term[]> iterator2 = termArrays2.GetEnumerator();
		while (iterator1.MoveNext())
		{
		  Term[] termArray1 = iterator1.Current;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Term[] termArray2 = iterator2.next();
		  if (!(termArray1 == null ? termArray2 == null : Array.Equals(termArray1, termArray2)))
		  {
			return false;
		  }
		}
		return true;
	  }
	}

	/// <summary>
	/// Takes the logical union of multiple DocsEnum iterators.
	/// </summary>

	// TODO: if ever we allow subclassing of the *PhraseScorer
	internal class UnionDocsAndPositionsEnum : DocsAndPositionsEnum
	{

	  private sealed class DocsQueue : PriorityQueue<DocsAndPositionsEnum>
	  {
		internal DocsQueue(IList<DocsAndPositionsEnum> docsEnums) : base(docsEnums.Count)
		{

		  IEnumerator<DocsAndPositionsEnum> i = docsEnums.GetEnumerator();
		  while (i.MoveNext())
		  {
			DocsAndPositionsEnum postings = i.Current;
			if (postings.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
			  Add(postings);
			}
		  }
		}

		public override bool LessThan(DocsAndPositionsEnum a, DocsAndPositionsEnum b)
		{
		  return a.DocID() < b.DocID();
		}
	  }

	  private sealed class IntQueue
	  {
		  internal bool InstanceFieldsInitialized = false;

		  public IntQueue()
		  {
			  if (!InstanceFieldsInitialized)
			  {
				  InitializeInstanceFields();
				  InstanceFieldsInitialized = true;
			  }
		  }

		  internal void InitializeInstanceFields()
		  {
			  _array = new int[_arraySize];
		  }

		internal int _arraySize = 16;
		internal int _index = 0;
		internal int _lastIndex = 0;
		internal int[] _array;

		internal void Add(int i)
		{
		  if (_lastIndex == _arraySize)
		  {
			GrowArray();
		  }

		  _array[_lastIndex++] = i;
		}

		internal int Next()
		{
		  return _array[_index++];
		}

		internal void Sort()
		{
		  Array.Sort(_array, _index, _lastIndex);
		}

		internal void Clear()
		{
		  _index = 0;
		  _lastIndex = 0;
		}

		internal int Size()
		{
		  return (_lastIndex - _index);
		}

		internal void GrowArray()
		{
		  int[] newArray = new int[_arraySize * 2];
		  Array.Copy(_array, 0, newArray, 0, _arraySize);
		  _array = newArray;
		  _arraySize *= 2;
		}
	  }

	  private int _doc;
	  private int _freq;
	  private DocsQueue _queue;
	  private IntQueue _posList;
	  private long Cost_Renamed;

	  public UnionDocsAndPositionsEnum(Bits liveDocs, AtomicReaderContext context, Term[] terms, IDictionary<Term, TermContext> termContexts, TermsEnum termsEnum)
	  {
		IList<DocsAndPositionsEnum> docsEnums = new LinkedList<DocsAndPositionsEnum>();
		for (int i = 0; i < terms.Length; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Term term = terms[i];
		  Term term = terms[i];
		  TermState termState = termContexts[term].Get(context.Ord);
		  if (termState == null)
		  {
			// Term doesn't exist in reader
			continue;
		  }
		  termsEnum.SeekExact(term.Bytes(), termState);
		  DocsAndPositionsEnum postings = termsEnum.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);
		  if (postings == null)
		  {
			// term does exist, but has no positions
			throw new IllegalStateException("field \"" + term.Field() + "\" was indexed without position data; cannot run PhraseQuery (term=" + term.Text() + ")");
		  }
		  Cost_Renamed += postings.Cost();
		  docsEnums.Add(postings);
		}

		_queue = new DocsQueue(docsEnums);
		_posList = new IntQueue();
	  }

	  public override sealed int NextDoc()
	  {
		if (_queue.Size() == 0)
		{
		  return NO_MORE_DOCS;
		}

		// TODO: move this init into positions(): if the search
		// doesn't need the positions for this doc then don't
		// waste CPU merging them:
		_posList.Clear();
		_doc = _queue.Top().docID();

		// merge sort all positions together
		DocsAndPositionsEnum postings;
		do
		{
		  postings = _queue.Top();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int freq = postings.freq();
		  int freq = postings.Freq();
		  for (int i = 0; i < freq; i++)
		  {
			_posList.Add(postings.NextPosition());
		  }

		  if (postings.NextDoc() != NO_MORE_DOCS)
		  {
			_queue.UpdateTop();
		  }
		  else
		  {
			_queue.Pop();
		  }
		} while (_queue.Size() > 0 && _queue.Top().docID() == _doc);

		_posList.Sort();
		_freq = _posList.Size();

		return _doc;
	  }

	  public override int NextPosition()
	  {
		return _posList.Next();
	  }

	  public override int StartOffset()
	  {
		return -1;
	  }

	  public override int EndOffset()
	  {
		return -1;
	  }

	  public override BytesRef Payload
	  {
		  get
		  {
			return null;
		  }
	  }

	  public override sealed int Advance(int target)
	  {
		while (_queue.Top() != null && target > _queue.Top().docID())
		{
		  DocsAndPositionsEnum postings = _queue.Pop();
		  if (postings.Advance(target) != NO_MORE_DOCS)
		  {
			_queue.Add(postings);
		  }
		}
		return NextDoc();
	  }

	  public override sealed int Freq()
	  {
		return _freq;
	  }

	  public override sealed int DocID()
	  {
		return _doc;
	  }

	  public override long Cost()
	  {
		return Cost_Renamed;
	  }
	}

}