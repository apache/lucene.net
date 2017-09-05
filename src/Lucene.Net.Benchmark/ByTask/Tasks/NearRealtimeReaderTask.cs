﻿using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Spawns a BG thread that periodically (defaults to 3.0
    /// seconds, but accepts param in seconds) wakes up and asks
    /// IndexWriter for a near real-time reader.  Then runs a
    /// single query (body: 1) sorted by docdate, and prints
    /// time to reopen and time to run the search.
    /// <para/>
    /// @lucene.experimental It's also not generally usable, eg
    /// you cannot change which query is executed.
    /// </summary>
    public class NearRealtimeReaderTask : PerfTask
    {
        internal long pauseMSec = 3000L;

        internal int reopenCount;
        internal int[] reopenTimes = new int[1];

        public NearRealtimeReaderTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            PerfRunData runData = RunData;

            // Get initial reader
            IndexWriter w = runData.IndexWriter;
            if (w == null)
            {
                throw new Exception("please open the writer before invoking NearRealtimeReader");
            }

            if (runData.GetIndexReader() != null)
            {
                throw new Exception("please close the existing reader before invoking NearRealtimeReader");
            }


            long t = Support.Time.CurrentTimeMilliseconds();
            DirectoryReader r = DirectoryReader.Open(w, true);
            runData.SetIndexReader(r);
            // Transfer our reference to runData
            r.DecRef();

            // TODO: gather basic metrics for reporting -- eg mean,
            // stddev, min/max reopen latencies

            // Parent sequence sets stopNow
            reopenCount = 0;
            while (!Stop)
            {
                long waitForMsec = (pauseMSec - (Support.Time.CurrentTimeMilliseconds() - t));
                if (waitForMsec > 0)
                {
                    Thread.Sleep((int)waitForMsec);
                    //System.out.println("NRT wait: " + waitForMsec + " msec");
                }

                t = Support.Time.CurrentTimeMilliseconds();
                DirectoryReader newReader = DirectoryReader.OpenIfChanged(r);
                if (newReader != null)
                {
                    int delay = (int)(Support.Time.CurrentTimeMilliseconds() - t);
                    if (reopenTimes.Length == reopenCount)
                    {
                        reopenTimes = ArrayUtil.Grow(reopenTimes, 1 + reopenCount);
                    }
                    reopenTimes[reopenCount++] = delay;
                    // TODO: somehow we need to enable warming, here
                    runData.SetIndexReader(newReader);
                    // Transfer our reference to runData
                    newReader.DecRef();
                    r = newReader;
                }
            }
            Stop = false;

            return reopenCount;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            pauseMSec = (long)(1000.0 * float.Parse(@params, CultureInfo.InvariantCulture));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemConsole.WriteLine("NRT reopen times:");
                for (int i = 0; i < reopenCount; i++)
                {
                    SystemConsole.Write(" " + reopenTimes[i]);
                }
                SystemConsole.WriteLine();
            }
        }

        public override bool SupportsParams
        {
            get { return true; }
        }
    }
}
