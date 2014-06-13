namespace Lucene.Net.Codecs.Lucene40
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
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Read-write version of <seealso cref="Lucene40DocValuesFormat"/> for testing </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("deprecation") public class Lucene40RWDocValuesFormat extends Lucene40DocValuesFormat
	public class Lucene40RWDocValuesFormat : Lucene40DocValuesFormat
	{
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
		if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
		{
		  return base.fieldsConsumer(state);
		}
		else
		{
		  string filename = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "dv", IndexFileNames.COMPOUND_FILE_EXTENSION);
		  return new Lucene40DocValuesWriter(state, filename, Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY);
		}
		}
	}

}