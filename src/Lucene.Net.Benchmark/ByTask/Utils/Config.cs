﻿using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// Perf run configuration properties.
    /// </summary>
    /// <remarks>
    /// Numeric property containing ":", e.g. "10:100:5" is interpreted
    /// as array of numeric values. It is extracted once, on first use, and
    /// maintain a round number to return the appropriate value.
    /// <para/>
    /// The config property "work.dir" tells where is the root of
    /// docs data dirs and indexes dirs. It is set to either of:
    /// <list type="bullet">
    ///     <item><description>value supplied for it in the alg file;</description></item>
    ///     <item><description>otherwise, value of environment variable "benchmark.work.dir";</description></item>
    ///     <item><description>otherwise, "work".</description></item>
    /// </list>
    /// </remarks>
    public class Config
    {
        // For tests, if verbose is not turned on, don't print the props.
        private static readonly bool DEFAULT_PRINT_PROPS = SystemProperties.GetPropertyAsBoolean("tests.verbose", true);
        private static readonly string NEW_LINE = Environment.NewLine;

        private int roundNumber = 0;
        private IDictionary<string, string> props;
        private IDictionary<string, object> valByRound = new Dictionary<string, object>();
        private IDictionary<string, string> colForValByRound = new Dictionary<string, string>();
        private string algorithmText;

        /// <summary>
        /// Read both algorithm and config properties.
        /// </summary>
        /// <param name="algReader">From where to read algorithm and config properties.</param>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        public Config(TextReader algReader)
        {
            // read alg file to array of lines
            IList<string> lines = new List<string>();
            int lastConfigLine = 0;
            string line;
            while ((line = algReader.ReadLine()) != null)
            {
                lines.Add(line);
                if (line.IndexOf('=') > 0)
                {
                    lastConfigLine = lines.Count;
                }
            }
            algReader.Dispose();
            // copy props lines to string
            MemoryStream ms = new MemoryStream();
            TextWriter writer = new StreamWriter(ms);
            for (int i = 0; i < lastConfigLine; i++)
            {
                writer.WriteLine(lines[i]);
            }
            // read props from string
            this.props = new Dictionary<string, string>();
            writer.Flush();
            ms.Position = 0;
            props.Load(ms); 

            // make sure work dir is set properly 
            string temp;
            if (!props.TryGetValue("work.dir", out temp) || temp == null)
            {
                props["work.dir"] = SystemProperties.GetProperty("benchmark.work.dir", "work");
            }

            if (props.TryGetValue("print.props", out temp))
            {
                if (temp.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    PrintProps();
                }
            }
            else if (DEFAULT_PRINT_PROPS)
            {
                PrintProps();
            }

            // copy algorithm lines
            var sb = new StringBuilder();
            for (int i = lastConfigLine; i < lines.Count; i++)
            {
                sb.Append(lines[i]);
                sb.Append(NEW_LINE);
            }
            algorithmText = sb.ToString();
        }

        /// <summary>
        /// Create config without algorithm - useful for a programmatic perf test.
        /// </summary>
        /// <param name="props">Configuration properties.</param>
        public Config(IDictionary<string, string> props)
        {
            this.props = props;
            string temp;
            if (props.TryGetValue("print.props", out temp))
            {
                if (temp.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    PrintProps();
                }
            }
            else if (DEFAULT_PRINT_PROPS)
            {
                PrintProps();
            }
        }

        private void PrintProps()
        {
            SystemConsole.WriteLine("------------> config properties:");
            List<string> propKeys = new List<string>(props.Keys);
            propKeys.Sort();
            foreach (string propName in propKeys)
            {
                SystemConsole.WriteLine(propName + " = " + props[propName]);
            }
            SystemConsole.WriteLine("-------------------------------");
        }

        /// <summary>
        /// Return a string property.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <param name="dflt">Default value.</param>
        /// <returns>A string property.</returns>
        public virtual string Get(string name, string dflt)
        {
            string[] vals;
            object temp;
            if (valByRound.TryGetValue(name, out temp) && temp != null)
            {
                vals = (string[])temp;
                return vals[roundNumber % vals.Length];
            }
            // done if not by round
            string sval;
            if (!props.TryGetValue(name, out sval))
            {
                sval = dflt;
            }
            if (sval == null)
            {
                return null;
            }
            if (sval.IndexOf(":") < 0)
            {
                return sval;
            }
            else if (sval.IndexOf(":\\") >= 0 || sval.IndexOf(":/") >= 0)
            {
                // this previously messed up absolute path names on Windows. Assuming
                // there is no real value that starts with \ or /
                return sval;
            }
            // first time this prop is extracted by round
            int k = sval.IndexOf(":");
            string colName = sval.Substring(0, k - 0);
            sval = sval.Substring(k + 1);
            colForValByRound[name] = colName;
            vals = PropToStringArray(sval);
            valByRound[name] = vals;
            return vals[roundNumber % vals.Length];
        }

        /// <summary>
        /// Set a property.
        /// <para/>
        /// Note: once a multiple values property is set, it can no longer be modified.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <param name="value">Either single or multiple property value (multiple values are separated by ":")</param>
        public virtual void Set(string name, string value)
        {
            object temp;
            if (valByRound.TryGetValue(name, out temp) && temp != null)
            {
                throw new Exception("Cannot modify a multi value property!");
            }
            props[name] = value;
        }

        /// <summary>
        /// Return an <see cref="int"/> property.
        /// <para/>
        /// If the property contain ":", e.g. "10:100:5", it is interpreted
        /// as array of ints. It is extracted once, on first call
        /// to Get() it, and a by-round-value is returned.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <param name="dflt">Default value.</param>
        /// <returns>An <see cref="int"/> property.</returns>
        public virtual int Get(string name, int dflt)
        {
            // use value by round if already parsed
            int[] vals;
            object temp;
            if (valByRound.TryGetValue(name, out temp) && temp != null)
            {
                vals = (int[])temp;
                return vals[roundNumber % vals.Length];
            }
            // done if not by round 
            string sval;
            if (!props.TryGetValue(name, out sval))
            {
                sval = "" + dflt;
            }
            if (sval.IndexOf(":") < 0)
            {
                return int.Parse(sval, CultureInfo.InvariantCulture);
            }
            // first time this prop is extracted by round
            int k = sval.IndexOf(":");
            string colName = sval.Substring(0, k - 0);
            sval = sval.Substring(k + 1);
            colForValByRound[name] = colName;
            vals = PropToInt32Array(sval);
            valByRound[name] = vals;
            return vals[roundNumber % vals.Length];
        }

        /// <summary>
        /// Return a double property.
        /// <para/>
        /// If the property contain ":", e.g. "10:100:5", it is interpreted
        /// as array of doubles. It is extracted once, on first call
        /// to Get() it, and a by-round-value is returned.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <param name="dflt">Default value.</param>
        /// <returns>A double property.</returns>
        public virtual double Get(string name, double dflt)
        {
            // use value by round if already parsed
            double[] vals;
            object temp;
            if (valByRound.TryGetValue(name, out temp) && temp != null)
            {
                vals = (double[])temp;
                return vals[roundNumber % vals.Length];
            }
            // done if not by round 
            string sval;
            if (!props.TryGetValue(name, out sval))
            {
                sval = "" + dflt;
            }
            if (sval.IndexOf(":") < 0)
            {
                return double.Parse(sval, CultureInfo.InvariantCulture);
            }
            // first time this prop is extracted by round
            int k = sval.IndexOf(":");
            string colName = sval.Substring(0, k - 0);
            sval = sval.Substring(k + 1);
            colForValByRound[name] = colName;
            vals = PropToDoubleArray(sval);
            valByRound[name] = vals;
            return vals[roundNumber % vals.Length];
        }

        /// <summary>
        /// Return a boolean property.
        /// If the property contain ":", e.g. "true.true.false", it is interpreted
        /// as array of booleans. It is extracted once, on first call
        /// to Get() it, and a by-round-value is returned.
        /// </summary>
        /// <param name="name">Name of property.</param>
        /// <param name="dflt">Default value.</param>
        /// <returns>A <see cref="bool"/> property.</returns>
        public virtual bool Get(string name, bool dflt)
        {
            // use value by round if already parsed
            bool[] vals;
            object temp;
            if (valByRound.TryGetValue(name, out temp) && temp != null)
            {
                vals = (bool[])temp;
                return vals[roundNumber % vals.Length];
            }
            // done if not by round 
            string sval;
            if (!props.TryGetValue(name, out sval))
            {
                sval = "" + dflt;
            }
            if (sval.IndexOf(":") < 0)
            {
                return bool.Parse(sval);
            }
            // first time this prop is extracted by round 
            int k = sval.IndexOf(":");
            string colName = sval.Substring(0, k - 0);
            sval = sval.Substring(k + 1);
            colForValByRound[name] = colName;
            vals = PropToBooleanArray(sval);
            valByRound[name] = vals;
            return vals[roundNumber % vals.Length];
        }

        /// <summary>
        /// Increment the round number, for config values that are extracted by round number.
        /// </summary>
        /// <returns>The new round number.</returns>
        public virtual int NewRound()
        {
            roundNumber++;

            StringBuilder sb = new StringBuilder("--> Round ").Append(roundNumber - 1).Append("-->").Append(roundNumber);

            // log changes in values
            if (valByRound.Count > 0)
            {
                sb.Append(": ");
                foreach (string name in valByRound.Keys)
                {
                    object a = valByRound[name];
                    if (a is int[])
                    {
                        int[] ai = (int[])a;
                        int n1 = (roundNumber - 1) % ai.Length;
                        int n2 = roundNumber % ai.Length;
                        sb.Append("  ").Append(name).Append(":").Append(ai[n1]).Append("-->").Append(ai[n2]);
                    }
                    else if (a is double[])
                    {
                        double[] ad = (double[])a;
                        int n1 = (roundNumber - 1) % ad.Length;
                        int n2 = roundNumber % ad.Length;
                        sb.Append("  ").Append(name).Append(":").Append(ad[n1]).Append("-->").Append(ad[n2]);
                    }
                    else if (a is string[])
                    {
                        string[] ad = (string[])a;
                        int n1 = (roundNumber - 1) % ad.Length;
                        int n2 = roundNumber % ad.Length;
                        sb.Append("  ").Append(name).Append(":").Append(ad[n1]).Append("-->").Append(ad[n2]);
                    }
                    else
                    {
                        bool[] ab = (bool[])a;
                        int n1 = (roundNumber - 1) % ab.Length;
                        int n2 = roundNumber % ab.Length;
                        sb.Append("  ").Append(name).Append(":").Append(ab[n1]).Append("-->").Append(ab[n2]);
                    }
                }
            }

            SystemConsole.WriteLine();
            SystemConsole.WriteLine(sb.ToString());
            SystemConsole.WriteLine();

            return roundNumber;
        }

        private string[] PropToStringArray(string s)
        {
            if (s.IndexOf(":") < 0)
            {
                return new string[] { s };
            }

            List<string> a = new List<string>();
            StringTokenizer st = new StringTokenizer(s, ":");
            while (st.HasMoreTokens())
            {
                string t = st.NextToken();
                a.Add(t);
            }
            return a.ToArray();
        }

        // extract properties to array, e.g. for "10:100:5" return int[]{10,100,5}. 
        private int[] PropToInt32Array(string s)
        {
            if (s.IndexOf(":") < 0)
            {
                return new int[] { int.Parse(s, CultureInfo.InvariantCulture) };
            }

            List<int> a = new List<int>();
            StringTokenizer st = new StringTokenizer(s, ":");
            while (st.HasMoreTokens())
            {
                string t = st.NextToken();
                a.Add(int.Parse(t, CultureInfo.InvariantCulture));
            }
            int[] res = new int[a.Count];
            for (int i = 0; i < a.Count; i++)
            {
                res[i] = a[i];
            }
            return res;
        }

        // extract properties to array, e.g. for "10.7:100.4:-2.3" return int[]{10.7,100.4,-2.3}. 
        private double[] PropToDoubleArray(string s)
        {
            if (s.IndexOf(":") < 0)
            {
                return new double[] { double.Parse(s, CultureInfo.InvariantCulture) };
            }

            List<double> a = new List<double>();
            StringTokenizer st = new StringTokenizer(s, ":");
            while (st.HasMoreTokens())
            {
                string t = st.NextToken();
                a.Add(double.Parse(t, CultureInfo.InvariantCulture));
            }
            double[] res = new double[a.Count];
            for (int i = 0; i < a.Count; i++)
            {
                res[i] = a[i];
            }
            return res;
        }

        // extract properties to array, e.g. for "true:true:false" return boolean[]{true,false,false}. 
        private bool[] PropToBooleanArray(string s)
        {
            if (s.IndexOf(":") < 0)
            {
                return new bool[] { bool.Parse(s) };
            }

            List<bool> a = new List<bool>();
            StringTokenizer st = new StringTokenizer(s, ":");
            while (st.HasMoreTokens())
            {
                string t = st.NextToken();
                a.Add(bool.Parse(t));
            }
            bool[] res = new bool[a.Count];
            for (int i = 0; i < a.Count; i++)
            {
                res[i] = a[i];
            }
            return res;
        }

        /// <summary>
        /// Gets names of params set by round, for reports title.
        /// </summary>
        public virtual string GetColsNamesForValsByRound()
        {
            if (colForValByRound.Count == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            foreach (string name in colForValByRound.Keys)
            {
                string colName = colForValByRound[name];
                sb.Append(" ").Append(colName);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets values of params set by round, for reports lines.
        /// </summary>
        public virtual string GetColsValuesForValsByRound(int roundNum)
        {
            if (colForValByRound.Count == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            foreach (string name in colForValByRound.Keys)
            {
                string colName = colForValByRound[name];
                string template = " " + colName;
                if (roundNum < 0)
                {
                    // just append blanks
                    sb.Append(Formatter.FormatPaddLeft("-", template));
                }
                else
                {
                    // append actual values, for that round
                    object a;
                    valByRound.TryGetValue(name, out a);
                    if (a is int[])
                    {
                        int[] ai = (int[])a;
                        int n = roundNum % ai.Length;
                        sb.Append(Formatter.Format(ai[n], template));
                    }
                    else if (a is double[])
                    {
                        double[] ad = (double[])a;
                        int n = roundNum % ad.Length;
                        sb.Append(Formatter.Format(2, ad[n], template));
                    }
                    else if (a is string[])
                    {
                        string[] ad = (string[])a;
                        int n = roundNum % ad.Length;
                        sb.Append(ad[n]);
                    }
                    else
                    {
                        bool[] ab = (bool[])a;
                        int n = roundNum % ab.Length;
                        sb.Append(Formatter.FormatPaddLeft("" + ab[n], template));
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the round number.
        /// </summary>
        public virtual int RoundNumber
        {
            get { return roundNumber; }
        }

        /// <summary>
        /// Gets the algorithmText.
        /// </summary>
        public virtual string AlgorithmText
        {
            get { return algorithmText; }
        }
    }
}
