﻿using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.QueryParsers.Simple
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
    /// SimpleQueryParser is used to parse human readable query syntax.
    /// <p>
    /// The main idea behind this parser is that a person should be able to type
    /// whatever they want to represent a query, and this parser will do its best
    /// to interpret what to search for no matter how poorly composed the request
    /// may be. Tokens are considered to be any of a term, phrase, or subquery for the
    /// operations described below.  Whitespace including ' ' '\n' '\r' and '\t'
    /// and certain operators may be used to delimit tokens ( ) + | " .
    /// <p>
    /// Any errors in query syntax will be ignored and the parser will attempt
    /// to decipher what it can; however, this may mean odd or unexpected results.
    /// <h4>Query Operators</h4>
    /// <ul>
    ///  <li>'{@code +}' specifies {@code AND} operation: <tt>token1+token2</tt>
    ///  <li>'{@code |}' specifies {@code OR} operation: <tt>token1|token2</tt>
    ///  <li>'{@code -}' negates a single token: <tt>-token0</tt>
    ///  <li>'{@code "}' creates phrases of terms: <tt>"term1 term2 ..."</tt>
    ///  <li>'{@code *}' at the end of terms specifies prefix query: <tt>term*</tt>
    ///  <li>'{@code ~}N' at the end of terms specifies fuzzy query: <tt>term~1</tt>
    ///  <li>'{@code ~}N' at the end of phrases specifies near query: <tt>"term1 term2"~5</tt>
    ///  <li>'{@code (}' and '{@code )}' specifies precedence: <tt>token1 + (token2 | token3)</tt>
    /// </ul>
    /// <p>
    /// The {@link #setDefaultOperator default operator} is {@code OR} if no other operator is specified.
    /// For example, the following will {@code OR} {@code token1} and {@code token2} together:
    /// <tt>token1 token2</tt>
    /// <p>
    /// Normal operator precedence will be simple order from right to left.
    /// For example, the following will evaluate {@code token1 OR token2} first,
    /// then {@code AND} with {@code token3}:
    /// <blockquote>token1 | token2 + token3</blockquote>
    /// <h4>Escaping</h4>
    /// <p>
    /// An individual term may contain any possible character with certain characters
    /// requiring escaping using a '{@code \}'.  The following characters will need to be escaped in
    /// terms and phrases:
    /// {@code + | " ( ) ' \}
    /// <p>
    /// The '{@code -}' operator is a special case.  On individual terms (not phrases) the first
    /// character of a term that is {@code -} must be escaped; however, any '{@code -}' characters
    /// beyond the first character do not need to be escaped.
    /// For example:
    /// <ul>
    ///   <li>{@code -term1}   -- Specifies {@code NOT} operation against {@code term1}
    ///   <li>{@code \-term1}  -- Searches for the term {@code -term1}.
    ///   <li>{@code term-1}   -- Searches for the term {@code term-1}.
    ///   <li>{@code term\-1}  -- Searches for the term {@code term-1}.
    /// </ul>
    /// <p>
    /// The '{@code *}' operator is a special case. On individual terms (not phrases) the last
    /// character of a term that is '{@code *}' must be escaped; however, any '{@code *}' characters
    /// before the last character do not need to be escaped:
    /// <ul>
    ///   <li>{@code term1*}  --  Searches for the prefix {@code term1}
    ///   <li>{@code term1\*} --  Searches for the term {@code term1*}
    ///   <li>{@code term*1}  --  Searches for the term {@code term*1}
    ///   <li>{@code term\*1} --  Searches for the term {@code term*1}
    /// </ul>
    /// <p>
    /// Note that above examples consider the terms before text processing.
    /// </summary>
    public class SimpleQueryParser : QueryBuilder
    {
        /** Map of fields to query against with their weights */
        protected readonly IDictionary<string, float> weights;

        // TODO: Make these into a [Flags] enum in .NET??
        /** flags to the parser (to turn features on/off) */
        protected readonly int flags;

        /** Enables {@code AND} operator (+) */
        public static readonly int AND_OPERATOR         = 1<<0;
        /** Enables {@code NOT} operator (-) */
        public static readonly int NOT_OPERATOR         = 1<<1;
        /** Enables {@code OR} operator (|) */
        public static readonly int OR_OPERATOR          = 1<<2;
        /** Enables {@code PREFIX} operator (*) */
        public static readonly int PREFIX_OPERATOR      = 1<<3;
        /** Enables {@code PHRASE} operator (") */
        public static readonly int PHRASE_OPERATOR      = 1<<4;
        /** Enables {@code PRECEDENCE} operators: {@code (} and {@code )} */
        public static readonly int PRECEDENCE_OPERATORS = 1<<5;
        /** Enables {@code ESCAPE} operator (\) */
        public static readonly int ESCAPE_OPERATOR      = 1<<6;
        /** Enables {@code WHITESPACE} operators: ' ' '\n' '\r' '\t' */
        public static readonly int WHITESPACE_OPERATOR  = 1<<7;
        /** Enables {@code FUZZY} operators: (~) on single terms */
        public static readonly int FUZZY_OPERATOR       = 1<<8;
        /** Enables {@code NEAR} operators: (~) on phrases */
        public static readonly int NEAR_OPERATOR        = 1<<9;

        private BooleanClause.Occur defaultOperator = BooleanClause.Occur.SHOULD;

        /// <summary>
        /// Creates a new parser searching over a single field.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="field"></param>
        public SimpleQueryParser(Analyzer analyzer, string field)
            : this(analyzer, new HashMap<string, float>() { { field, 1.0F } })
        {
        }

        /// <summary>
        /// Creates a new parser searching over multiple fields with different weights.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="weights"></param>
        public SimpleQueryParser(Analyzer analyzer, IDictionary<string, float> weights)
            : this(analyzer, weights, -1)
        {
        }

        /// <summary>
        /// Creates a new parser with custom flags used to enable/disable certain features.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="weights"></param>
        /// <param name="flags"></param>
        public SimpleQueryParser(Analyzer analyzer, IDictionary<string, float> weights, int flags)
            : base(analyzer)
        {
            this.weights = weights;
            this.flags = flags;
        }

        /// <summary>
        /// Parses the query text and returns parsed query (or null if empty)
        /// </summary>
        /// <param name="queryText"></param>
        /// <returns></returns>
        public Query Parse(string queryText)
        {
            char[] data = queryText.ToCharArray();
            char[] buffer = new char[data.Length];

            State state = new State(data, buffer, 0, data.Length);
            ParseSubQuery(state);
            return state.Top;
        }

        private void ParseSubQuery(State state)
        {
            while (state.Index < state.Length)
            {
                if (state.Data[state.Index] == '(' && (flags & PRECEDENCE_OPERATORS) != 0)
                {
                    // the beginning of a subquery has been found
                    ConsumeSubQuery(state);
                }
                else if (state.Data[state.Index] == ')' && (flags & PRECEDENCE_OPERATORS) != 0)
                {
                    // this is an extraneous character so it is ignored
                    ++state.Index;
                }
                else if (state.Data[state.Index] == '"' && (flags & PHRASE_OPERATOR) != 0)
                {
                    // the beginning of a phrase has been found
                    ConsumePhrase(state);
                }
                else if (state.Data[state.Index] == '+' && (flags & AND_OPERATOR) != 0)
                {
                    // an and operation has been explicitly set
                    // if an operation has already been set this one is ignored
                    // if a term (or phrase or subquery) has not been found yet the
                    // operation is also ignored since there is no previous
                    // term (or phrase or subquery) to and with
                    if (!state.CurrentOperationIsSet && state.Top != null)
                    {
                        state.CurrentOperation = BooleanClause.Occur.MUST;
                    }

                    ++state.Index;
                }
                else if (state.Data[state.Index] == '|' && (flags & OR_OPERATOR) != 0)
                {
                    // an or operation has been explicitly set
                    // if an operation has already been set this one is ignored
                    // if a term (or phrase or subquery) has not been found yet the
                    // operation is also ignored since there is no previous
                    // term (or phrase or subquery) to or with
                    if (!state.CurrentOperationIsSet && state.Top != null)
                    {
                        state.CurrentOperation = BooleanClause.Occur.SHOULD;
                    }

                    ++state.Index;
                }
                else if (state.Data[state.Index] == '-' && (flags & NOT_OPERATOR) != 0)
                {
                    // a not operator has been found, so increase the not count
                    // two not operators in a row negate each other
                    ++state.Not;
                    ++state.Index;

                    // continue so the not operator is not reset
                    // before the next character is determined
                    continue;
                }
                else if ((state.Data[state.Index] == ' '
                  || state.Data[state.Index] == '\t'
                  || state.Data[state.Index] == '\n'
                  || state.Data[state.Index] == '\r') && (flags & WHITESPACE_OPERATOR) != 0)
                {
                    // ignore any whitespace found as it may have already been
                    // used a delimiter across a term (or phrase or subquery)
                    // or is simply extraneous
                    ++state.Index;
                }
                else
                {
                    // the beginning of a token has been found
                    ConsumeToken(state);
                }

                // reset the not operator as even whitespace is not allowed when
                // specifying the not operation for a term (or phrase or subquery)
                state.Not = 0;
            }
        }

        private void ConsumeSubQuery(State state)
        {
            Debug.Assert((flags & PRECEDENCE_OPERATORS) != 0);
            int start = ++state.Index;
            int precedence = 1;
            bool escaped = false;

            while (state.Index < state.Length)
            {
                if (!escaped)
                {
                    if (state.Data[state.Index] == '\\' && (flags & ESCAPE_OPERATOR) != 0)
                    {
                        // an escape character has been found so
                        // whatever character is next will become
                        // part of the subquery unless the escape
                        // character is the last one in the data
                        escaped = true;
                        ++state.Index;

                        continue;
                    }
                    else if (state.Data[state.Index] == '(')
                    {
                        // increase the precedence as there is a
                        // subquery in the current subquery
                        ++precedence;
                    }
                    else if (state.Data[state.Index] == ')')
                    {
                        --precedence;

                        if (precedence == 0)
                        {
                            // this should be the end of the subquery
                            // all characters found will used for
                            // creating the subquery
                            break;
                        }
                    }
                }

                escaped = false;
                ++state.Index;
            }

            if (state.Index == state.Length)
            {
                // a closing parenthesis was never found so the opening
                // parenthesis is considered extraneous and will be ignored
                state.Index = start;
            }
            else if (state.Index == start)
            {
                // a closing parenthesis was found immediately after the opening
                // parenthesis so the current operation is reset since it would
                // have been applied to this subquery
                state.CurrentOperationIsSet = false;

                ++state.Index;
            }
            else
            {
                // a complete subquery has been found and is recursively parsed by
                // starting over with a new state object
                State subState = new State(state.Data, state.Buffer, start, state.Index);
                ParseSubQuery(subState);
                BuildQueryTree(state, subState.Top);

                ++state.Index;
            }
        }

        private void ConsumePhrase(State state)
        {
            Debug.Assert((flags & PHRASE_OPERATOR) != 0);
            int start = ++state.Index;
            int copied = 0;
            bool escaped = false;
            bool hasSlop = false;

            while (state.Index < state.Length)
            {
                if (!escaped)
                {
                    if (state.Data[state.Index] == '\\' && (flags & ESCAPE_OPERATOR) != 0)
                    {
                        // an escape character has been found so
                        // whatever character is next will become
                        // part of the phrase unless the escape
                        // character is the last one in the data
                        escaped = true;
                        ++state.Index;

                        continue;
                    }
                    else if (state.Data[state.Index] == '"')
                    {
                        // if there are still characters after the closing ", check for a
                        // tilde
                        if (state.Length > (state.Index + 1) &&
                            state.Data[state.Index + 1] == '~' &&
                            (flags & NEAR_OPERATOR) != 0)
                        {
                            state.Index++;
                            // check for characters after the tilde
                            if (state.Length > (state.Index + 1))
                            {
                                hasSlop = true;
                            }
                            break;
                        }
                        else
                        {
                            // this should be the end of the phrase
                            // all characters found will used for
                            // creating the phrase query
                            break;
                        }
                    }
                }

                escaped = false;
                state.Buffer[copied++] = state.Data[state.Index++];
            }

            if (state.Index == state.Length)
            {
                // a closing double quote was never found so the opening
                // double quote is considered extraneous and will be ignored
                state.Index = start;
            }
            else if (state.Index == start)
            {
                // a closing double quote was found immediately after the opening
                // double quote so the current operation is reset since it would
                // have been applied to this phrase
                state.CurrentOperationIsSet = false;

                ++state.Index;
            }
            else
            {
                // a complete phrase has been found and is parsed through
                // through the analyzer from the given field
                string phrase = new string(state.Buffer, 0, copied);
                Query branch;
                if (hasSlop)
                {
                    branch = NewPhraseQuery(phrase, ParseFuzziness(state));
                }
                else
                {
                    branch = NewPhraseQuery(phrase, 0);
                }
                BuildQueryTree(state, branch);

                ++state.Index;
            }
        }

        private void ConsumeToken(State state)
        {
            int copied = 0;
            bool escaped = false;
            bool prefix = false;
            bool fuzzy = false;

            while (state.Index < state.Length)
            {
                if (!escaped)
                {
                    if (state.Data[state.Index] == '\\' && (flags & ESCAPE_OPERATOR) != 0)
                    {
                        // an escape character has been found so
                        // whatever character is next will become
                        // part of the term unless the escape
                        // character is the last one in the data
                        escaped = true;
                        prefix = false;
                        ++state.Index;

                        continue;
                    }
                    else if (TokenFinished(state))
                    {
                        // this should be the end of the term
                        // all characters found will used for
                        // creating the term query
                        break;
                    }
                    else if (copied > 0 && state.Data[state.Index] == '~' && (flags & FUZZY_OPERATOR) != 0)
                    {
                        fuzzy = true;
                        break;
                    }

                    // wildcard tracks whether or not the last character
                    // was a '*' operator that hasn't been escaped
                    // there must be at least one valid character before
                    // searching for a prefixed set of terms
                    prefix = copied > 0 && state.Data[state.Index] == '*' && (flags & PREFIX_OPERATOR) != 0;
                }

                escaped = false;
                state.Buffer[copied++] = state.Data[state.Index++];
            }

            if (copied > 0)
            {
                Query branch;

                if (fuzzy && (flags & FUZZY_OPERATOR) != 0)
                {
                    string token = new string(state.Buffer, 0, copied);
                    int fuzziness = ParseFuzziness(state);
                    // edit distance has a maximum, limit to the maximum supported
                    fuzziness = Math.Min(fuzziness, LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
                    if (fuzziness == 0)
                    {
                        branch = NewDefaultQuery(token);
                    }
                    else
                    {
                        branch = NewFuzzyQuery(token, fuzziness);
                    }
                }
                else if (prefix)
                {
                    // if a term is found with a closing '*' it is considered to be a prefix query
                    // and will have prefix added as an option
                    string token = new string(state.Buffer, 0, copied - 1);
                    branch = NewPrefixQuery(token);
                }
                else
                {
                    // a standard term has been found so it will be run through
                    // the entire analysis chain from the specified schema field
                    string token = new string(state.Buffer, 0, copied);
                    branch = NewDefaultQuery(token);
                }

                BuildQueryTree(state, branch);
            }
        }

        /// <summary>
        /// buildQueryTree should be called after a term, phrase, or subquery
        /// is consumed to be added to our existing query tree
        /// this method will only add to the existing tree if the branch contained in state is not null
        /// </summary>
        /// <param name="state"></param>
        /// <param name="branch"></param>
        private void BuildQueryTree(State state, Query branch)
        {
            if (branch != null)
            {
                // modify our branch to a BooleanQuery wrapper for not
                // this is necessary any time a term, phrase, or subquery is negated
                if (state.Not % 2 == 1)
                {
                    BooleanQuery nq = new BooleanQuery();
                    nq.Add(branch, BooleanClause.Occur.MUST_NOT);
                    nq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
                    branch = nq;
                }

                // first term (or phrase or subquery) found and will begin our query tree
                if (state.Top == null)
                {
                    state.Top = branch;
                }
                else
                {
                    // more than one term (or phrase or subquery) found
                    // set currentOperation to the default if no other operation is explicitly set
                    if (!state.CurrentOperationIsSet)
                    {
                        state.CurrentOperation = defaultOperator;
                    }

                    // operational change requiring a new parent node
                    // this occurs if the previous operation is not the same as current operation
                    // because the previous operation must be evaluated separately to preserve
                    // the proper precedence and the current operation will take over as the top of the tree
                    if (!state.PreviousOperationIsSet || state.PreviousOperation != state.CurrentOperation)
                    {
                        BooleanQuery bq = new BooleanQuery();
                        bq.Add(state.Top, state.CurrentOperation);
                        state.Top = bq;
                    }

                    // reset all of the state for reuse
                    ((BooleanQuery)state.Top).Add(branch, state.CurrentOperation);
                    state.PreviousOperation = state.CurrentOperation;
                }

                // reset the current operation as it was intended to be applied to
                // the incoming term (or phrase or subquery) even if branch was null
                // due to other possible errors
                state.CurrentOperationIsSet = false;
            }
        }

        /// <summary>
        /// Helper parsing fuzziness from parsing state
        /// </summary>
        /// <param name="state"></param>
        /// <returns>slop/edit distance, 0 in the case of non-parsing slop/edit string</returns>
        private int ParseFuzziness(State state)
        {
            char[] slopText = new char[state.Length];
            int slopLength = 0;

            if (state.Data[state.Index] == '~')
            {
                while (state.Index < state.Length)
                {
                    state.Index++;
                    // it's possible that the ~ was at the end, so check after incrementing
                    // to make sure we don't go out of bounds
                    if (state.Index < state.Length)
                    {
                        if (TokenFinished(state))
                        {
                            break;
                        }
                        slopText[slopLength] = state.Data[state.Index];
                        slopLength++;
                    }
                }
                int fuzziness = 0;
                int.TryParse(new string(slopText, 0, slopLength), out fuzziness);
                // negative -> 0
                if (fuzziness < 0)
                {
                    fuzziness = 0;
                }
                return fuzziness;
            }
            return 0;
        }

        /// <summary>
        /// Helper returning true if the state has reached the end of token.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool TokenFinished(State state)
        {
            if ((state.Data[state.Index] == '"' && (flags & PHRASE_OPERATOR) != 0)
                || (state.Data[state.Index] == '|' && (flags & OR_OPERATOR) != 0)
                || (state.Data[state.Index] == '+' && (flags & AND_OPERATOR) != 0)
                || (state.Data[state.Index] == '(' && (flags & PRECEDENCE_OPERATORS) != 0)
                || (state.Data[state.Index] == ')' && (flags & PRECEDENCE_OPERATORS) != 0)
                || ((state.Data[state.Index] == ' '
                || state.Data[state.Index] == '\t'
                || state.Data[state.Index] == '\n'
                || state.Data[state.Index] == '\r') && (flags & WHITESPACE_OPERATOR) != 0))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Factory method to generate a standard query (no phrase or prefix operators).
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected virtual Query NewDefaultQuery(string text)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in weights)
            {
                Query q = CreateBooleanQuery(entry.Key, text, defaultOperator);
                if (q != null)
                {
                    q.Boost = entry.Value;
                    bq.Add(q, BooleanClause.Occur.SHOULD);
                }
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Factory method to generate a fuzzy query.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fuzziness"></param>
        /// <returns></returns>
        protected virtual Query NewFuzzyQuery(string text, int fuzziness)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in weights)
            {
                Query q = new FuzzyQuery(new Term(entry.Key, text), fuzziness);
                if (q != null)
                {
                    q.Boost = entry.Value;
                    bq.Add(q, BooleanClause.Occur.SHOULD);
                }
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Factory method to generate a phrase query with slop.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="slop"></param>
        /// <returns></returns>
        protected virtual Query NewPhraseQuery(string text, int slop)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in weights)
            {
                Query q = CreatePhraseQuery(entry.Key, text, slop);
                if (q != null)
                {
                    q.Boost = entry.Value;
                    bq.Add(q, BooleanClause.Occur.SHOULD);
                }
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Factory method to generate a prefix query.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected virtual Query NewPrefixQuery(string text)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in weights)
            {
                PrefixQuery prefix = new PrefixQuery(new Term(entry.Key, text));
                prefix.Boost = entry.Value;
                bq.Add(prefix, BooleanClause.Occur.SHOULD);
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Helper to simplify boolean queries with 0 or 1 clause
        /// </summary>
        /// <param name="bq"></param>
        /// <returns></returns>
        protected virtual Query Simplify(BooleanQuery bq)
        {
            if (!bq.Clauses.Any())
            {
                return null;
            }
            else if (bq.Clauses.Length == 1)
            {
                return bq.Clauses[0].Query;
            }
            else
            {
                return bq;
            }
        }

        /// <summary>
        /// Gets or Sets the implicit operator setting, which will be
        /// either {@code SHOULD} or {@code MUST}.
        /// </summary>
        public virtual BooleanClause.Occur DefaultOperator
        {
            get 
            { 
                return defaultOperator; 
            }
            set 
            {
                if (value != BooleanClause.Occur.SHOULD && value != BooleanClause.Occur.MUST)
                {
                    throw new ArgumentException("invalid operator: only SHOULD or MUST are allowed");
                }
                defaultOperator = value; 
            }
        }


        public class State
        {
            //private readonly char[] data;   // the characters in the query string
            //private readonly char[] buffer; // a temporary buffer used to reduce necessary allocations
            //private int index;
            //private int length;

            private BooleanClause.Occur currentOperation;
            private BooleanClause.Occur previousOperation;
            //private int not;

            //private Query top;

            internal State(char[] data, char[] buffer, int index, int length)
            {
                this.Data = data;
                this.Buffer = buffer;
                this.Index = index;
                this.Length = length;
            }

            public char[] Data { get; protected set; } // the characters in the query string
            public char[] Buffer { get; protected set; } // a temporary buffer used to reduce necessary allocations
            public int Index { get; set; }
            public int Length { get; protected set; }

            public BooleanClause.Occur CurrentOperation 
            {
                get 
                { 
                    return currentOperation; 
                }
                set
                {
                    currentOperation = value;
                    CurrentOperationIsSet = true;
                }
            }

            public BooleanClause.Occur PreviousOperation
            {
                get
                {
                    return previousOperation;
                }
                set
                {
                    previousOperation = value;
                    PreviousOperationIsSet = true;
                }
            }

            public bool CurrentOperationIsSet { get; set; }
            public bool PreviousOperationIsSet { get; set; }

            public int Not { get; set; }
            public Query Top { get; set; }
        }
    }
}
