﻿using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Stats
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
    /// Statistics for a task run. 
    /// <para/>
    /// The same task can run more than once, but, if that task records statistics, 
    /// each run would create its own TaskStats.
    /// </summary>
    public class TaskStats
    {
        /// <summary>Task for which data was collected.</summary>
        private PerfTask task;

        /// <summary>Round in which task run started.</summary>
        private int round;

        /// <summary>Task start time.</summary>
        private long start;

        /// <summary>Task elapsed time.  elapsed >= 0 indicates run completion!</summary>
        private long elapsed = -1;

        /// <summary>Max tot mem during task.</summary>
        private long maxTotMem;

        /// <summary>Max used mem during task.</summary>
        private long maxUsedMem;

        /// <summary>Serial run number of this task run in the perf run.</summary>
        private int taskRunNum;

        /// <summary>Number of other tasks that started to run while this task was still running.</summary>
        private int numParallelTasks;

        /// <summary>
        /// Number of work items done by this task.
        /// For indexing that can be number of docs added.
        /// For warming that can be number of scanned items, etc. 
        /// For repeating tasks, this is a sum over repetitions.
        /// </summary>
        private int count;

        /// <summary>
        /// Number of similar tasks aggregated into this record.   
        /// Used when summing up on few runs/instances of similar tasks.
        /// </summary>
        private int numRuns = 1;

        /// <summary>
        /// Create a run data for a task that is starting now.
        /// To be called from Points.
        /// </summary>
        internal TaskStats(PerfTask task, int taskRunNum, int round)
        {
            this.task = task;
            this.taskRunNum = taskRunNum;
            this.round = round;
            maxTotMem = GC.GetTotalMemory(false); //Runtime.getRuntime().totalMemory();
            maxUsedMem = maxTotMem; // - Runtime.getRuntime().freeMemory(); // LUCENENET TODO: available RAM
            start = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Mark the end of a task.
        /// </summary>
        internal void MarkEnd(int numParallelTasks, int count)
        {
            elapsed = Support.Time.CurrentTimeMilliseconds();
            long totMem = GC.GetTotalMemory(false); //Runtime.getRuntime().totalMemory();
            if (totMem > maxTotMem)
            {
                maxTotMem = totMem;
            }
            long usedMem = totMem; //- Runtime.getRuntime().freeMemory(); // LUCENENET TODO: available RAM
            if (usedMem > maxUsedMem)
            {
                maxUsedMem = usedMem;
            }
            this.numParallelTasks = numParallelTasks;
            this.count = count;
        }

        private int[] countsByTime;
        private long countsByTimeStepMSec;

        public virtual void SetCountsByTime(int[] counts, long msecStep)
        {
            countsByTime = counts;
            countsByTimeStepMSec = msecStep;
        }

        [WritableArray]
        public virtual int[] GetCountsByTime()
        {
            return countsByTime; 
        }

        public virtual long CountsByTimeStepMSec
        {
            get { return countsByTimeStepMSec; }
        }

        /// <summary>Gets the taskRunNum.</summary>
        public virtual int TaskRunNum
        {
            get { return taskRunNum; }
        }

        /// <seealso cref="object.ToString()"/>
        public override string ToString()
        {
            StringBuilder res = new StringBuilder(task.GetName());
            res.Append(" ");
            res.Append(count);
            res.Append(" ");
            res.Append(elapsed);
            return res.ToString();
        }

        /// <summary>Gets the count.</summary>
        public virtual int Count
        {
            get { return count; }
        }

        /// <summary>Gets elapsed time.</summary>
        public virtual long Elapsed
        {
            get { return elapsed; }
        }

        /// <summary>Gets the maxTotMem.</summary>
        public virtual long MaxTotMem
        {
            get { return maxTotMem; }
        }

        /// <summary>Gets the maxUsedMem.</summary>
        public virtual long MaxUsedMem
        {
            get { return maxUsedMem; }
        }

        /// <summary>Gets the numParallelTasks.</summary>
        public virtual int NumParallelTasks
        {
            get { return numParallelTasks; }
        }

        /// <summary>Gets the task.</summary>
        public virtual PerfTask Task
        {
            get { return task; }
        }

        /// <summary>Gets the numRuns.</summary>
        public virtual int NumRuns
        {
            get { return numRuns; }
        }

        /// <summary>
        /// Add data from another stat, for aggregation.
        /// </summary>
        /// <param name="stat2">The added stat data.</param>
        public virtual void Add(TaskStats stat2)
        {
            numRuns += stat2.NumRuns;
            elapsed += stat2.Elapsed;
            maxTotMem += stat2.MaxTotMem;
            maxUsedMem += stat2.MaxUsedMem;
            count += stat2.Count;
            if (round != stat2.round)
            {
                round = -1; // no meaning if aggregating tasks of different round. 
            }

            if (countsByTime != null && stat2.countsByTime != null)
            {
                if (countsByTimeStepMSec != stat2.countsByTimeStepMSec)
                {
                    throw new InvalidOperationException("different by-time msec step");
                }
                if (countsByTime.Length != stat2.countsByTime.Length)
                {
                    throw new InvalidOperationException("different by-time msec count");
                }
                for (int i = 0; i < stat2.countsByTime.Length; i++)
                {
                    countsByTime[i] += stat2.countsByTime[i];
                }
            }
        }

#if FEATURE_CLONEABLE
        /// <seealso cref="ICloneable.Clone()"/>
#endif
        public virtual object Clone()
        {
            TaskStats c = (TaskStats)base.MemberwiseClone();
            if (c.countsByTime != null)
            {
                c.countsByTime = (int[])c.countsByTime.Clone();
            }
            return c;
        }

        /// <summary>Gets the round number.</summary>
        public virtual int Round
        {
            get { return round; }
        }
    }
}
