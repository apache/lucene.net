using System.Diagnostics;

namespace Lucene.Net.Util.Fst
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
    /// Enumerates all input (BytesRef) + output pairs in an
    ///  FST.
    ///
    /// @lucene.experimental
    /// </summary>

    public sealed class BytesRefFSTEnum<T> : FSTEnum<T>
    {
        private readonly BytesRef current = new BytesRef(10);
        private readonly BytesRefFSTEnum.InputOutput<T> result = new BytesRefFSTEnum.InputOutput<T>();
        private BytesRef target;

        // LUCENENET NOTE: InputOutput<T> was moved to the BytesRefFSTEnum class

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        ///  doFloor is true, advance positions to the biggest
        ///  term before target.
        /// </summary>
        public BytesRefFSTEnum(FST<T> fst)
            : base(fst)
        {
            result.Input = current;
            current.Offset = 1;
        }

        public BytesRefFSTEnum.InputOutput<T> Current()
        {
            return result;
        }

        public BytesRefFSTEnum.InputOutput<T> Next()
        {
            //System.out.println("  enum.next");
            DoNext();
            return SetResult();
        }

        /// <summary>
        /// Seeks to smallest term that's >= target. </summary>
        public BytesRefFSTEnum.InputOutput<T> SeekCeil(BytesRef target)
        {
            this.target = target;
            targetLength = target.Length;
            base.DoSeekCeil();
            return SetResult();
        }

        /// <summary>
        /// Seeks to biggest term that's <= target. </summary>
        public BytesRefFSTEnum.InputOutput<T> SeekFloor(BytesRef target)
        {
            this.target = target;
            targetLength = target.Length;
            base.DoSeekFloor();
            return SetResult();
        }

        /// <summary>
        /// Seeks to exactly this term, returning null if the term
        ///  doesn't exist.  this is faster than using {@link
        ///  #seekFloor} or <seealso cref="#seekCeil"/> because it
        ///  short-circuits as soon the match is not found.
        /// </summary>
        public BytesRefFSTEnum.InputOutput<T> SeekExact(BytesRef target)
        {
            this.target = target;
            targetLength = target.Length;
            if (base.DoSeekExact())
            {
                Debug.Assert(upto == 1 + target.Length);
                return SetResult();
            }
            else
            {
                return null;
            }
        }

        protected internal override int TargetLabel
        {
            get
            {
                if (upto - 1 == target.Length)
                {
                    return FST.END_LABEL;
                }
                else
                {
                    return target.Bytes[target.Offset + upto - 1] & 0xFF;
                }
            }
        }

        protected internal override int CurrentLabel
        {
            get
            {
                // current.offset fixed at 1
                return current.Bytes[upto] & 0xFF;
            }
            set
            {
                current.Bytes[upto] = (byte)value;
            }
        }

        protected internal override void Grow()
        {
            current.Bytes = ArrayUtil.Grow(current.Bytes, upto + 1);
        }

        private BytesRefFSTEnum.InputOutput<T> SetResult()
        {
            if (upto == 0)
            {
                return null;
            }
            else
            {
                current.Length = upto - 1;
                result.Output = output[upto];
                return result;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific. This class is to mimic Java's ability to specify
    /// nested classes of Generics without having to specify the generic type
    /// (i.e. BytesRefFSTEnum.InputOutput{T} rather than BytesRefFSTEnum{T}.InputOutput{T})
    /// </summary>
    public sealed class BytesRefFSTEnum
    {
        private BytesRefFSTEnum()
        { }

        /// <summary>
        /// Holds a single input (BytesRef) + output pair. </summary>
        public class InputOutput<T>
        {
            public BytesRef Input;
            public T Output;
        }
    }
}