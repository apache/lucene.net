using System;

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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestFilterAtomicReader : LuceneTestCase
	{

	  private class TestReader : FilterAtomicReader
	  {

		/// <summary>
		/// Filter that only permits terms containing 'e'. </summary>
		private class TestFields : FilterFields
		{
		  internal TestFields(Fields @in) : base(@in)
		  {
		  }

		  public override Terms Terms(string field)
		  {
			return new TestTerms(base.terms(field));
		  }
		}

		private class TestTerms : FilterTerms
		{
		  internal TestTerms(Terms @in) : base(@in)
		  {
		  }

		  public override TermsEnum Iterator(TermsEnum reuse)
		  {
			return new TestTermsEnum(base.iterator(reuse));
		  }
		}

		private class TestTermsEnum : FilterTermsEnum
		{
		  public TestTermsEnum(TermsEnum @in) : base(@in)
		  {
		  }

		  /// <summary>
		  /// Scan for terms containing the letter 'e'. </summary>
		  public override BytesRef Next()
		  {
			BytesRef text;
			while ((text = @in.next()) != null)
			{
			  if (text.utf8ToString().IndexOf('e') != -1)
			  {
				return text;
			  }
			}
			return null;
		  }

		  public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		  {
			return new TestPositions(base.docsAndPositions(liveDocs, reuse == null ? null : ((FilterDocsAndPositionsEnum) reuse).@in, flags));
		  }
		}

		/// <summary>
		/// Filter that only returns odd numbered documents. </summary>
		private class TestPositions : FilterDocsAndPositionsEnum
		{
		  public TestPositions(DocsAndPositionsEnum @in) : base(@in)
		  {
		  }

		  /// <summary>
		  /// Scan for odd numbered documents. </summary>
		  public override int NextDoc()
		  {
			int doc;
			while ((doc = @in.nextDoc()) != NO_MORE_DOCS)
			{
			  if ((doc % 2) == 1)
			  {
				return doc;
			  }
			}
			return NO_MORE_DOCS;
		  }
		}

		public TestReader(IndexReader reader) : base(SlowCompositeReaderWrapper.wrap(reader))
		{
		}

		public override Fields Fields()
		{
		  return new TestFields(base.fields());
		}
	  }

	  /// <summary>
	  /// Tests the IndexReader.getFieldNames implementation </summary>
	  /// <exception cref="Exception"> on error </exception>
	  public virtual void TestFilterIndexReader()
	  {
		Directory directory = newDirectory();

		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		Document d1 = new Document();
		d1.add(newTextField("default", "one two", Field.Store.YES));
		writer.addDocument(d1);

		Document d2 = new Document();
		d2.add(newTextField("default", "one three", Field.Store.YES));
		writer.addDocument(d2);

		Document d3 = new Document();
		d3.add(newTextField("default", "two four", Field.Store.YES));
		writer.addDocument(d3);

		writer.close();

		Directory target = newDirectory();

		// We mess with the postings so this can fail:
		((BaseDirectoryWrapper) target).CrossCheckTermVectorsOnClose = false;

		writer = new IndexWriter(target, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		IndexReader reader = new TestReader(DirectoryReader.open(directory));
		writer.addIndexes(reader);
		writer.close();
		reader.close();
		reader = DirectoryReader.open(target);

		TermsEnum terms = MultiFields.getTerms(reader, "default").iterator(null);
		while (terms.next() != null)
		{
		  Assert.IsTrue(terms.term().utf8ToString().IndexOf('e') != -1);
		}

		Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.seekCeil(new BytesRef("one")));

		DocsAndPositionsEnum positions = terms.docsAndPositions(MultiFields.getLiveDocs(reader), null);
		while (positions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.IsTrue((positions.docID() % 2) == 1);
		}

		reader.close();
		directory.close();
		target.close();
	  }

	  private static void CheckOverrideMethods(Type clazz)
	  {
		Type superClazz = clazz.BaseType;
		foreach (Method m in superClazz.GetMethods())
		{
		  int mods = m.Modifiers;
		  if (Modifier.isStatic(mods) || Modifier.isAbstract(mods) || Modifier.isFinal(mods) || m.Synthetic || m.Name.Equals("attributes"))
		  {
			continue;
		  }
		  // The point of these checks is to ensure that methods that have a default
		  // impl through other methods are not overridden. this makes the number of
		  // methods to override to have a working impl minimal and prevents from some
		  // traps: for example, think about having getCoreCacheKey delegate to the
		  // filtered impl by default
		  Method subM = clazz.GetMethod(m.Name, m.ParameterTypes);
		  if (subM.DeclaringClass == clazz && m.DeclaringClass != typeof(object) && m.DeclaringClass != subM.DeclaringClass)
		  {
			Assert.Fail(clazz + " overrides " + m + " although it has a default impl");
		  }
		}
	  }

	  public virtual void TestOverrideMethods()
	  {
		CheckOverrideMethods(typeof(FilterAtomicReader));
		CheckOverrideMethods(typeof(FilterAtomicReader.FilterFields));
		CheckOverrideMethods(typeof(FilterAtomicReader.FilterTerms));
		CheckOverrideMethods(typeof(FilterAtomicReader.FilterTermsEnum));
		CheckOverrideMethods(typeof(FilterAtomicReader.FilterDocsEnum));
		CheckOverrideMethods(typeof(FilterAtomicReader.FilterDocsAndPositionsEnum));
	  }

	}

}