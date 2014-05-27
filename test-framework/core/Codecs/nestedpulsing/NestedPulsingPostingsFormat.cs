namespace Lucene.Net.Codecs.nestedpulsing
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

	using Lucene41PostingsReader = Lucene.Net.Codecs.Lucene41.Lucene41PostingsReader;
	using Lucene41PostingsWriter = Lucene.Net.Codecs.Lucene41.Lucene41PostingsWriter;
	using PulsingPostingsReader = Lucene.Net.Codecs.pulsing.PulsingPostingsReader;
	using PulsingPostingsWriter = Lucene.Net.Codecs.pulsing.PulsingPostingsWriter;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Pulsing(1, Pulsing(2, Lucene41))
	/// @lucene.experimental
	/// </summary>
	// TODO: if we create PulsingPostingsBaseFormat then we
	// can simplify this? note: I don't like the *BaseFormat
	// hierarchy, maybe we can clean that up...
	public sealed class NestedPulsingPostingsFormat : PostingsFormat
	{
	  public NestedPulsingPostingsFormat() : base("NestedPulsing")
	  {
	  }

	  public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
	  {
		PostingsWriterBase docsWriter = null;
		PostingsWriterBase pulsingWriterInner = null;
		PostingsWriterBase pulsingWriter = null;

		// Terms dict
		bool success = false;
		try
		{
		  docsWriter = new Lucene41PostingsWriter(state);

		  pulsingWriterInner = new PulsingPostingsWriter(state, 2, docsWriter);
		  pulsingWriter = new PulsingPostingsWriter(state, 1, pulsingWriterInner);
		  FieldsConsumer ret = new BlockTreeTermsWriter(state, pulsingWriter, BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(docsWriter, pulsingWriterInner, pulsingWriter);
		  }
		}
	  }

	  public override FieldsProducer FieldsProducer(SegmentReadState state)
	  {
		PostingsReaderBase docsReader = null;
		PostingsReaderBase pulsingReaderInner = null;
		PostingsReaderBase pulsingReader = null;
		bool success = false;
		try
		{
		  docsReader = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
		  pulsingReaderInner = new PulsingPostingsReader(state, docsReader);
		  pulsingReader = new PulsingPostingsReader(state, pulsingReaderInner);
		  FieldsProducer ret = new BlockTreeTermsReader(state.Directory, state.FieldInfos, state.SegmentInfo, pulsingReader, state.Context, state.SegmentSuffix, state.TermsIndexDivisor);
		  success = true;
		  return ret;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(docsReader, pulsingReaderInner, pulsingReader);
		  }
		}
	  }
	}

}