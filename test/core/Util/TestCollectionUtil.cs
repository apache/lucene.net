using System;
using System.Collections.Generic;

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


	public class TestCollectionUtil : LuceneTestCase
	{

	  private IList<int?> CreateRandomList(int maxSize)
	  {
		Random rnd = random();
		int?[] a = new int?[rnd.Next(maxSize) + 1];
		for (int i = 0; i < a.Length; i++)
		{
		  a[i] = Convert.ToInt32(rnd.Next(a.Length));
		}
		return Arrays.asList(a);
	  }

	  public virtual void TestIntroSort()
	  {
		for (int i = 0, c = atLeast(500); i < c; i++)
		{
		  IList<int?> list1 = CreateRandomList(2000), list2 = new List<int?>(list1);
		  CollectionUtil.introSort(list1);
		  list2.Sort();
		  Assert.AreEqual(list2, list1);

		  list1 = CreateRandomList(2000);
		  list2 = new List<>(list1);
		  CollectionUtil.introSort(list1, Collections.reverseOrder());
		  list2.Sort(Collections.reverseOrder());
		  Assert.AreEqual(list2, list1);
		  // reverse back, so we can test that completely backwards sorted array (worst case) is working:
		  CollectionUtil.introSort(list1);
		  list2.Sort();
		  Assert.AreEqual(list2, list1);
		}
	  }

	  public virtual void TestTimSort()
	  {
		for (int i = 0, c = atLeast(500); i < c; i++)
		{
		  IList<int?> list1 = CreateRandomList(2000), list2 = new List<int?>(list1);
		  CollectionUtil.timSort(list1);
		  list2.Sort();
		  Assert.AreEqual(list2, list1);

		  list1 = CreateRandomList(2000);
		  list2 = new List<>(list1);
		  CollectionUtil.timSort(list1, Collections.reverseOrder());
		  list2.Sort(Collections.reverseOrder());
		  Assert.AreEqual(list2, list1);
		  // reverse back, so we can test that completely backwards sorted array (worst case) is working:
		  CollectionUtil.timSort(list1);
		  list2.Sort();
		  Assert.AreEqual(list2, list1);
		}
	  }

	  public virtual void TestEmptyListSort()
	  {
		// should produce no exceptions
		IList<int?> list = Arrays.asList(new int?[0]); // LUCENE-2989
		CollectionUtil.introSort(list);
		CollectionUtil.timSort(list);
		CollectionUtil.introSort(list, Collections.reverseOrder());
		CollectionUtil.timSort(list, Collections.reverseOrder());

		// check that empty non-random access lists pass sorting without ex (as sorting is not needed)
		list = new LinkedList<>();
		CollectionUtil.introSort(list);
		CollectionUtil.timSort(list);
		CollectionUtil.introSort(list, Collections.reverseOrder());
		CollectionUtil.timSort(list, Collections.reverseOrder());
	  }

	  public virtual void TestOneElementListSort()
	  {
		// check that one-element non-random access lists pass sorting without ex (as sorting is not needed)
		IList<int?> list = new LinkedList<int?>();
		list.Add(1);
		CollectionUtil.introSort(list);
		CollectionUtil.timSort(list);
		CollectionUtil.introSort(list, Collections.reverseOrder());
		CollectionUtil.timSort(list, Collections.reverseOrder());
	  }

	}

}