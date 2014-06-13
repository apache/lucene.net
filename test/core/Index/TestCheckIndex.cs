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


	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Directory = Lucene.Net.Store.Directory;
	using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Token = Lucene.Net.Analysis.Token;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
    using NUnit.Framework;

	public class TestCheckIndex : LuceneTestCase
	{

	  public virtual void TestDeletedDocs()
	  {
		Directory dir = NewDirectory();
        IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));
		for (int i = 0;i < 19;i++)
		{
		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;
		  doc.Add(NewField("field", "aaa" + i, customType));
		  writer.AddDocument(doc);
		}
		writer.ForceMerge(1);
		writer.Commit();
		writer.DeleteDocuments(new Term("field","aaa5"));
		writer.Dispose();

		ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
		CheckIndex checker = new CheckIndex(dir);
		checker.InfoStream = new PrintStream(bos, false, IOUtils.UTF_8);
		if (VERBOSE)
		{
            checker.InfoStream = Console.Out;
		}
		CheckIndex.Status indexStatus = checker.CheckIndex();
		if (indexStatus.Clean == false)
		{
		  Console.WriteLine("CheckIndex failed");
		  Console.WriteLine(bos.ToString(IOUtils.UTF_8));
		  Assert.Fail();
		}

		CheckIndex.Status.SegmentInfoStatus seg = indexStatus.SegmentInfos[0];
		Assert.IsTrue(seg.OpenReaderPassed);

		Assert.IsNotNull(seg.Diagnostics);

		Assert.IsNotNull(seg.FieldNormStatus);
		Assert.IsNull(seg.FieldNormStatus.Error);
		Assert.AreEqual(1, seg.FieldNormStatus.TotFields);

		Assert.IsNotNull(seg.TermIndexStatus);
		Assert.IsNull(seg.TermIndexStatus.Error);
		Assert.AreEqual(18, seg.TermIndexStatus.TermCount);
		Assert.AreEqual(18, seg.TermIndexStatus.TotFreq);
		Assert.AreEqual(18, seg.TermIndexStatus.TotPos);

		Assert.IsNotNull(seg.StoredFieldStatus);
		Assert.IsNull(seg.StoredFieldStatus.Error);
		Assert.AreEqual(18, seg.StoredFieldStatus.DocCount);
		Assert.AreEqual(18, seg.StoredFieldStatus.TotFields);

		Assert.IsNotNull(seg.TermVectorStatus);
		Assert.IsNull(seg.TermVectorStatus.Error);
		Assert.AreEqual(18, seg.TermVectorStatus.DocCount);
		Assert.AreEqual(18, seg.TermVectorStatus.TotVectors);

		Assert.IsTrue(seg.Diagnostics.Count > 0);
		IList<string> onlySegments = new List<string>();
		onlySegments.Add("_0");

		Assert.IsTrue(checker.CheckIndex(onlySegments).Clean == true);
		dir.Dispose();
	  }

	  // LUCENE-4221: we have to let these thru, for now
	  public virtual void TestBogusTermVectors()
	  {
		Directory dir = NewDirectory();
		IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.StoreTermVectors = true;
		ft.StoreTermVectorOffsets = true;
		Field field = new Field("foo", "", ft);
		field.SetTokenStream(new CannedTokenStream(new Token("bar", 5, 10), new Token("bar", 1, 4)
	   ));
		doc.Add(field);
		iw.AddDocument(doc);
		iw.Dispose();
		dir.Dispose(); // checkindex
	  }
	}

}