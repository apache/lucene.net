using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Index
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


	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

	/// <summary>
	/// Collection of <seealso cref="FieldInfo"/>s (accessible by number or by name).
	///  @lucene.experimental
	/// </summary>
	public class FieldInfos : IEnumerable<FieldInfo>
	{
	  private readonly bool HasFreq_Renamed;
	  private readonly bool HasProx_Renamed;
	  private readonly bool HasPayloads_Renamed;
	  private readonly bool HasOffsets_Renamed;
	  private readonly bool HasVectors_Renamed;
	  private readonly bool HasNorms_Renamed;
	  private readonly bool HasDocValues_Renamed;

	  private readonly SortedMap<int?, FieldInfo> ByNumber = new SortedDictionary<int?, FieldInfo>();
	  private readonly Dictionary<string, FieldInfo> ByName = new Dictionary<string, FieldInfo>();
	  private readonly ICollection<FieldInfo> Values; // for an unmodifiable iterator

	  /// <summary>
	  /// Constructs a new FieldInfos from an array of FieldInfo objects
	  /// </summary>
	  public FieldInfos(FieldInfo[] infos)
	  {
		bool hasVectors = false;
		bool hasProx = false;
		bool hasPayloads = false;
		bool hasOffsets = false;
		bool hasFreq = false;
		bool hasNorms = false;
		bool hasDocValues = false;

		foreach (FieldInfo info in infos)
		{
		  if (info.Number < 0)
		  {
			throw new System.ArgumentException("illegal field number: " + info.Number + " for field " + info.Name);
		  }
		  FieldInfo previous = ByNumber.put(info.Number, info);
		  if (previous != null)
		  {
			throw new System.ArgumentException("duplicate field numbers: " + previous.Name + " and " + info.Name + " have: " + info.Number);
		  }
		  previous = ByName[info.Name] = info;
		  if (previous != null)
		  {
			throw new System.ArgumentException("duplicate field names: " + previous.Number + " and " + info.Number + " have: " + info.Name);
		  }

		  hasVectors |= info.HasVectors();
		  hasProx |= info.Indexed && info.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		  hasFreq |= info.Indexed && info.IndexOptions != IndexOptions.DOCS_ONLY;
		  hasOffsets |= info.Indexed && info.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		  hasNorms |= info.HasNorms();
		  hasDocValues |= info.HasDocValues();
		  hasPayloads |= info.HasPayloads();
		}

		this.HasVectors_Renamed = hasVectors;
		this.HasProx_Renamed = hasProx;
		this.HasPayloads_Renamed = hasPayloads;
		this.HasOffsets_Renamed = hasOffsets;
		this.HasFreq_Renamed = hasFreq;
		this.HasNorms_Renamed = hasNorms;
		this.HasDocValues_Renamed = hasDocValues;
		this.Values = Collections.unmodifiableCollection(ByNumber.values());
	  }

	  /// <summary>
	  /// Returns true if any fields have freqs </summary>
	  public virtual bool HasFreq()
	  {
		return HasFreq_Renamed;
	  }

	  /// <summary>
	  /// Returns true if any fields have positions </summary>
	  public virtual bool HasProx()
	  {
		return HasProx_Renamed;
	  }

	  /// <summary>
	  /// Returns true if any fields have payloads </summary>
	  public virtual bool HasPayloads()
	  {
		return HasPayloads_Renamed;
	  }

	  /// <summary>
	  /// Returns true if any fields have offsets </summary>
	  public virtual bool HasOffsets()
	  {
		return HasOffsets_Renamed;
	  }

	  /// <summary>
	  /// Returns true if any fields have vectors </summary>
	  public virtual bool HasVectors()
	  {
		return HasVectors_Renamed;
	  }

	  /// <summary>
	  /// Returns true if any fields have norms </summary>
	  public virtual bool HasNorms()
	  {
		return HasNorms_Renamed;
	  }

	  /// <summary>
	  /// Returns true if any fields have DocValues </summary>
	  public virtual bool HasDocValues()
	  {
		return HasDocValues_Renamed;
	  }

	  /// <summary>
	  /// Returns the number of fields </summary>
	  public virtual int Size()
	  {
		Debug.Assert(ByNumber.size() == ByName.Count);
		return ByNumber.size();
	  }

	  /// <summary>
	  /// Returns an iterator over all the fieldinfo objects present,
	  /// ordered by ascending field number
	  /// </summary>
	  // TODO: what happens if in fact a different order is used?
	  public virtual IEnumerator<FieldInfo> GetEnumerator()
	  {
		return Values.GetEnumerator();
	  }

	  /// <summary>
	  /// Return the fieldinfo object referenced by the field name </summary>
	  /// <returns> the FieldInfo object or null when the given fieldName
	  /// doesn't exist. </returns>
	  public virtual FieldInfo FieldInfo(string fieldName)
	  {
		return ByName[fieldName];
	  }

	  /// <summary>
	  /// Return the fieldinfo object referenced by the fieldNumber. </summary>
	  /// <param name="fieldNumber"> field's number. </param>
	  /// <returns> the FieldInfo object or null when the given fieldNumber
	  /// doesn't exist. </returns>
	  /// <exception cref="IllegalArgumentException"> if fieldNumber is negative </exception>
	  public virtual FieldInfo FieldInfo(int fieldNumber)
	  {
		if (fieldNumber < 0)
		{
		  throw new System.ArgumentException("Illegal field number: " + fieldNumber);
		}
		return ByNumber.get(fieldNumber);
	  }

	  internal sealed class FieldNumbers
	  {

		internal readonly IDictionary<int?, string> NumberToName;
		internal readonly IDictionary<string, int?> NameToNumber;
		// We use this to enforce that a given field never
		// changes DV type, even across segments / IndexWriter
		// sessions:
		internal readonly IDictionary<string, DocValuesType> DocValuesType;

		// TODO: we should similarly catch an attempt to turn
		// norms back on after they were already ommitted; today
		// we silently discard the norm but this is badly trappy
		internal int LowestUnassignedFieldNumber = -1;

		internal FieldNumbers()
		{
		  this.NameToNumber = new Dictionary<>();
		  this.NumberToName = new Dictionary<>();
		  this.DocValuesType = new Dictionary<>();
		}

		/// <summary>
		/// Returns the global field number for the given field name. If the name
		/// does not exist yet it tries to add it with the given preferred field
		/// number assigned if possible otherwise the first unassigned field number
		/// is used as the field number.
		/// </summary>
		internal int AddOrGet(string fieldName, int preferredFieldNumber, DocValuesType dvType)
		{
			lock (this)
			{
			  if (dvType != null)
			  {
				DocValuesType currentDVType = DocValuesType[fieldName];
				if (currentDVType == null)
				{
				  DocValuesType[fieldName] = dvType;
				}
				else if (currentDVType != null && currentDVType != dvType)
				{
				  throw new System.ArgumentException("cannot change DocValues type from " + currentDVType + " to " + dvType + " for field \"" + fieldName + "\"");
				}
			  }
			  int? fieldNumber = NameToNumber[fieldName];
			  if (fieldNumber == null)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer preferredBoxed = Integer.valueOf(preferredFieldNumber);
				int? preferredBoxed = Convert.ToInt32(preferredFieldNumber);
        
				if (preferredFieldNumber != -1 && !NumberToName.ContainsKey(preferredBoxed))
				{
				  // cool - we can use this number globally
				  fieldNumber = preferredBoxed;
				}
				else
				{
				  // find a new FieldNumber
				  while (NumberToName.ContainsKey(++LowestUnassignedFieldNumber))
				  {
					// might not be up to date - lets do the work once needed
				  }
				  fieldNumber = LowestUnassignedFieldNumber;
				}
        
				NumberToName[fieldNumber] = fieldName;
				NameToNumber[fieldName] = fieldNumber;
			  }
        
			  return (int)fieldNumber;
			}
		}

		// used by assert
		internal bool ContainsConsistent(int? number, string name, DocValuesType dvType)
		{
			lock (this)
			{
			  return name.Equals(NumberToName[number]) && number.Equals(NameToNumber[name]) && (dvType == null || DocValuesType[name] == null || dvType == DocValuesType[name]);
			}
		}

		/// <summary>
		/// Returns true if the {@code fieldName} exists in the map and is of the
		/// same {@code dvType}.
		/// </summary>
		internal bool Contains(string fieldName, DocValuesType dvType)
		{
			lock (this)
			{
			  // used by IndexWriter.updateNumericDocValue
			  if (!NameToNumber.ContainsKey(fieldName))
			  {
				return false;
			  }
			  else
			  {
				// only return true if the field has the same dvType as the requested one
				return dvType == DocValuesType[fieldName];
			  }
			}
		}

		internal void Clear()
		{
			lock (this)
			{
			  NumberToName.Clear();
			  NameToNumber.Clear();
			  DocValuesType.Clear();
			}
		}

		internal void SetDocValuesType(int number, string name, DocValuesType dvType)
		{
			lock (this)
			{
			  Debug.Assert(ContainsConsistent(number, name, dvType));
			  DocValuesType[name] = dvType;
			}
		}
	  }

	  internal sealed class Builder
	  {
		internal readonly Dictionary<string, FieldInfo> ByName = new Dictionary<string, FieldInfo>();
		internal readonly FieldNumbers GlobalFieldNumbers;

		internal Builder() : this(new FieldNumbers())
		{
		}

		/// <summary>
		/// Creates a new instance with the given <seealso cref="FieldNumbers"/>. 
		/// </summary>
		internal Builder(FieldNumbers globalFieldNumbers)
		{
		  Debug.Assert(globalFieldNumbers != null);
		  this.GlobalFieldNumbers = globalFieldNumbers;
		}

		public void Add(FieldInfos other)
		{
		  foreach (FieldInfo fieldInfo in other)
		  {
			Add(fieldInfo);
		  }
		}

		/// <summary>
		/// NOTE: this method does not carry over termVector
		///  booleans nor docValuesType; the indexer chain
		///  (TermVectorsConsumerPerField, DocFieldProcessor) must
		///  set these fields when they succeed in consuming
		///  the document 
		/// </summary>
		public FieldInfo AddOrUpdate(string name, IndexableFieldType fieldType)
		{
		  // TODO: really, indexer shouldn't even call this
		  // method (it's only called from DocFieldProcessor);
		  // rather, each component in the chain should update
		  // what it "owns".  EG fieldType.indexOptions() should
		  // be updated by maybe FreqProxTermsWriterPerField:
		  return AddOrUpdateInternal(name, -1, fieldType.Indexed(), false, fieldType.OmitNorms(), false, fieldType.IndexOptions(), fieldType.DocValueType(), null);
		}

		internal FieldInfo AddOrUpdateInternal(string name, int preferredFieldNumber, bool isIndexed, bool storeTermVector, bool omitNorms, bool storePayloads, IndexOptions indexOptions, DocValuesType docValues, DocValuesType normType)
		{
		  FieldInfo fi = FieldInfo(name);
		  if (fi == null)
		  {
			// this field wasn't yet added to this in-RAM
			// segment's FieldInfo, so now we get a global
			// number for this field.  If the field was seen
			// before then we'll get the same name and number,
			// else we'll allocate a new one:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int fieldNumber = globalFieldNumbers.addOrGet(name, preferredFieldNumber, docValues);
			int fieldNumber = GlobalFieldNumbers.AddOrGet(name, preferredFieldNumber, docValues);
			fi = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, docValues, normType, null);
			Debug.Assert(!ByName.ContainsKey(fi.Name));
			Debug.Assert(GlobalFieldNumbers.ContainsConsistent(Convert.ToInt32(fi.Number), fi.Name, fi.DocValuesType));
			ByName[fi.Name] = fi;
		  }
		  else
		  {
			fi.Update(isIndexed, storeTermVector, omitNorms, storePayloads, indexOptions);

			if (docValues != null)
			{
			  // only pay the synchronization cost if fi does not already have a DVType
			  bool updateGlobal = !fi.HasDocValues();
			  fi.DocValuesType = docValues; // this will also perform the consistency check.
			  if (updateGlobal)
			  {
				// must also update docValuesType map so it's
				// aware of this field's DocValueType 
				GlobalFieldNumbers.SetDocValuesType(fi.Number, name, docValues);
			  }
			}

			if (!fi.OmitsNorms() && normType != null)
			{
			  fi.NormValueType = normType;
			}
		  }
		  return fi;
		}

		public FieldInfo Add(FieldInfo fi)
		{
		  // IMPORTANT - reuse the field number if possible for consistent field numbers across segments
		  return AddOrUpdateInternal(fi.Name, fi.Number, fi.Indexed, fi.HasVectors(), fi.OmitsNorms(), fi.HasPayloads(), fi.IndexOptions, fi.DocValuesType, fi.NormType);
		}

		public FieldInfo FieldInfo(string fieldName)
		{
		  return ByName[fieldName];
		}

		internal FieldInfos Finish()
		{
		  return new FieldInfos(ByName.Values.toArray(new FieldInfo[ByName.Count]));
		}
	  }
	}

}