﻿using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene41Ords
{
    public sealed class Lucene41WithOrds : PostingsFormat
    {
        public Lucene41WithOrds()
            : base("Lucene41WithOrds")
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase docs = new Lucene41PostingsWriter(state);

            // TODO: should we make the terms index more easily
            // pluggable?  Ie so that this codec would record which
            // index impl was used, and switch on loading?
            // Or... you must make a new Codec for this?
            TermsIndexWriterBase indexWriter;
            bool success = false;
            try
            {
                indexWriter = new FixedGapTermsIndexWriter(state);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    docs.Dispose();
                }
            }

            success = false;
            try
            {
                // Must use BlockTermsWriter (not BlockTree) because
                // BlockTree doens't support ords (yet)...
                FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, docs);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        docs.Dispose();
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
            PostingsReaderBase postings = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
            TermsIndexReaderBase indexReader;

            bool success = false;
            try
            {
                indexReader = new FixedGapTermsIndexReader(state.Directory,
                                                           state.FieldInfos,
                                                           state.SegmentInfo.Name,
                                                           state.TermsIndexDivisor,
                                                           BytesRef.UTF8SortedAsUnicodeComparer,
                                                           state.SegmentSuffix, state.Context);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    postings.Dispose();
                }
            }

            success = false;
            try
            {
                FieldsProducer ret = new BlockTermsReader(indexReader,
                                                          state.Directory,
                                                          state.FieldInfos,
                                                          state.SegmentInfo,
                                                          postings,
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
                        postings.Dispose();
                    }
                    finally
                    {
                        indexReader.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Extension of freq postings file
        /// </summary>
        static readonly string FREQ_EXTENSION = "frq";

        /// <summary>
        /// Extension of prox postings file
        /// </summary>
        static readonly string PROX_EXTENSION = "prx";
    }
}
