using System.Collections.Generic;

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

	/// <summary>
	/// An <seealso cref="IndexDeletionPolicy"/> which keeps all index commits around, never
	/// deleting them. this class is a singleton and can be accessed by referencing
	/// <seealso cref="#INSTANCE"/>.
	/// </summary>
	public sealed class NoDeletionPolicy : IndexDeletionPolicy
	{

	  /// <summary>
	  /// The single instance of this class. </summary>
	  public static readonly IndexDeletionPolicy INSTANCE = new NoDeletionPolicy();

	  private NoDeletionPolicy()
	  {
		// keep private to avoid instantiation
	  }

	  public override void onCommit<T1>(IList<T1> commits) where T1 : IndexCommit
	  {
	  }
	  public override void onInit<T1>(IList<T1> commits) where T1 : IndexCommit
	  {
	  }
	  public override IndexDeletionPolicy Clone()
	  {
		return this;
	  }

	}

}