using System;

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

	/// <summary>
	/// Sneaky: rethrowing checked exceptions as unchecked
	/// ones. Eh, it is sometimes useful...
	/// 
	/// <p>Pulled from <a href="http://www.javapuzzlers.com">Java Puzzlers</a>.</p> </summary>
	/// <seealso cref= "http://www.amazon.com/Java-Puzzlers-Traps-Pitfalls-Corner/dp/032133678X" </seealso>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"unchecked","rawtypes"}) public final class Rethrow
	public sealed class Rethrow
	{
	  /// <summary>
	  /// Classy puzzler to rethrow any checked exception as an unchecked one.
	  /// </summary>
	  private class Rethrower<T> where T : Throwable
	  {
		internal virtual void Rethrow(Exception t)
		{
		  throw (T) t;
		}
	  }

	  /// <summary>
	  /// Rethrows <code>t</code> (identical object).
	  /// </summary>
	  public static void Rethrow(Exception t)
	  {
		(new Rethrower<Exception>()).Rethrow(t);
	  }
	}


}