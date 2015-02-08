using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Analysis
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;

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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;

    [TestFixture]
    public class TestGraphTokenizers : BaseTokenStreamTestCase
    {
        // Makes a graph TokenStream from the string; separate
        // positions with single space, multiple tokens at the same
        // position with /, and add optional position length with
        // :.  EG "a b c" is a simple chain, "a/x b c" adds 'x'
        // over 'a' at position 0 with posLen=1, "a/x:3 b c" adds
        // 'x' over a with posLen=3.  Tokens are in normal-form!
        // So, offsets are computed based on the first token at a
        // given position.  NOTE: each token must be a single
        // character!  We assume this when computing offsets...

        // NOTE: all input tokens must be length 1!!!  this means
        // you cannot turn on MockCharFilter when random
        // testing...

        private class GraphTokenizer : Tokenizer
        {
            internal IList<Token> Tokens;
            internal int Upto;
            internal int InputLength;

            internal readonly ICharTermAttribute TermAtt;
            internal readonly IOffsetAttribute OffsetAtt;
            internal readonly IPositionIncrementAttribute PosIncrAtt;
            internal readonly IPositionLengthAttribute PosLengthAtt;

            public GraphTokenizer(TextReader input)
                : base(input)
            {
                TermAtt = AddAttribute<ICharTermAttribute>();
                OffsetAtt = AddAttribute<IOffsetAttribute>();
                PosIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                PosLengthAtt = AddAttribute<IPositionLengthAttribute>();
            }

            public override void Reset()
            {
                base.Reset();
                Tokens = null;
                Upto = 0;
            }

            public override bool IncrementToken()
            {
                if (Tokens == null)
                {
                    FillTokens();
                }
                //System.out.println("graphTokenizer: incr upto=" + upto + " vs " + tokens.size());
                if (Upto == Tokens.Count)
                {
                    //System.out.println("  END @ " + tokens.size());
                    return false;
                }
                Token t = Tokens[Upto++];
                //System.out.println("  return token=" + t);
                ClearAttributes();
                TermAtt.Append(t.ToString());
                OffsetAtt.SetOffset(t.StartOffset(), t.EndOffset());
                PosIncrAtt.PositionIncrement = t.PositionIncrement;
                PosLengthAtt.PositionLength = t.PositionLength;
                return true;
            }

            public override void End()
            {
                base.End();
                // NOTE: somewhat... hackish, but we need this to
                // satisfy BTSTC:
                int lastOffset;
                if (Tokens != null && Tokens.Count > 0)
                {
                    lastOffset = Tokens[Tokens.Count - 1].EndOffset();
                }
                else
                {
                    lastOffset = 0;
                }
                OffsetAtt.SetOffset(CorrectOffset(lastOffset), CorrectOffset(InputLength));
            }

            internal virtual void FillTokens()
            {
                StringBuilder sb = new StringBuilder();
                char[] buffer = new char[256];
                while (true)
                {
                    int count = input.Read(buffer, 0, buffer.Length);

                    //.NET TextReader.Read(buff, int, int) returns 0, not -1 on no chars
                    // but in some cases, such as MockCharFilter, it overloads read and returns -1
                    // so we should handle both 0 and -1 values
                    if (count <= 0)
                    {
                        break;
                    }
                    sb.Append(buffer, 0, count);
                    //System.out.println("got count=" + count);
                }
                //System.out.println("fillTokens: " + sb);

                InputLength = sb.Length;

                string[] parts = sb.ToString().Split(' ');

                Tokens = new List<Token>();
                int pos = 0;
                int maxPos = -1;
                int offset = 0;
                //System.out.println("again");
                foreach (string part in parts)
                {
                    string[] overlapped = part.Split('/');
                    bool firstAtPos = true;
                    int minPosLength = int.MaxValue;
                    foreach (string part2 in overlapped)
                    {
                        int colonIndex = part2.IndexOf(':');
                        string token;
                        int posLength;
                        if (colonIndex != -1)
                        {
                            token = part2.Substring(0, colonIndex);
                            posLength = Convert.ToInt32(part2.Substring(1 + colonIndex));
                        }
                        else
                        {
                            token = part2;
                            posLength = 1;
                        }
                        maxPos = Math.Max(maxPos, pos + posLength);
                        minPosLength = Math.Min(minPosLength, posLength);
                        Token t = new Token(token, offset, offset + 2 * posLength - 1);
                        t.PositionLength = posLength;
                        t.PositionIncrement = firstAtPos ? 1 : 0;
                        firstAtPos = false;
                        //System.out.println("  add token=" + t + " startOff=" + t.StartOffset() + " endOff=" + t.EndOffset());
                        Tokens.Add(t);
                    }
                    pos += minPosLength;
                    offset = 2 * pos;
                }
                Debug.Assert(maxPos <= pos, "input string mal-formed: posLength>1 tokens hang over the end");
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterBasic()
        {
            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);

                CheckAnalysisConsistency(Random(), a, false, "a b c d e f g h i j k");
            }
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t2 = new MockGraphTokenFilter(Random(), t);
                return new TokenStreamComponents(t, t2);
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterOnGraphInput()
        {
            for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);

                CheckAnalysisConsistency(Random(), a, false, "a/x:3 c/y:2 d e f/z:4 g h i j k");
            }
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new GraphTokenizer(reader);
                TokenStream t2 = new MockGraphTokenFilter(Random(), t);
                return new TokenStreamComponents(t, t2);
            }
        }

        // Just deletes (leaving hole) token 'a':
        private sealed class RemoveATokens : TokenFilter
        {
            internal int PendingPosInc;

            internal readonly ICharTermAttribute TermAtt;// = addAttribute(typeof(CharTermAttribute));
            internal readonly IPositionIncrementAttribute PosIncAtt;// = addAttribute(typeof(PositionIncrementAttribute));

            public RemoveATokens(TokenStream @in)
                : base(@in)
            {
                TermAtt = AddAttribute<ICharTermAttribute>();
                PosIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override void Reset()
            {
                base.Reset();
                PendingPosInc = 0;
            }

            public override void End()
            {
                base.End();
                PosIncAtt.PositionIncrement = PendingPosInc + PosIncAtt.PositionIncrement;
            }

            public override bool IncrementToken()
            {
                while (true)
                {
                    bool gotOne = input.IncrementToken();
                    if (!gotOne)
                    {
                        return false;
                    }
                    else if (TermAtt.ToString().Equals("a"))
                    {
                        PendingPosInc += PosIncAtt.PositionIncrement;
                    }
                    else
                    {
                        PosIncAtt.PositionIncrement = PendingPosInc + PosIncAtt.PositionIncrement;
                        PendingPosInc = 0;
                        return true;
                    }
                }
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterBeforeHoles()
        {
            for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new MGTFBHAnalyzerAnonymousInnerClassHelper(this);

                Random random = Random();
                CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k");
                CheckAnalysisConsistency(random, a, false, "x y a b c d e f g h i j k");
                CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a");
                CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a x y");
            }
        }

        private class MGTFBHAnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public MGTFBHAnalyzerAnonymousInnerClassHelper(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t2 = new MockGraphTokenFilter(Random(), t);
                TokenStream t3 = new RemoveATokens(t2);
                return new TokenStreamComponents(t, t3);
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterAfterHoles()
        {
            for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new MGTFAHAnalyzerAnonymousInnerClassHelper2(this);

                Random random = Random();
                CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k");
                CheckAnalysisConsistency(random, a, false, "x y a b c d e f g h i j k");
                CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a");
                CheckAnalysisConsistency(random, a, false, "a b c d e f g h i j k a x y");
            }
        }

        private class MGTFAHAnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public MGTFAHAnalyzerAnonymousInnerClassHelper2(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t2 = new RemoveATokens(t);
                TokenStream t3 = new MockGraphTokenFilter(Random(), t2);
                return new TokenStreamComponents(t, t3);
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterRandom()
        {
            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);

                Random random = Random();
                CheckRandomData(random, a, 5, AtLeast(100));
            }
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public AnalyzerAnonymousInnerClassHelper3(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t2 = new MockGraphTokenFilter(Random(), t);
                return new TokenStreamComponents(t, t2);
            }
        }

        // Two MockGraphTokenFilters
        [Test]
        public virtual void TestDoubleMockGraphTokenFilterRandom()
        {
            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new AnalyzerAnonymousInnerClassHelper4(this);

                Random random = Random();
                CheckRandomData(random, a, 5, AtLeast(100));
            }
        }

        [Test]
        public void TestMockTokenizerCtor()
        {
            var sr = new StringReader("Hello");
            var mt = new MockTokenizer(sr);
        }

        private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public AnalyzerAnonymousInnerClassHelper4(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t1 = new MockGraphTokenFilter(Random(), t);
                TokenStream t2 = new MockGraphTokenFilter(Random(), t1);
                return new TokenStreamComponents(t, t2);
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterBeforeHolesRandom()
        {
            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new AnalyzerAnonymousInnerClassHelper5(this);

                Random random = Random();
                CheckRandomData(random, a, 5, AtLeast(100));
            }
        }

        private class AnalyzerAnonymousInnerClassHelper5 : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public AnalyzerAnonymousInnerClassHelper5(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t1 = new MockGraphTokenFilter(Random(), t);
                TokenStream t2 = new MockHoleInjectingTokenFilter(Random(), t1);
                return new TokenStreamComponents(t, t2);
            }
        }

        [Test]
        public virtual void TestMockGraphTokenFilterAfterHolesRandom()
        {
            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }

                // Make new analyzer each time, because MGTF has fixed
                // seed:
                Analyzer a = new AnalyzerAnonymousInnerClassHelper6(this);

                Random random = Random();
                CheckRandomData(random, a, 5, AtLeast(100));
            }
        }

        private class AnalyzerAnonymousInnerClassHelper6 : Analyzer
        {
            private readonly TestGraphTokenizers OuterInstance;

            public AnalyzerAnonymousInnerClassHelper6(TestGraphTokenizers outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream t1 = new MockHoleInjectingTokenFilter(Random(), t);
                TokenStream t2 = new MockGraphTokenFilter(Random(), t1);
                return new TokenStreamComponents(t, t2);
            }
        }

        private static Token Token(string term, int posInc, int posLength)
        {
            Token t = new Token(term, 0, 0);
            t.PositionIncrement = posInc;
            t.PositionLength = posLength;
            return t;
        }

        private static Token Token(string term, int posInc, int posLength, int startOffset, int endOffset)
        {
            Token t = new Token(term, startOffset, endOffset);
            t.PositionIncrement = posInc;
            t.PositionLength = posLength;
            return t;
        }

        [Test]
        public virtual void TestSingleToken()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton expected = BasicAutomata.MakeString("abc");
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestMultipleHoles()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("b", 3, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton expected = Join(S2a("a"), SEP_A, HOLE_A, SEP_A, HOLE_A, SEP_A, S2a("b"));
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestSynOverMultipleHoles()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("x", 0, 3), Token("b", 3, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton a1 = Join(S2a("a"), SEP_A, HOLE_A, SEP_A, HOLE_A, SEP_A, S2a("b"));
            Automaton a2 = Join(S2a("x"), SEP_A, S2a("b"));
            Automaton expected = BasicOperations.Union(a1, a2);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        // for debugging!
        /*
        private static void toDot(Automaton a) throws IOException {
          final String s = a.toDot();
          Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp/out.dot"));
          w.write(s);
          w.close();
          System.out.println("TEST: saved to /x/tmp/out.dot");
        }
        */

        private static readonly Automaton SEP_A = BasicAutomata.MakeChar(TokenStreamToAutomaton.POS_SEP);
        private static readonly Automaton HOLE_A = BasicAutomata.MakeChar(TokenStreamToAutomaton.HOLE);

        private Automaton Join(params string[] strings)
        {
            IList<Automaton> @as = new List<Automaton>();
            foreach (string s in strings)
            {
                @as.Add(BasicAutomata.MakeString(s));
                @as.Add(SEP_A);
            }
            @as.RemoveAt(@as.Count - 1);
            return BasicOperations.Concatenate(@as);
        }

        private Automaton Join(params Automaton[] @as)
        {
            return BasicOperations.Concatenate(Arrays.AsList(@as));
        }

        private Automaton S2a(string s)
        {
            return BasicAutomata.MakeString(s);
        }

        [Test]
        public virtual void TestTwoTokens()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("def", 1, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton expected = Join("abc", "def");

            //toDot(actual);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestHole()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("def", 2, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);

            Automaton expected = Join(S2a("abc"), SEP_A, HOLE_A, SEP_A, S2a("def"));

            //toDot(actual);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestOverlappedTokensSausage()
        {
            // Two tokens on top of each other (sausage):
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz", 0, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton a1 = BasicAutomata.MakeString("abc");
            Automaton a2 = BasicAutomata.MakeString("xyz");
            Automaton expected = BasicOperations.Union(a1, a2);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestOverlappedTokensLattice()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz", 0, 2), Token("def", 1, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton a1 = BasicAutomata.MakeString("xyz");
            Automaton a2 = Join("abc", "def");

            Automaton expected = BasicOperations.Union(a1, a2);
            //toDot(actual);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestSynOverHole()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("X", 0, 2), Token("b", 2, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton a1 = BasicOperations.Union(Join(S2a("a"), SEP_A, HOLE_A), BasicAutomata.MakeString("X"));
            Automaton expected = BasicOperations.Concatenate(a1, Join(SEP_A, S2a("b")));
            //toDot(actual);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestSynOverHole2()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("xyz", 1, 1), Token("abc", 0, 3), Token("def", 2, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton expected = BasicOperations.Union(Join(S2a("xyz"), SEP_A, HOLE_A, SEP_A, S2a("def")), BasicAutomata.MakeString("abc"));
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestOverlappedTokensLattice2()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1), Token("xyz", 0, 3), Token("def", 1, 1), Token("ghi", 1, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton a1 = BasicAutomata.MakeString("xyz");
            Automaton a2 = Join("abc", "def", "ghi");
            Automaton expected = BasicOperations.Union(a1, a2);
            //toDot(actual);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        [Test]
        public virtual void TestToDot()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 1, 1, 0, 4) });
            StringWriter w = new StringWriter();
            (new TokenStreamToDot("abcd", ts, (TextWriter)(w))).ToDot();
            Assert.IsTrue(w.ToString().IndexOf("abc / abcd") != -1);
        }

        [Test]
        public virtual void TestStartsWithHole()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("abc", 2, 1) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton expected = Join(HOLE_A, SEP_A, S2a("abc"));
            //toDot(actual);
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }

        // TODO: testEndsWithHole... but we need posInc to set in TS.end()

        [Test]
        public virtual void TestSynHangingOverEnd()
        {
            TokenStream ts = new CannedTokenStream(new Token[] { Token("a", 1, 1), Token("X", 0, 10) });
            Automaton actual = (new TokenStreamToAutomaton()).ToAutomaton(ts);
            Automaton expected = BasicOperations.Union(BasicAutomata.MakeString("a"), BasicAutomata.MakeString("X"));
            Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
        }
    }
}