﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Search.Suggest.Fst
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
    /// Suggester based on a weighted FST: it first traverses the prefix, 
    /// then walks the <i>n</i> shortest paths to retrieve top-ranked
    /// suggestions.
    /// <para>
    /// <b>NOTE</b>:
    /// Input weights must be between 0 and <seealso cref="Integer#MAX_VALUE"/>, any
    /// other values will be rejected.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class WFSTCompletionLookup : Lookup
    {

        /// <summary>
        /// FST<Long>, weights are encoded as costs: (Integer.MAX_VALUE-weight)
        /// </summary>
        // NOTE: like FSTSuggester, this is really a WFSA, if you want to
        // customize the code to add some output you should use PairOutputs.
        private FST<long?> fst = null;

        /// <summary>
        /// True if exact match suggestions should always be returned first.
        /// </summary>
        private readonly bool exactFirst;

        /// <summary>
        /// Number of entries the lookup was built with </summary>
        private long count = 0;

        /// <summary>
        /// Calls <seealso cref="#WFSTCompletionLookup(boolean) WFSTCompletionLookup(true)"/>
        /// </summary>
        public WFSTCompletionLookup()
            : this(true)
        {
        }

        /// <summary>
        /// Creates a new suggester.
        /// </summary>
        /// <param name="exactFirst"> <code>true</code> if suggestions that match the 
        ///        prefix exactly should always be returned first, regardless
        ///        of score. This has no performance impact, but could result
        ///        in low-quality suggestions. </param>
        public WFSTCompletionLookup(bool exactFirst)
        {
            this.exactFirst = exactFirst;
        }

        public override void Build(InputIterator iterator)
        {
            if (iterator.HasPayloads)
            {
                throw new ArgumentException("this suggester doesn't support payloads");
            }
            if (iterator.HasContexts)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            count = 0;
            var scratch = new BytesRef();
            InputIterator iter = new WFSTInputIterator(this, iterator);
            var scratchInts = new IntsRef();
            BytesRef previous = null;
            var outputs = PositiveIntOutputs.Singleton;
            var builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
            while ((scratch = iter.Next()) != null)
            {
                long cost = iter.Weight;

                if (previous == null)
                {
                    previous = new BytesRef();
                }
                else if (scratch.Equals(previous))
                {
                    continue; // for duplicate suggestions, the best weight is actually
                    // added
                }
                Lucene.Net.Util.Fst.Util.ToIntsRef(scratch, scratchInts);
                builder.Add(scratchInts, cost);
                previous.CopyBytes(scratch);
                count++;
            }
            fst = builder.Finish();
        }

        public override bool Store(DataOutput output)
        {
            output.WriteVLong(count);
            if (fst == null)
            {
                return false;
            }
            fst.Save(output);
            return true;
        }

        public override bool Load(DataInput input)
	  {
		count = input.ReadVLong();
		this.fst = new FST<>(input, PositiveIntOutputs.Singleton);
		return true;
	  }

        public override IList<LookupResult> DoLookup(string key, HashSet<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            if (contexts != null)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }
            Debug.Assert(num > 0);

            if (onlyMorePopular)
            {
                throw new System.ArgumentException("this suggester only works with onlyMorePopular=false");
            }

            if (fst == null)
            {
                return Collections.EmptyList<LookupResult>();
            }

            BytesRef scratch = new BytesRef(key);
            int prefixLength = scratch.Length;
            FST.Arc<long?> arc = new FST.Arc<long?>();

            // match the prefix portion exactly
            long? prefixOutput = null;
            try
            {
                prefixOutput = LookupPrefix(scratch, arc);
            }
            catch (IOException bogus)
            {
                throw new Exception(bogus);
            }

            if (prefixOutput == null)
            {
                return Collections.EmptyList();
            }

            IList<LookupResult> results = new List<LookupResult>(num);
            CharsRef spare = new CharsRef();
            if (exactFirst && arc.Final)
            {
                spare.grow(scratch.length);
                UnicodeUtil.UTF8toUTF16(scratch, spare);
                results.Add(new LookupResult(spare.ToString(), decodeWeight(prefixOutput + arc.NextFinalOutput)));
                if (--num == 0)
                {
                    return results; // that was quick
                }
            }

            // complete top-N
            Util.Fst.Util.TopResults<long?> completions = null;
            try
            {
                completions = Util.ShortestPaths(fst, arc, prefixOutput, weightComparator, num, !exactFirst);
                Debug.Assert(completions.IsComplete);
            }
            catch (IOException bogus)
            {
                throw new Exception(bogus);
            }

            BytesRef suffix = new BytesRef(8);
            foreach (Util.Fst.Util.Result<long?> completion in completions)
            {
                scratch.length = prefixLength;
                // append suffix
                Util.ToBytesRef(completion.input, suffix);
                scratch.Append(suffix);
                spare.Grow(scratch.Length);
                UnicodeUtil.UTF8toUTF16(scratch, spare);
                results.Add(new LookupResult(spare.ToString(), decodeWeight(completion.output)));
            }
            return results;
        }

        private long? LookupPrefix(BytesRef scratch, FST.Arc<long?> arc) //Bogus
        {
            Debug.Assert(0 == (long)fst.Outputs.NoOutput);
            long output = 0;
            var bytesReader = fst.BytesReader;

            fst.GetFirstArc(arc);

            sbyte[] bytes = scratch.Bytes;
            int pos = scratch.Offset;
            int end = pos + scratch.Length;
            while (pos < end)
            {
                if (fst.FindTargetArc(bytes[pos++] & 0xff, arc, arc, bytesReader) == null)
                {
                    return null;
                }
                else
                {
                    output += (long)arc.Output;
                }
            }

            return output;
        }

        /// <summary>
        /// Returns the weight associated with an input string,
        /// or null if it does not exist.
        /// </summary>
        public virtual object Get(string key)
        {
            if (fst == null)
            {
                return null;
            }
            Arc<long?> arc = new Arc<long?>();
            long? result = null;
            try
            {
                result = LookupPrefix(new BytesRef(key), arc);
            }
            catch (IOException bogus)
            {
                throw new Exception(bogus);
            }
            if (result == null || !arc.Final)
            {
                return null;
            }
            else
            {
                return Convert.ToInt32(decodeWeight(result + arc.NextFinalOutput));
            }
        }

        /// <summary>
        /// cost -> weight </summary>
        private static int decodeWeight(long encoded)
        {
            return (int)(int.MaxValue - encoded);
        }

        /// <summary>
        /// weight -> cost </summary>
        private static int encodeWeight(long value)
        {
            if (value < 0 || value > int.MaxValue)
            {
                throw new System.NotSupportedException("cannot encode value: " + value);
            }
            return int.MaxValue - (int)value;
        }

        private sealed class WFSTInputIterator : SortedInputIterator
        {
            private readonly WFSTCompletionLookup outerInstance;


            internal WFSTInputIterator(WFSTCompletionLookup outerInstance, InputIterator source)
                : base(source)
            {
                this.outerInstance = outerInstance;
                Debug.Assert(source.HasPayloads == false);
            }

            protected internal override void Encode(OfflineSorter.ByteSequencesWriter writer, ByteArrayDataOutput output, sbyte[] buffer, BytesRef spare, BytesRef payload, HashSet<BytesRef> contexts, long weight)
            {
                if (spare.Length + 4 >= buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, spare.Length + 4);
                }
                output.Reset(buffer);
                output.WriteBytes(spare.Bytes, spare.Offset, spare.Length);
                output.WriteInt(encodeWeight(weight));
                writer.Write(buffer, 0, output.Position);
            }

            protected internal override long Decode(BytesRef scratch, ByteArrayDataInput tmpInput)
            {
                scratch.Length -= 4; // int
                // skip suggestion:
                tmpInput.Reset(scratch.Bytes, scratch.Offset + scratch.Length, 4);
                return tmpInput.ReadInt();
            }
        }

        internal static readonly IComparer<long?> weightComparator = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<long?>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(long? left, long? right)
            {
                return left.CompareTo(right);
            }
        }

        /// <summary>
        /// Returns byte size of the underlying FST. </summary>
        public override long SizeInBytes()
        {
            return (fst == null) ? 0 : fst.SizeInBytes();
        }

        public override long Count
        {
            get
            {
                return count;
            }
        }
    }
}