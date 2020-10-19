using J2N.Runtime.CompilerServices;
using J2N.Threading;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Controls the health status of a <see cref="DocumentsWriter"/> sessions. This class
    /// used to block incoming indexing threads if flushing significantly slower than
    /// indexing to ensure the <see cref="DocumentsWriter"/>s healthiness. If flushing is
    /// significantly slower than indexing the net memory used within an
    /// <see cref="IndexWriter"/> session can increase very quickly and easily exceed the
    /// runtime's available memory.
    /// <para/>
    /// To prevent OOM Errors and ensure <see cref="IndexWriter"/>'s stability this class blocks
    /// incoming threads from indexing once 2 x number of available
    /// <see cref="DocumentsWriterPerThreadPool.ThreadState"/> is exceeded.
    /// Once flushing catches up and the number of flushing DWPT is equal or lower
    /// than the number of active <see cref="DocumentsWriterPerThreadPool.ThreadState"/>s threads are released and can
    /// continue indexing.
    /// </summary>
    internal sealed class DocumentsWriterStallControl
    {
        private volatile bool stalled;
        private int numWaiting; // only with assert
        private bool wasStalled; // only with assert
        private readonly IDictionary<ThreadJob, bool?> waiting = new JCG.Dictionary<ThreadJob, bool?>(IdentityEqualityComparer<ThreadJob>.Default); // only with assert

        /// <summary>
        /// Update the stalled flag status. this method will set the stalled flag to
        /// <c>true</c> iff the number of flushing
        /// <see cref="DocumentsWriterPerThread"/> is greater than the number of active
        /// <see cref="DocumentsWriterPerThread"/>. Otherwise it will reset the
        /// <see cref="DocumentsWriterStallControl"/> to healthy and release all threads
        /// waiting on <see cref="WaitIfStalled()"/>
        /// </summary>
        internal void UpdateStalled(bool stalled)
        {
            lock (this)
            {
                this.stalled = stalled;
                if (stalled)
                {
                    wasStalled = true;
                }
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Blocks if documents writing is currently in a stalled state.
        ///
        /// </summary>
        internal void WaitIfStalled()
        {
            if (stalled)
            {
                lock (this)
                {
                    if (stalled) // react on the first wakeup call!
                    {
                        // don't loop here, higher level logic will re-stall!
//#if FEATURE_THREAD_INTERRUPT
//                        try
//                        {
//#endif
                        // LUCENENET: make sure not to run IncWaiters / DecrWaiters in Debugging.Assert as that gets 
                        // disabled in production
                        var result = IncWaiters();
                        if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(result);
                        Monitor.Wait(this);
                        result = DecrWaiters();
                        if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(result);
//#if FEATURE_THREAD_INTERRUPT // LUCENENET NOTE: Senseless to catch and rethrow the same exception type
//                        }
//                        catch (ThreadInterruptedException e)
//                        {
//                            throw new ThreadInterruptedException("Thread Interrupted Exception", e);
//                        }
//#endif
                    }
                }
            }
        }

        internal bool AnyStalledThreads()
        {
            return stalled;
        }

        private bool IncWaiters()
        {
            numWaiting++;
            if (Debugging.AssertsEnabled && Debugging.ShouldAssert(!waiting.ContainsKey(ThreadJob.CurrentThread))) Debugging.ThrowAssert();
            waiting[ThreadJob.CurrentThread] = true;

            return numWaiting > 0;
        }

        private bool DecrWaiters()
        {
            numWaiting--;
            bool removed = waiting.Remove(ThreadJob.CurrentThread);
            if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(removed);

            return numWaiting >= 0;
        }

        internal bool HasBlocked // for tests
        {
            get
            {
                lock (this)
                {
                    return numWaiting > 0;
                }
            }
        }

        internal bool IsHealthy => !stalled; // volatile read!

        internal bool IsThreadQueued(ThreadJob t) // for tests
        {
            lock (this)
            {
                return waiting.ContainsKey(t);
            }
        }

        internal bool WasStalled // for tests
        {
            get
            {
                lock (this)
                {
                    return wasStalled;
                }
            }
        }
    }
}