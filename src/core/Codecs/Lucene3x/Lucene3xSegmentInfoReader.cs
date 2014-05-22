using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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


	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using IndexFormatTooNewException = Lucene.Net.Index.IndexFormatTooNewException;
	using IndexFormatTooOldException = Lucene.Net.Index.IndexFormatTooOldException;
	using SegmentInfo = Lucene.Net.Index.SegmentInfo;
	using SegmentCommitInfo = Lucene.Net.Index.SegmentCommitInfo;
	using SegmentInfos = Lucene.Net.Index.SegmentInfos;
	using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 3x implementation of <seealso cref="SegmentInfoReader"/>.
	/// @lucene.experimental </summary>
	/// @deprecated Only for reading existing 3.x indexes 
	[Obsolete("Only for reading existing 3.x indexes")]
	public class Lucene3xSegmentInfoReader : SegmentInfoReader
	{

	  public static void ReadLegacyInfos(SegmentInfos infos, Directory directory, IndexInput input, int format)
	  {
		infos.Version_Renamed = input.ReadLong(); // read version
		infos.Counter = input.ReadInt(); // read counter
		Lucene3xSegmentInfoReader reader = new Lucene3xSegmentInfoReader();
		for (int i = input.ReadInt(); i > 0; i--) // read segmentInfos
		{
		  SegmentCommitInfo siPerCommit = reader.ReadLegacySegmentInfo(directory, format, input);
		  SegmentInfo si = siPerCommit.Info;

		  if (si.Version == null)
		  {
			// Could be a 3.0 - try to open the doc stores - if it fails, it's a
			// 2.x segment, and an IndexFormatTooOldException will be thrown,
			// which is what we want.
			Directory dir = directory;
			if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(si) != -1)
			{
			  if (Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
			  {
				dir = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(Lucene3xSegmentInfoFormat.GetDocStoreSegment(si), "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), IOContext.READONCE, false);
			  }
			}
			else if (si.UseCompoundFile)
			{
			  dir = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.Name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), IOContext.READONCE, false);
			}

			try
			{
			  Lucene3xStoredFieldsReader.CheckCodeVersion(dir, Lucene3xSegmentInfoFormat.GetDocStoreSegment(si));
			}
			finally
			{
			  // If we opened the directory, close it
			  if (dir != directory)
			  {
				  dir.Close();
			  }
			}

			// Above call succeeded, so it's a 3.0 segment. Upgrade it so the next
			// time the segment is read, its version won't be null and we won't
			// need to open FieldsReader every time for each such segment.
			si.Version = "3.0";
		  }
		  else if (si.Version.Equals("2.x"))
		  {
			// If it's a 3x index touched by 3.1+ code, then segments record their
			// version, whether they are 2.x ones or not. We detect that and throw
			// appropriate exception.
			throw new IndexFormatTooOldException("segment " + si.Name + " in resource " + input, si.Version);
		  }
		  infos.Add(siPerCommit);
		}

		infos.UserData_Renamed = input.ReadStringStringMap();
	  }

	  public override SegmentInfo Read(Directory directory, string segmentName, IOContext context)
	  {
		// NOTE: this is NOT how 3.x is really written...
		string fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);

		bool success = false;

		IndexInput input = directory.OpenInput(fileName, context);

		try
		{
		  SegmentInfo si = ReadUpgradedSegmentInfo(segmentName, directory, input);
		  success = true;
		  return si;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(input);
		  }
		  else
		  {
			input.Close();
		  }
		}
	  }

	  private static void AddIfExists(Directory dir, Set<string> files, string fileName)
	  {
		if (dir.FileExists(fileName))
		{
		  files.add(fileName);
		}
	  }

	  /// <summary>
	  /// reads from legacy 3.x segments_N </summary>
	  private SegmentCommitInfo ReadLegacySegmentInfo(Directory dir, int format, IndexInput input)
	  {
		// check that it is a format we can understand
		if (format > Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS)
		{
		  throw new IndexFormatTooOldException(input, format, Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS, Lucene3xSegmentInfoFormat.FORMAT_3_1);
		}
		if (format < Lucene3xSegmentInfoFormat.FORMAT_3_1)
		{
		  throw new IndexFormatTooNewException(input, format, Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS, Lucene3xSegmentInfoFormat.FORMAT_3_1);
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String version;
		string version;
		if (format <= Lucene3xSegmentInfoFormat.FORMAT_3_1)
		{
		  version = input.ReadString();
		}
		else
		{
		  version = null;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String name = input.readString();
		string name = input.ReadString();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docCount = input.readInt();
		int docCount = input.ReadInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long delGen = input.readLong();
		long delGen = input.ReadLong();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docStoreOffset = input.readInt();
		int docStoreOffset = input.ReadInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> attributes = new java.util.HashMap<>();
		IDictionary<string, string> attributes = new Dictionary<string, string>();

		// parse the docstore stuff and shove it into attributes
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String docStoreSegment;
		string docStoreSegment;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean docStoreIsCompoundFile;
		bool docStoreIsCompoundFile;
		if (docStoreOffset != -1)
		{
		  docStoreSegment = input.ReadString();
		  docStoreIsCompoundFile = input.ReadByte() == SegmentInfo.YES;
		  attributes[Lucene3xSegmentInfoFormat.DS_OFFSET_KEY] = Convert.ToString(docStoreOffset);
		  attributes[Lucene3xSegmentInfoFormat.DS_NAME_KEY] = docStoreSegment;
		  attributes[Lucene3xSegmentInfoFormat.DS_COMPOUND_KEY] = Convert.ToString(docStoreIsCompoundFile);
		}
		else
		{
		  docStoreSegment = name;
		  docStoreIsCompoundFile = false;
		}

		// pre-4.0 indexes write a byte if there is a single norms file
		sbyte b = input.ReadByte();

		//System.out.println("version=" + version + " name=" + name + " docCount=" + docCount + " delGen=" + delGen + " dso=" + docStoreOffset + " dss=" + docStoreSegment + " dssCFs=" + docStoreIsCompoundFile + " b=" + b + " format=" + format);

		Debug.Assert(1 == b, "expected 1 but was: " + b + " format: " + format);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numNormGen = input.readInt();
		int numNormGen = input.ReadInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<Integer,Long> normGen;
		IDictionary<int?, long?> normGen;
		if (numNormGen == SegmentInfo.NO)
		{
		  normGen = null;
		}
		else
		{
		  normGen = new Dictionary<>();
		  for (int j = 0;j < numNormGen;j++)
		  {
			normGen[j] = input.ReadLong();
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean isCompoundFile = input.readByte() == Lucene.Net.Index.SegmentInfo.YES;
		bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int delCount = input.readInt();
		int delCount = input.ReadInt();
		Debug.Assert(delCount <= docCount);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean hasProx = input.readByte() == 1;
		bool hasProx = input.ReadByte() == 1;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> diagnostics = input.readStringStringMap();
		IDictionary<string, string> diagnostics = input.ReadStringStringMap();

		if (format <= Lucene3xSegmentInfoFormat.FORMAT_HAS_VECTORS)
		{
		  // NOTE: unused
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int hasVectors = input.readByte();
		  int hasVectors = input.ReadByte();
		}

		// Replicate logic from 3.x's SegmentInfo.files():
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<String> files = new java.util.HashSet<>();
		Set<string> files = new HashSet<string>();
		if (isCompoundFile)
		{
		  files.add(IndexFileNames.SegmentFileName(name, "", IndexFileNames.COMPOUND_FILE_EXTENSION));
		}
		else
		{
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xFieldInfosReader.FIELD_INFOS_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.FREQ_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.PROX_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.TERMS_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xNormsProducer.NORMS_EXTENSION));
		}

		if (docStoreOffset != -1)
		{
		  if (docStoreIsCompoundFile)
		  {
			files.add(IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION));
		  }
		  else
		  {
			files.add(IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
			files.add(IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION));
			AddIfExists(dir, files, IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION));
			AddIfExists(dir, files, IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
			AddIfExists(dir, files, IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION));
		  }
		}
		else if (!isCompoundFile)
		{
		  files.add(IndexFileNames.SegmentFileName(name, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
		  files.add(IndexFileNames.SegmentFileName(name, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
		  AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION));
		}

		// parse the normgen stuff and shove it into attributes
		if (normGen != null)
		{
		  attributes[Lucene3xSegmentInfoFormat.NORMGEN_KEY] = Convert.ToString(numNormGen);
		  foreach (KeyValuePair<int?, long?> ent in normGen)
		  {
			long gen = ent.Value;
			if (gen >= SegmentInfo.YES)
			{
			  // Definitely a separate norm file, with generation:
			  files.add(IndexFileNames.FileNameFromGeneration(name, "s" + ent.Key, gen));
			  attributes[Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + ent.Key] = Convert.ToString(gen);
			}
			else if (gen == SegmentInfo.NO)
			{
			  // No separate norm
			}
			else
			{
			  // We should have already hit indexformat too old exception
			  Debug.Assert(false);
			}
		  }
		}

		SegmentInfo info = new SegmentInfo(dir, version, name, docCount, isCompoundFile, null, diagnostics, Collections.unmodifiableMap(attributes));
		info.Files = files;

		SegmentCommitInfo infoPerCommit = new SegmentCommitInfo(info, delCount, delGen, -1);
		return infoPerCommit;
	  }

	  private SegmentInfo ReadUpgradedSegmentInfo(string name, Directory dir, IndexInput input)
	  {
		CodecUtil.CheckHeader(input, Lucene3xSegmentInfoFormat.UPGRADED_SI_CODEC_NAME, Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_START, Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_CURRENT);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String version = input.readString();
		string version = input.ReadString();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docCount = input.readInt();
		int docCount = input.ReadInt();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> attributes = input.readStringStringMap();
		IDictionary<string, string> attributes = input.ReadStringStringMap();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean isCompoundFile = input.readByte() == Lucene.Net.Index.SegmentInfo.YES;
		bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> diagnostics = input.readStringStringMap();
		IDictionary<string, string> diagnostics = input.ReadStringStringMap();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<String> files = input.readStringSet();
		Set<string> files = input.ReadStringSet();

		SegmentInfo info = new SegmentInfo(dir, version, name, docCount, isCompoundFile, null, diagnostics, Collections.unmodifiableMap(attributes));
		info.Files = files;
		return info;
	  }
	}

}