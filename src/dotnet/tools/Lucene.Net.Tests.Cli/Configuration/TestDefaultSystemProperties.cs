﻿using Lucene.Net.Configuration;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Cli.Configuration
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

    [TestFixture]
    class TestDefaultSystemProperties : LuceneTestCase
    {
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            Environment.SetEnvironmentVariable(testKey, testValue);
            base.BeforeClass();
            ConfigurationSettings.SetConfigurationRootFactory(new DefaultConfigurationRootFactory() { IgnoreSecurityExceptionsOnRead = false });
        }

        [Test]
        public virtual void ReadEnvironmentTest()
        {
            string testKey = "tests:setting";
            string testValue = "test.success";
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }
        [Test]
        public virtual void SetEnvironmentTest()
        {
            string setKey = "tests:setting";
            string testKey = "tests:setting";
            string testValue = "test.success";
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[setKey] = testValue;
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

    }
}