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

	using Codec = Lucene.Net.Codecs.Codec;
	using DocValuesProducer = Lucene.Net.Codecs.DocValuesProducer;
	using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
	using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
	using StoredFieldsReader = Lucene.Net.Codecs.StoredFieldsReader;
	using TermVectorsReader = Lucene.Net.Codecs.TermVectorsReader;
	using CoreClosedListener = Lucene.Net.Index.SegmentReader.CoreClosedListener;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using Lucene.Net.Util;
	using IOUtils = Lucene.Net.Util.IOUtils;


	/// <summary>
	/// Holds core readers that are shared (unchanged) when
	/// SegmentReader is cloned or reopened 
	/// </summary>
	internal sealed class SegmentCoreReaders
	{

	  // Counts how many other readers share the core objects
	  // (freqStream, proxStream, tis, etc.) of this reader;
	  // when coreRef drops to 0, these core objects may be
	  // closed.  A given instance of SegmentReader may be
	  // closed, even though it shares core objects with other
	  // SegmentReaders:
	  private readonly AtomicInteger @ref = new AtomicInteger(1);

	  internal readonly FieldsProducer Fields;
	  internal readonly DocValuesProducer NormsProducer;

	  internal readonly int TermsIndexDivisor;

	  internal readonly StoredFieldsReader FieldsReaderOrig;
	  internal readonly TermVectorsReader TermVectorsReaderOrig;
	  internal readonly CompoundFileDirectory CfsReader;

	  // TODO: make a single thread local w/ a
	  // Thingy class holding fieldsReader, termVectorsReader,
	  // normsProducer

	  internal readonly IDisposableThreadLocal<StoredFieldsReader> fieldsReaderLocal = new IDisposableThreadLocalAnonymousInnerClassHelper();

	  private class IDisposableThreadLocalAnonymousInnerClassHelper : IDisposableThreadLocal<StoredFieldsReader>
	  {
		  public IDisposableThreadLocalAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override StoredFieldsReader InitialValue()
		  {
			return outerInstance.FieldsReaderOrig.clone();
		  }
	  }

	  internal readonly IDisposableThreadLocal<TermVectorsReader> termVectorsLocal = new IDisposableThreadLocalAnonymousInnerClassHelper2();

	  private class IDisposableThreadLocalAnonymousInnerClassHelper2 : IDisposableThreadLocal<TermVectorsReader>
	  {
		  public IDisposableThreadLocalAnonymousInnerClassHelper2()
		  {
		  }

		  protected internal override TermVectorsReader InitialValue()
		  {
			return (outerInstance.TermVectorsReaderOrig == null) ? null : outerInstance.TermVectorsReaderOrig.clone();
		  }
	  }

	  internal readonly IDisposableThreadLocal<IDictionary<string, object>> normsLocal = new IDisposableThreadLocalAnonymousInnerClassHelper3();

	  private class IDisposableThreadLocalAnonymousInnerClassHelper3 : IDisposableThreadLocal<IDictionary<string, object>>
	  {
		  public IDisposableThreadLocalAnonymousInnerClassHelper3()
		  {
		  }

		  protected internal override IDictionary<string, object> InitialValue()
		  {
			return new Dictionary<>();
		  }
	  }

	  private readonly Set<CoreClosedListener> CoreClosedListeners = Collections.synchronizedSet(new LinkedHashSet<CoreClosedListener>());

	  internal SegmentCoreReaders(SegmentReader owner, Directory dir, SegmentCommitInfo si, IOContext context, int termsIndexDivisor)
	  {

		if (termsIndexDivisor == 0)
		{
		  throw new System.ArgumentException("indexDivisor must be < 0 (don't load terms index) or greater than 0 (got 0)");
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.Codec codec = si.info.getCodec();
		Codec codec = si.Info.Codec;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Store.Directory cfsDir;
		Directory cfsDir; // confusing name: if (cfs) its the cfsdir, otherwise its the segment's directory.

		bool success = false;

		try
		{
		  if (si.Info.UseCompoundFile)
		  {
			cfsDir = CfsReader = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.Info.name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), context, false);
		  }
		  else
		  {
			CfsReader = null;
			cfsDir = dir;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FieldInfos fieldInfos = owner.fieldInfos;
		  FieldInfos fieldInfos = owner.FieldInfos_Renamed;

		  this.TermsIndexDivisor = termsIndexDivisor;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Codecs.PostingsFormat format = codec.postingsFormat();
		  PostingsFormat format = codec.PostingsFormat();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SegmentReadState segmentReadState = new SegmentReadState(cfsDir, si.info, fieldInfos, context, termsIndexDivisor);
		  SegmentReadState segmentReadState = new SegmentReadState(cfsDir, si.Info, fieldInfos, context, termsIndexDivisor);
		  // Ask codec for its Fields
		  Fields = format.FieldsProducer(segmentReadState);
		  Debug.Assert(Fields != null);
		  // ask codec for its Norms: 
		  // TODO: since we don't write any norms file if there are no norms,
		  // kinda jaky to assume the codec handles the case of no norms file at all gracefully?!

		  if (fieldInfos.HasNorms())
		  {
			NormsProducer = codec.NormsFormat().normsProducer(segmentReadState);
			Debug.Assert(NormsProducer != null);
		  }
		  else
		  {
			NormsProducer = null;
		  }

		  FieldsReaderOrig = si.Info.Codec.storedFieldsFormat().fieldsReader(cfsDir, si.Info, fieldInfos, context);

		  if (fieldInfos.HasVectors()) // open term vector files only as needed
		  {
			TermVectorsReaderOrig = si.Info.Codec.termVectorsFormat().vectorsReader(cfsDir, si.Info, fieldInfos, context);
		  }
		  else
		  {
			TermVectorsReaderOrig = null;
		  }

		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			DecRef();
		  }
		}
	  }

	  internal int RefCount
	  {
		  get
		  {
			return @ref.get();
		  }
	  }

	  internal void IncRef()
	  {
		int count;
		while ((count = @ref.get()) > 0)
		{
		  if (@ref.compareAndSet(count, count + 1))
		  {
			return;
		  }
		}
		throw new AlreadyClosedException("SegmentCoreReaders is already closed");
	  }

	  internal NumericDocValues GetNormValues(FieldInfo fi)
	  {
		Debug.Assert(NormsProducer != null);

		IDictionary<string, object> normFields = normsLocal.get();

		NumericDocValues norms = (NumericDocValues) normFields[fi.Name];
		if (norms == null)
		{
		  norms = NormsProducer.GetNumeric(fi);
		  normFields[fi.Name] = norms;
		}

		return norms;
	  }

	  internal void DecRef()
	  {
		if (@ref.decrementAndGet() == 0)
		{
	//      System.err.println("--- closing core readers");
		  Exception th = null;
		  try
		  {
			IOUtils.close(termVectorsLocal, fieldsReaderLocal, normsLocal, Fields, TermVectorsReaderOrig, FieldsReaderOrig, CfsReader, NormsProducer);
		  }
		  catch (Exception throwable)
		  {
			th = throwable;
		  }
		  finally
		  {
			NotifyCoreClosedListeners(th);
		  }
		}
	  }

	  private void NotifyCoreClosedListeners(Exception th)
	  {
		lock (CoreClosedListeners)
		{
		  foreach (CoreClosedListener listener in CoreClosedListeners)
		  {
			// SegmentReader uses our instance as its
			// coreCacheKey:
			try
			{
			  listener.OnClose(this);
			}
			catch (Exception t)
			{
			  if (th == null)
			  {
				th = t;
			  }
			  else
			  {
				th.addSuppressed(t);
			  }
			}
		  }
		  IOUtils.ReThrowUnchecked(th);
		}
	  }

	  internal void AddCoreClosedListener(CoreClosedListener listener)
	  {
		CoreClosedListeners.add(listener);
	  }

	  internal void RemoveCoreClosedListener(CoreClosedListener listener)
	  {
		CoreClosedListeners.remove(listener);
	  }

	  /// <summary>
	  /// Returns approximate RAM bytes used </summary>
	  public long RamBytesUsed()
	  {
		return ((NormsProducer != null) ? NormsProducer.RamBytesUsed() : 0) + ((Fields != null) ? Fields.RamBytesUsed() : 0) + ((FieldsReaderOrig != null)? FieldsReaderOrig.RamBytesUsed() : 0) + ((TermVectorsReaderOrig != null) ? TermVectorsReaderOrig.RamBytesUsed() : 0);
	  }
	}

}