﻿using Lucene.Net.Configuration;
using Lucene.Net.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;

namespace Lucene.Net.Configuration
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
    class TestConfigurationSettings : LuceneTestCase
    {

        internal IProperties SystemProperties { get; private set; }

        public IConfigurationSettings ConfigurationSettings { get; private set; }

        protected IConfigurationRoot LoadConfiguration()
        {
            string JsonTestPath =
#if TESTFRAMEWORK_NUNIT
            NUnit.Framework.TestContext.CurrentContext.TestDirectory;
#else
            AppDomain.CurrentDomain.BaseDirectory;
#endif
            string JsonTestSettingsFolderName = @"Configuration";
            string JsonTestSettingsFileName = @"lucene.TestSettings.json";
            return new ConfigurationBuilder().Add(new LuceneDefaultConfigurationSource() { Prefix = "lucene:", IgnoreSecurityExceptionsOnRead = false }
            ).AddJsonFile(Path.Combine(new string[] { JsonTestPath, JsonTestSettingsFolderName, JsonTestSettingsFileName })).Build();
        }

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            // set an Enviroment variable used in the test
            string testKey = "lucene:tests:setup";
            string testValue = "setup";
            Environment.SetEnvironmentVariable(testKey, testValue);

            base.BeforeClass();
            var configurationRoot = LoadConfiguration();
            // Set up mocks for ConfigurationSettings and SystemProperties
            ConfigurationSettings = new MockConfigurationSettings(configurationRoot);
            SystemProperties = new Properties(configurationRoot);
        }

        [Test]
        public virtual void ReadEnvironmentTest()
        {
            string testKey = "tests:setup";
            string testValue = "setup";
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }
        [Test]
        public virtual void SetEnvironmentTest()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

        [Test]
        public virtual void TestSetandUnset()
        {
            string testKey = "tests:locale";
            string testValue_fr = "fr";
            string testValue_en = "en";
            //ConfigurationSettings.CurrentConfiguration[testKey] = testValue_fr;
            Assert.AreEqual(testValue_fr, ConfigurationSettings.CurrentConfiguration["tests:locale"]);
            Assert.AreEqual(testValue_fr, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_fr, SystemProperties.GetProperty(testKey));
            ConfigurationSettings.CurrentConfiguration[testKey] = testValue_en;
            Assert.AreEqual(testValue_en, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_en, SystemProperties.GetProperty(testKey));
            ConfigurationSettings.CurrentConfiguration.Reload();
            Assert.AreEqual(testValue_fr, ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue_fr, SystemProperties.GetProperty(testKey));
        }
    }
}
