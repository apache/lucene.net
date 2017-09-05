﻿using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask
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
    /// Run the benchmark algorithm.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///     <item><description>Read algorithm.</description></item>
    ///     <item><description>Run the algorithm.</description></item>
    /// </list>
    /// <para/>
    /// Things to be added/fixed in "Benchmarking by tasks":
    /// <list type="number">
    ///     <item><description>TODO - report into Excel and/or graphed view.</description></item>
    ///     <item><description>TODO - perf comparison between Lucene releases over the years.</description></item>
    ///     <item><description>TODO - perf report adequate to include in Lucene nightly build site? (so we can easily track performance changes.)</description></item>
    ///     <item><description>TODO - add overall time control for repeated execution (vs. current by-count only).</description></item>
    ///     <item><description>TODO - query maker that is based on index statistics.</description></item>
    /// </list>
    /// </remarks>
    public class Benchmark
    {
        private PerfRunData runData;
        private Algorithm algorithm;
        private bool executed;

        public Benchmark(TextReader algReader)
        {
            // prepare run data
            try
            {
                runData = new PerfRunData(new Config(algReader));
            }
            catch (Exception e)
            {
                //e.printStackTrace();
                throw new Exception("Error: cannot init PerfRunData!", e);
            }

            // parse algorithm
            try
            {
                algorithm = new Algorithm(runData);
            }
            catch (Exception e)
            {
                throw new Exception("Error: cannot understand algorithm!", e);
            }
        }

        /// <summary>
        /// Execute this benchmark.
        /// </summary>
        public virtual void Execute()
        {
            lock (this)
            {
                if (executed)
                {
                    throw new InvalidOperationException("Benchmark was already executed");
                }
                executed = true;
                runData.SetStartTimeMillis();
                algorithm.Execute();
            }
        }

        /// <summary>
        /// Run the benchmark algorithm.
        /// </summary>
        /// <param name="args">Benchmark config and algorithm files.</param>
        public static void Main(string[] args)
        {
            Exec(args);
        }

        /// <summary>
        /// Utility: execute benchmark from command line.
        /// </summary>
        /// <param name="args">Single argument is expected: algorithm-file.</param>
        public static void Exec(string[] args)
        {
            // verify command line args
            if (args.Length < 1)
            {
                SystemConsole.WriteLine("Usage: java Benchmark <algorithm file>");
                Environment.Exit(1);
            }

            // verify input files 
            FileInfo algFile = new FileInfo(args[0]);
            if (!algFile.Exists /*|| !algFile.isFile() ||!algFile.canRead()*/ )
            {
                SystemConsole.WriteLine("cannot find/read algorithm file: " + algFile.FullName);
                Environment.Exit(1);
            }

            SystemConsole.WriteLine("Running algorithm from: " + algFile.FullName);

            Benchmark benchmark = null;
            try
            {
                benchmark = new Benchmark(IOUtils.GetDecodingReader(algFile, Encoding.UTF8));
            }
            catch (Exception e)
            {
                SystemConsole.WriteLine(e.ToString());
                Environment.Exit(1);
            }

            SystemConsole.WriteLine("------------> algorithm:");
            SystemConsole.WriteLine(benchmark.Algorithm.ToString());

            // execute
            try
            {
                benchmark.Execute();
            }
            catch (Exception e)
            {
                SystemConsole.WriteLine("Error: cannot execute the algorithm! " + e.Message);
                SystemConsole.WriteLine(e.StackTrace);
            }

            SystemConsole.WriteLine("####################");
            SystemConsole.WriteLine("###  D O N E !!! ###");
            SystemConsole.WriteLine("####################");
        }

        /// <summary>
        /// Returns the algorithm.
        /// </summary>
        public virtual Algorithm Algorithm
        {
            get { return algorithm; }
        }

        /// <summary>
        /// Returns the runData.
        /// </summary>
        public virtual PerfRunData RunData
        {
            get { return runData; }
        }
    }
}
