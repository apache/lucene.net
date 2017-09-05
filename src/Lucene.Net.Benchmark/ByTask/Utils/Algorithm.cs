﻿using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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
    /// Test algorithm, as read from file
    /// </summary>
    public class Algorithm
    {
        private TaskSequence sequence;
        private readonly string[] taskPackages;

        /// <summary>
        /// Read algorithm from file.
        /// Property examined: alt.tasks.packages == comma separated list of 
        /// alternate Assembly names where tasks would be searched for, when not found 
        /// in the default Assembly (that of <see cref="PerfTask"/>).
        /// If the same task class appears in more than one Assembly, the Assembly
        /// indicated first in this list will be used.
        /// <para/>
        /// The Lucene.Net implementation differs from Lucene in that all
        /// referenced assemblies are also scanned for the type. However,
        /// alt.tasks.packages may be included for assemblies that are
        /// not referenced in your project.
        /// </summary>
        /// <param name="runData">perf-run-data used at running the tasks.</param>
        /// <exception cref="Exception">if errors while parsing the algorithm.</exception>
        public Algorithm(PerfRunData runData)
        {
            Config config = runData.Config;
            taskPackages = InitTasksPackages(config);
            string algTxt = config.AlgorithmText;
            sequence = new TaskSequence(runData, null, null, false);
            TaskSequence currSequence = sequence;
            PerfTask prevTask = null;
            StreamTokenizer stok = new StreamTokenizer(new StringReader(algTxt));
            stok.CommentChar('#');
            stok.IsEOLSignificant = false;
            stok.QuoteChar('"');
            stok.QuoteChar('\'');
            stok.OrdinaryChar('/');
            stok.OrdinaryChar('(');
            stok.OrdinaryChar(')');
            bool colonOk = false;
            bool isDisableCountNextTask = false; // only for primitive tasks
            currSequence.Depth = 0;

            while (stok.NextToken() != StreamTokenizer.TT_EOF)
            {
                switch (stok.TokenType)
                {

                    case StreamTokenizer.TT_WORD:
                        string s = stok.StringValue;
                        PerfTask task = (PerfTask)Activator.CreateInstance(TaskClass(config, s), runData);
                        task.AlgLineNum = stok.LineNumber;
                        task.DisableCounting = isDisableCountNextTask;
                        isDisableCountNextTask = false;
                        currSequence.AddTask(task);
                        if (task is RepSumByPrefTask)
                        {
                            stok.NextToken();
                            string prefix = stok.StringValue;
                            if (prefix == null || prefix.Length == 0)
                            {
                                throw new Exception("named report prefix problem - " + stok.ToString());
                            }
                          ((RepSumByPrefTask)task).SetPrefix(prefix);
                        }
                        // check for task param: '(' someParam ')'
                        stok.NextToken();
                        if (stok.TokenType != '(')
                        {
                            stok.PushBack();
                        }
                        else
                        {
                            // get params, for tasks that supports them - allow recursive parenthetical expressions
                            stok.IsEOLSignificant = true;  // Allow params tokenizer to keep track of line number
                            StringBuilder @params = new StringBuilder();
                            stok.NextToken();
                            if (stok.TokenType != ')')
                            {
                                int count = 1;
                                while (true)
                                {
                                    switch (stok.TokenType)
                                    {
                                        case StreamTokenizer.TT_NUMBER:
                                            {
                                                @params.Append(stok.NumberValue);
                                                break;
                                            }
                                        case StreamTokenizer.TT_WORD:
                                            {
                                                @params.Append(stok.StringValue);
                                                break;
                                            }
                                        case StreamTokenizer.TT_EOF:
                                            {
                                                throw new Exception("Unexpexted EOF: - " + stok.ToString());
                                            }
                                        case '"':
                                        case '\'':
                                            {
                                                @params.Append((char)stok.TokenType);
                                                // re-escape delimiters, if any
                                                @params.Append(stok.StringValue.Replace("" + (char)stok.TokenType, @"\" + (char)stok.TokenType));
                                                @params.Append((char)stok.TokenType);
                                                break;
                                            }
                                        case '(':
                                            {
                                                @params.Append((char)stok.TokenType);
                                                ++count;
                                                break;
                                            }
                                        case ')':
                                            {
                                                if (--count >= 1)
                                                {  // exclude final closing parenthesis
                                                    @params.Append((char)stok.TokenType);
                                                }
                                                else
                                                {
                                                    goto BALANCED_PARENS_BREAK;
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                @params.Append((char)stok.TokenType);
                                                break;
                                            }
                                    }
                                    stok.NextToken();
                                }
                                BALANCED_PARENS_BREAK: { }
                            }
                            stok.IsEOLSignificant = false;
                            string prm = @params.ToString().Trim();
                            if (prm.Length > 0)
                            {
                                task.SetParams(prm);
                            }
                        }

                        // ---------------------------------------
                        colonOk = false; prevTask = task;
                        break;

                    default:
                        char c = (char)stok.TokenType;

                        switch (c)
                        {

                            case ':':
                                if (!colonOk) throw new Exception("colon unexpexted: - " + stok.ToString());
                                colonOk = false;
                                // get repetitions number
                                stok.NextToken();
                                if ((char)stok.TokenType == '*')
                                {
                                    ((TaskSequence)prevTask).SetRepetitions(TaskSequence.REPEAT_EXHAUST);
                                }
                                else
                                {
                                    if (stok.TokenType != StreamTokenizer.TT_NUMBER)
                                    {
                                        throw new Exception("expected repetitions number or XXXs: - " + stok.ToString());
                                    }
                                    else
                                    {
                                        double num = stok.NumberValue;
                                        stok.NextToken();
                                        if (stok.TokenType == StreamTokenizer.TT_WORD && stok.StringValue.Equals("s", StringComparison.Ordinal))
                                        {
                                            ((TaskSequence)prevTask).SetRunTime(num);
                                        }
                                        else
                                        {
                                            stok.PushBack();
                                            ((TaskSequence)prevTask).SetRepetitions((int)num);
                                        }
                                    }
                                }
                                // check for rate specification (ops/min)
                                stok.NextToken();
                                if (stok.TokenType != ':')
                                {
                                    stok.PushBack();
                                }
                                else
                                {
                                    // get rate number
                                    stok.NextToken();
                                    if (stok.TokenType != StreamTokenizer.TT_NUMBER) throw new Exception("expected rate number: - " + stok.ToString());
                                    // check for unit - min or sec, sec is default
                                    stok.NextToken();
                                    if (stok.TokenType != '/')
                                    {
                                        stok.PushBack();
                                        ((TaskSequence)prevTask).SetRate((int)stok.NumberValue, false); // set rate per sec
                                    }
                                    else
                                    {
                                        stok.NextToken();
                                        if (stok.TokenType != StreamTokenizer.TT_WORD) throw new Exception("expected rate unit: 'min' or 'sec' - " + stok.ToString());
                                        string unit = stok.StringValue.ToLowerInvariant();
                                        if ("min".Equals(unit, StringComparison.Ordinal))
                                        {
                                            ((TaskSequence)prevTask).SetRate((int)stok.NumberValue, true); // set rate per min
                                        }
                                        else if ("sec".Equals(unit, StringComparison.Ordinal))
                                        {
                                            ((TaskSequence)prevTask).SetRate((int)stok.NumberValue, false); // set rate per sec
                                        }
                                        else
                                        {
                                            throw new Exception("expected rate unit: 'min' or 'sec' - " + stok.ToString());
                                        }
                                    }
                                }
                                colonOk = false;
                                break;

                            case '{':
                            case '[':
                                // a sequence
                                // check for sequence name
                                string name = null;
                                stok.NextToken();
                                if (stok.TokenType != '"')
                                {
                                    stok.PushBack();
                                }
                                else
                                {
                                    name = stok.StringValue;
                                    if (stok.TokenType != '"' || name == null || name.Length == 0)
                                    {
                                        throw new Exception("sequence name problem - " + stok.ToString());
                                    }
                                }
                                // start the sequence
                                TaskSequence seq2 = new TaskSequence(runData, name, currSequence, c == '[');
                                currSequence.AddTask(seq2);
                                currSequence = seq2;
                                colonOk = false;
                                break;

                            case '&':
                                if (currSequence.IsParallel)
                                {
                                    throw new Exception("Can only create background tasks within a serial task");
                                }
                                stok.NextToken();
                                int deltaPri;
                                if (stok.TokenType != StreamTokenizer.TT_NUMBER)
                                {
                                    stok.PushBack();
                                    deltaPri = 0;
                                }
                                else
                                {
                                    // priority
                                    deltaPri = (int)stok.NumberValue;
                                }

                                if (prevTask == null)
                                {
                                    throw new Exception("& was unexpected");
                                }
                                else if (prevTask.RunInBackground)
                                {
                                    throw new Exception("double & was unexpected");
                                }
                                else
                                {
                                    prevTask.SetRunInBackground(deltaPri);
                                }
                                break;

                            case '>':
                                currSequence.SetNoChildReport(); /* intentional fallthrough */
                                // end sequence
                                colonOk = true; prevTask = currSequence;
                                currSequence = currSequence.Parent;
                                break;
                            case '}':
                            case ']':
                                // end sequence
                                colonOk = true; prevTask = currSequence;
                                currSequence = currSequence.Parent;
                                break;

                            case '-':
                                isDisableCountNextTask = true;
                                break;

                        } //switch(c)
                        break;

                } //switch(stok.ttype)

            }

            if (sequence != currSequence)
            {
                throw new Exception("Unmatched sequences");
            }

            // remove redundant top level enclosing sequences
            while (sequence.IsCollapsable && sequence.Repetitions == 1 && sequence.GetRate() == 0)
            {
                IList<PerfTask> t = sequence.Tasks;
                if (t != null && t.Count == 1)
                {
                    PerfTask p = t[0];
                    if (p is TaskSequence)
                    {
                        sequence = (TaskSequence)p;
                        continue;
                    }
                }
                break;
            }
        }

        private string[] InitTasksPackages(Config config)
        {
            // LUCENENET specific - changing the logic a bit
            // to add all referenced assemblies by default.
            // The alt.tasks.packages parameter still exists, but
            // it is only necessary for assemblies that are not
            // referenced by the host assembly.

            HashSet<string> result = new HashSet<string>();
            string alts = config.Get("alt.tasks.packages", null);
            string dfltPkg = typeof(PerfTask).GetTypeInfo().Assembly.GetName().Name;
            string[] referencedAssemblies = AssemblyUtils.GetReferencedAssemblies().Select(a => a.GetName().Name).ToArray();
            result.Add(dfltPkg);

            if (alts == null)
            {
                result.UnionWith(referencedAssemblies);
                return result.ToArray();
            }

            foreach (string alt in alts.Split(',').TrimEnd())
            {
                result.Add(alt);
            }
            result.UnionWith(referencedAssemblies);
            return result.ToArray();
        }

        private Type TaskClass(Config config, string taskName)
        {
            foreach (string pkg in taskPackages)
            {
                Type result = LoadType(pkg, taskName + "Task");
                if (result != null)
                {
                    return result;
                }
            }
            // can only get here if failed to instantiate
            throw new TypeLoadException(taskName + " not found in packages " + Collections.ToString(taskPackages));
        }

        private Type LoadType(string assemblyName, string typeName)
        {
            return Assembly.Load(new AssemblyName(assemblyName)).GetTypes().FirstOrDefault(t => t.Name == typeName);
        }

        public override string ToString()
        {
            string newline = Environment.NewLine;
            StringBuilder sb = new StringBuilder();
            sb.Append(sequence.ToString());
            sb.Append(newline);
            return sb.ToString();
        }

        /// <summary>
        /// Execute this algorithm.
        /// </summary>
        public virtual void Execute()
        {
            try
            {
                sequence.RunAndMaybeStats(true);
            }
            finally
            {
                sequence.Dispose();
            }
        }

        /// <summary>
        /// Expert: for test purposes, return all tasks participating in this algorithm.
        /// </summary>
        /// <returns>All tasks participating in this algorithm.</returns>
        public virtual IList<PerfTask> ExtractTasks()
        {
            List<PerfTask> res = new List<PerfTask>();
            ExtractTasks(res, sequence);
            return res;
        }

        private void ExtractTasks(IList<PerfTask> extrct, TaskSequence seq)
        {
            if (seq == null)
                return;
            extrct.Add(seq);
            IList<PerfTask> t = sequence.Tasks;
            if (t == null)
                return;
            foreach (PerfTask p in t)
            {
                if (p is TaskSequence)
                {
                    ExtractTasks(extrct, (TaskSequence)p);
                }
                else
                {
                    extrct.Add(p);
                }
            }
        }
    }
}
