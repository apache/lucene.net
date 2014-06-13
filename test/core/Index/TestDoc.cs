using System;
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using Document = Lucene.Net.Document.Document;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;
	using Constants = Lucene.Net.Util.Constants;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
    using NUnit.Framework;
    using System.IO;
    using Lucene.Net.Util;


	/// <summary>
	/// JUnit adaptation of an older test case DocTest. </summary>
	public class TestDoc : LuceneTestCase
	{

		private File WorkDir;
		private File IndexDir;
		private LinkedList<File> Files;

		/// <summary>
		/// Set the test case. this test case needs
		///  a few text files created in the current working directory.
		/// </summary>
		public override void SetUp()
		{
			base.SetUp();
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: setUp");
			}
			WorkDir = CreateTempDir("TestDoc");
			WorkDir.mkdirs();

			IndexDir = CreateTempDir("testIndex");
			IndexDir.mkdirs();

			Directory directory = NewFSDirectory(IndexDir);
			directory.Dispose();

			Files = new LinkedList<>();
			Files.AddLast(CreateOutput("test.txt", "this is the first test file"));

			Files.AddLast(CreateOutput("test2.txt", "this is the second test file"));
		}

		private FileInfo CreateOutput(string name, string text)
		{
			TextWriter fw = null;
            StreamWriter pw = null;

			try
			{
				FileInfo f = new FileInfo(WorkDir, name);
				if (f.exists())
				{
					f.delete();
				}

                fw = new OutputStreamWriter(new FileOutputStream(f), IOUtils.CHARSET_UTF_8);
                pw = new StreamWriter(fw);
				pw.WriteLine(text);
				return f;

			}
			finally
			{
				if (pw != null)
				{
					pw.Dispose();
				}
				if (fw != null)
				{
					fw.Dispose();
				}
			}
		}


		/// <summary>
		/// this test executes a number of merges and compares the contents of
		///  the segments created when using compound file or not using one.
		/// 
		///  TODO: the original test used to print the segment contents to System.out
		///        for visual validation. To have the same effect, a new method
		///        checkSegment(String name, ...) should be created that would
		///        assert various things about the segment.
		/// </summary>
		public virtual void TestIndexAndMerge()
		{
		  StringWriter sw = new StringWriter();
          StreamWriter @out = new StreamWriter(sw, true);

		  Directory directory = NewFSDirectory(IndexDir, null);

		  if (directory is MockDirectoryWrapper)
		  {
			// We create unreferenced files (we don't even write
			// a segments file):
			((MockDirectoryWrapper) directory).AssertNoUnrefencedFilesOnClose = false;
		  }

		  IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(10)));

		  SegmentCommitInfo si1 = IndexDoc(writer, "test.txt");
		  PrintSegment(@out, si1);

		  SegmentCommitInfo si2 = IndexDoc(writer, "test2.txt");
		  PrintSegment(@out, si2);
		  writer.Dispose();

		  SegmentCommitInfo siMerge = Merge(directory, si1, si2, "_merge", false);
		  PrintSegment(@out, siMerge);

		  SegmentCommitInfo siMerge2 = Merge(directory, si1, si2, "_merge2", false);
		  PrintSegment(@out, siMerge2);

		  SegmentCommitInfo siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", false);
		  PrintSegment(@out, siMerge3);

		  directory.Dispose();
		  @out.Dispose();
		  sw.Dispose();

		  string multiFileOutput = sw.ToString();
		  //System.out.println(multiFileOutput);

		  sw = new StringWriter();
          @out = new StreamWriter(sw, true);

		  directory = NewFSDirectory(IndexDir, null);

		  if (directory is MockDirectoryWrapper)
		  {
			// We create unreferenced files (we don't even write
			// a segments file):
			((MockDirectoryWrapper) directory).AssertNoUnrefencedFilesOnClose = false;
		  }

		  writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode_e.CREATE).SetMaxBufferedDocs(-1).SetMergePolicy(NewLogMergePolicy(10)));

		  si1 = IndexDoc(writer, "test.txt");
		  PrintSegment(@out, si1);

		  si2 = IndexDoc(writer, "test2.txt");
		  PrintSegment(@out, si2);
		  writer.Dispose();

		  siMerge = Merge(directory, si1, si2, "_merge", true);
		  PrintSegment(@out, siMerge);

		  siMerge2 = Merge(directory, si1, si2, "_merge2", true);
		  PrintSegment(@out, siMerge2);

		  siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", true);
		  PrintSegment(@out, siMerge3);

		  directory.Dispose();
		  @out.Dispose();
		  sw.Dispose();
		  string singleFileOutput = sw.ToString();

		  Assert.AreEqual(multiFileOutput, singleFileOutput);
		}

	   private SegmentCommitInfo IndexDoc(IndexWriter writer, string fileName)
	   {
		  File file = new File(WorkDir, fileName);
		  Document doc = new Document();
		  InputStreamReader @is = new InputStreamReader(new FileInputStream(file), IOUtils.CHARSET_UTF_8);
		  doc.Add(new TextField("contents", @is));
		  writer.AddDocument(doc);
		  writer.Commit();
		  @is.Dispose();
		  return writer.NewestSegment();
	   }


	   private SegmentCommitInfo Merge(Directory dir, SegmentCommitInfo si1, SegmentCommitInfo si2, string merged, bool useCompoundFile)
	   {
		  IOContext context = NewIOContext(Random());
		  SegmentReader r1 = new SegmentReader(si1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, context);
		  SegmentReader r2 = new SegmentReader(si2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, context);

		  Codec codec = Codec.Default;
		  TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(si1.info.dir);
		  SegmentInfo si = new SegmentInfo(si1.info.dir, Constants.LUCENE_MAIN_VERSION, merged, -1, false, codec, null);

		  SegmentMerger merger = new SegmentMerger(Arrays.asList<AtomicReader>(r1, r2), si, InfoStream.Default, trackingDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), context, true);

		  MergeState mergeState = merger.merge();
		  r1.Dispose();
		  r2.Dispose();
		  SegmentInfo info = new SegmentInfo(si1.info.dir, Constants.LUCENE_MAIN_VERSION, merged, si1.info.DocCount + si2.info.DocCount, false, codec, null);
		  info.Files = new HashSet<>(trackingDir.CreatedFiles);

		  if (useCompoundFile)
		  {
			ICollection<string> filesToDelete = IndexWriter.createCompoundFile(InfoStream.Default, dir, MergeState.CheckAbort.NONE, info, NewIOContext(Random()));
			info.UseCompoundFile = true;
			foreach (String fileToDelete in filesToDelete)
			{
			  si1.Info.dir.DeleteFile(fileToDelete);
			}
		  }

		  return new SegmentCommitInfo(info, 0, -1L, -1L);
	   }


       private void PrintSegment(StreamWriter @out, SegmentCommitInfo si)
	   {
		  SegmentReader reader = new SegmentReader(si, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));

		  for (int i = 0; i < reader.NumDocs(); i++)
		  {
			@out.WriteLine(reader.Document(i));
		  }

		  Fields fields = reader.Fields();
		  foreach (string field in fields)
		  {
			Terms terms = fields.Terms(field);
			Assert.IsNotNull(terms);
			TermsEnum tis = terms.Iterator(null);
			while (tis.Next() != null)
			{

			  @out.Write("  term=" + field + ":" + tis.Term());
              @out.WriteLine("    DF=" + tis.DocFreq());

			  DocsAndPositionsEnum positions = tis.DocsAndPositions(reader.LiveDocs, null);

			  while (positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			  {
                  @out.Write(" doc=" + positions.DocID());
                  @out.Write(" TF=" + positions.Freq());
                  @out.Write(" pos=");
                  @out.Write(positions.NextPosition());
				for (int j = 1; j < positions.Freq(); j++)
				{
                    @out.Write("," + positions.NextPosition());
				}
                @out.WriteLine("");
			  }
			}
		  }
		  reader.Dispose();
	   }
	}

}