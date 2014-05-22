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
	/// A convenient class which offers a semi-immutable object wrapper
	/// implementation which allows one to set the value of an object exactly once,
	/// and retrieve it many times. If <seealso cref="#set(Object)"/> is called more than once,
	/// <seealso cref="AlreadySetException"/> is thrown and the operation
	/// will fail.
	/// 
	/// @lucene.experimental
	/// </summary>
	public sealed class SetOnce<T> : ICloneable
	{

	  /// <summary>
	  /// Thrown when <seealso cref="SetOnce#set(Object)"/> is called more than once. </summary>
	  public sealed class AlreadySetException : IllegalStateException
	  {
		public AlreadySetException() : base("The object cannot be set twice!")
		{
		}
	  }

	  private volatile T Obj = null;
	  private readonly AtomicBoolean Set_Renamed;

	  /// <summary>
	  /// A default constructor which does not set the internal object, and allows
	  /// setting it by calling <seealso cref="#set(Object)"/>.
	  /// </summary>
	  public SetOnce()
	  {
		Set_Renamed = new AtomicBoolean(false);
	  }

	  /// <summary>
	  /// Creates a new instance with the internal object set to the given object.
	  /// Note that any calls to <seealso cref="#set(Object)"/> afterwards will result in
	  /// <seealso cref="AlreadySetException"/>
	  /// </summary>
	  /// <exception cref="AlreadySetException"> if called more than once </exception>
	  /// <seealso cref= #set(Object) </seealso>
	  public SetOnce(T obj)
	  {
		this.Obj = obj;
		Set_Renamed = new AtomicBoolean(true);
	  }

	  /// <summary>
	  /// Sets the given object. If the object has already been set, an exception is thrown. </summary>
	  public void Set(T obj)
	  {
		if (Set_Renamed.compareAndSet(false, true))
		{
		  this.Obj = obj;
		}
		else
		{
		  throw new AlreadySetException();
		}
	  }

	  /// <summary>
	  /// Returns the object set by <seealso cref="#set(Object)"/>. </summary>
	  public T Get()
	  {
		return Obj;
	  }

	  public override SetOnce<T> Clone()
	  {
		return Obj == null ? new SetOnce<T>() : new SetOnce<>(Obj);
	  }

	}

}