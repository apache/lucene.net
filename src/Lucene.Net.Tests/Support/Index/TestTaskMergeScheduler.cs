using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;

    /// <summary>
    /// Holds tests cases to verify external APIs are accessible
    /// while not being in Lucene.Net.Index package.
    /// </summary>
    public class TestTaskMergeScheduler : LuceneTestCase
    {
        internal volatile bool mergeCalled;
        internal volatile bool excCalled;

        private class MyMergeScheduler : TaskMergeScheduler
        {
            private readonly TestTaskMergeScheduler outerInstance;

            public MyMergeScheduler(TestTaskMergeScheduler outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override void HandleMergeException(Exception t)
            {
                outerInstance.excCalled = true;
            }

            public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
            {
                outerInstance.mergeCalled = true;
                base.Merge(writer, trigger, newMergesFound);
            }
        }

        private class FailOnlyOnMerge : Failure
        {
            public override void Eval(MockDirectoryWrapper dir)
            {
                // LUCENENET specific: for these to work in release mode, we have added [MethodImpl(MethodImplOptions.NoInlining)]
                // to each possible target of the StackTraceHelper. If these change, so must the attribute on the target methods.
                if (StackTraceHelper.DoesStackTraceContainMethod("DoMerge"))
                {
                    throw new IOException("now failing during merge");
                }
            }
        }

        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/269")] // LUCENENET TODO: this test occasionally fails
        public void TestSubclassTaskMergeScheduler()
        {
            MockDirectoryWrapper dir = NewMockDirectory();
            dir.FailOn(new FailOnlyOnMerge());

            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);

            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new MyMergeScheduler(this)).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NewLogMergePolicy()));
            LogMergePolicy logMP = (LogMergePolicy)writer.Config.MergePolicy;
            logMP.MergeFactor = 10;
            for (int i = 0; i < 20; i++)
            {
                writer.AddDocument(doc);
            }

            ((MyMergeScheduler)writer.Config.MergeScheduler).Sync();
            writer.Dispose();

            assertTrue(mergeCalled);
            dir.Dispose();
        }

        private class ReportingMergeScheduler : MergeScheduler
        {
            public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
            {
                MergePolicy.OneMerge merge = null;
                while ((merge = writer.NextMerge()) != null)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("executing merge " + merge.SegString(writer.Directory));
                    }
                    writer.Merge(merge);
                }
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        [Test]
        public void TestCustomMergeScheduler()
        {
            // we don't really need to execute anything, just to make sure the custom MS
            // compiles. But ensure that it can be used as well, e.g., no other hidden
            // dependencies or something. Therefore, don't use any random API !
            Directory dir = new RAMDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.SetMergeScheduler(new ReportingMergeScheduler());
            IndexWriter writer = new IndexWriter(dir, conf);
            writer.AddDocument(new Document());
            writer.Commit(); // trigger flush
            writer.AddDocument(new Document());
            writer.Commit(); // trigger flush
            writer.ForceMerge(1);
            writer.Dispose();
            dir.Dispose();
        }

        // LUCENENET-603
        [Test, LuceneNetSpecific]
        public void TestExceptionOnBackgroundThreadIsPropagatedToCallingThread()
        {
            using MockDirectoryWrapper dir = NewMockDirectory();
            dir.FailOn(new FailOnlyOnMerge());

            Document doc = new Document();
            Field idField = NewStringField("id", "", Field.Store.YES);
            doc.Add(idField);

            var mergeScheduler = new TaskMergeScheduler();
            using IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(mergeScheduler).SetMaxBufferedDocs(2).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).SetMergePolicy(NewLogMergePolicy()));
            LogMergePolicy logMP = (LogMergePolicy)writer.Config.MergePolicy;
            logMP.MergeFactor = 10;
            for (int i = 0; i < 20; i++)
            {
                writer.AddDocument(doc);
            }

            bool exceptionHit = false;
            try
            {
                mergeScheduler.Sync();
            }
            catch (MergePolicy.MergeException)
            {
                exceptionHit = true;
            }

            assertTrue(exceptionHit);
        }
    }
}