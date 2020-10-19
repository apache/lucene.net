using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using static Lucene.Net.Codecs.Asserting.AssertingDocValuesFormat;

namespace Lucene.Net.Codecs.Asserting
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
    /// Just like <see cref="Lucene42NormsFormat"/> but with additional asserts.
    /// </summary>
    public class AssertingNormsFormat : NormsFormat
    {
        private readonly NormsFormat @in = new Lucene42NormsFormat();

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            DocValuesConsumer consumer = @in.NormsConsumer(state);
            if (Debugging.ShouldAssert(consumer != null)) Debugging.ThrowAssert();
            return new AssertingNormsConsumer(consumer, state.SegmentInfo.DocCount);
        }

        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            if (Debugging.ShouldAssert(state.FieldInfos.HasNorms)) Debugging.ThrowAssert();
            DocValuesProducer producer = @in.NormsProducer(state);
            if (Debugging.ShouldAssert(producer != null)) Debugging.ThrowAssert();
            return new AssertingDocValuesProducer(producer, state.SegmentInfo.DocCount);
        }
    }
}