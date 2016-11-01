﻿using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.IntBlock
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

    /// <summary>
    /// A silly test codec to verify core support for fixed
    /// sized int block encoders is working.The int encoder
    /// used here just writes each block as a series of vInt.
    /// </summary>
    public sealed class MockFixedIntBlockPostingsFormat : PostingsFormat
    {
        private readonly int blockSize;

        public MockFixedIntBlockPostingsFormat()
            : this(1)
        {
        }

        public MockFixedIntBlockPostingsFormat(int blockSize)
            : base("MockFixedIntBlock")
        {
            this.blockSize = blockSize;
        }

        public override string ToString()
        {
            return Name + "(blockSize=" + blockSize + ")";
        }

        // only for testing
        public IntStreamFactory getIntFactory()
        {
            return new MockIntFactory(blockSize);
        }

        /**
         * Encodes blocks as vInts of a fixed block size.
         */
        public class MockIntFactory : IntStreamFactory
        {
            private readonly int blockSize;

            public MockIntFactory(int blockSize)
            {
                this.blockSize = blockSize;
            }

            public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext context)
            {
                return new FixedIntBlockIndexInputAnonymousHelper(this, dir.OpenInput(fileName, context));
            }

            private class FixedIntBlockIndexInputAnonymousHelper : FixedIntBlockIndexInput
            {
                private readonly MockIntFactory outerInstance;

                public FixedIntBlockIndexInputAnonymousHelper(MockIntFactory outerInstance, IndexInput input)
                    : base(input)
                {
                    this.outerInstance = outerInstance;
                }

                protected override IBlockReader GetBlockReader(IndexInput @in, int[] buffer)
                {
                    return new BlockReaderAnonymousHelper(outerInstance, @in, buffer);
                }

                private class BlockReaderAnonymousHelper : FixedIntBlockIndexInput.IBlockReader
                {
                    private readonly MockIntFactory outerInstance;
                    private readonly IndexInput @in;
                    private readonly int[] buffer;

                    public BlockReaderAnonymousHelper(MockIntFactory outerInstance, IndexInput @in, int[] buffer)
                    {
                        this.outerInstance = outerInstance;
                        this.@in = @in;
                        this.buffer = buffer;
                    }
                    public void Seek(long pos)
                    {
                    }

                    public void ReadBlock()
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = @in.ReadVInt();
                        }
                    }
                }
            }


            public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext context)
            {
                IndexOutput output = dir.CreateOutput(fileName, context);
                bool success = false;
                try
                {
                    FixedIntBlockIndexOutputAnonymousHelper ret = new FixedIntBlockIndexOutputAnonymousHelper(output, blockSize);

                    success = true;
                    return ret;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(output);
                    }
                }
            }
        }

        private class FixedIntBlockIndexOutputAnonymousHelper : FixedIntBlockIndexOutput
        {
            public FixedIntBlockIndexOutputAnonymousHelper(IndexOutput output, int blockSize)
                : base(output, blockSize)
            {
            }
            protected override void FlushBlock()
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    output.WriteVInt(buffer[i]);
                }
            }
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockIntFactory(blockSize));

            bool success = false;
            TermsIndexWriterBase indexWriter;
            try
            {
                indexWriter = new FixedGapTermsIndexWriter(state);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    postingsWriter.Dispose();
                }
            }

            success = false;
            try
            {
                FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, postingsWriter);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        postingsWriter.Dispose();
                    }
                    finally
                    {
                        indexWriter.Dispose();
                    }
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postingsReader = new SepPostingsReader(state.Directory,
                                                                      state.FieldInfos,
                                                                      state.SegmentInfo,
                                                                      state.Context,
                                                                      new MockIntFactory(blockSize), state.SegmentSuffix);

            TermsIndexReaderBase indexReader;
            bool success = false;
            try
            {
                indexReader = new FixedGapTermsIndexReader(state.Directory,
                                                                 state.FieldInfos,
                                                                 state.SegmentInfo.Name,
                                                                 state.TermsIndexDivisor,
                                                                 BytesRef.UTF8SortedAsUnicodeComparer, state.SegmentSuffix,
                                                                 IOContext.DEFAULT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    postingsReader.Dispose();
                }
            }

            success = false;
            try
            {
                FieldsProducer ret = new BlockTermsReader(indexReader,
                                                          state.Directory,
                                                          state.FieldInfos,
                                                          state.SegmentInfo,
                                                          postingsReader,
                                                          state.Context,
                                                          state.SegmentSuffix);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        postingsReader.Dispose();
                    }
                    finally
                    {
                        indexReader.Dispose();
                    }
                }
            }
        }
    }
}
