﻿#if FEATURE_SERIALIZABLE
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    public abstract class ExceptionSerializationTestBase : LuceneTestCase
#if TESTFRAMEWORK_XUNIT
        , Xunit.IClassFixture<BeforeAfterClass>
    {
        public ExceptionSerializationTestBase(BeforeAfterClass beforeAfter)
            : base(beforeAfter)
        {
        }
#else
    {
#endif

        protected static bool TypeCanSerialize<T>(T exception)
        {
            T clone;

            try
            {
                var binaryFormatter = new BinaryFormatter();
                using var serializationStream = new MemoryStream();
                binaryFormatter.Serialize(serializationStream, exception);
                serializationStream.Seek(0, SeekOrigin.Begin);
                clone = (T)binaryFormatter.Deserialize(serializationStream);
            }
            catch (SerializationException)
            {
                return false;
            }

            return true;
        }

        protected static object TryInstantiate(Type type)
        {
            object instance = null;
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            try
            {
                instance = Activator.CreateInstance(type, flags, null, new object[] { "A message" }, CultureInfo.InvariantCulture);
            }
            catch (MissingMethodException)
            {
                try
                {
                    instance = Activator.CreateInstance(type, flags, null, null, CultureInfo.InvariantCulture);
                }
                catch (MissingMethodException)
                {
                    Assert.Fail("Can't instantiate type {0}, it's missing the necessary constructors.", type.FullName);
                }
            }
            return instance;
        }
    }
}
#endif
