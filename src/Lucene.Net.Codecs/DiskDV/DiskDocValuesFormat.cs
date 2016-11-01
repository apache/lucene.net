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


namespace Lucene.Net.Codecs.DiskDV
{
    using Codecs;
    using Lucene45;
    using Index;
    using System;
    using Util;
    using System.Collections.Generic;

    /// <summary>
    /// DocValues format that keeps most things on disk.
    /// Only things like disk offsets are loaded into ram.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class DiskDocValuesFormat : DocValuesFormat
    {

        public const String DATA_CODEC = "DiskDocValuesData";
        public const String DATA_EXTENSION = "dvdd";
        public const String META_CODEC = "DiskDocValuesMetadata";
        public const String META_EXTENSION = "dvdm";

        public DiskDocValuesFormat() : base("Disk")
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new Lucene45DocValuesConsumerAnonymousHelper(this, state);
        }

        private class Lucene45DocValuesConsumerAnonymousHelper : Lucene45DocValuesConsumer
        {
            private readonly DiskDocValuesFormat outerInstance;

            public Lucene45DocValuesConsumerAnonymousHelper(DiskDocValuesFormat outerInstance, SegmentWriteState state)
                : base(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION)
            {
                this.outerInstance = outerInstance;
            }

            protected override void AddTermsDict(FieldInfo field, IEnumerable<BytesRef> values)
            {
                AddBinaryField(field, values);
            }
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new DiskDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION);
        }

    }
}