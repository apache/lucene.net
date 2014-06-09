/*
 * dk.brics.automaton
 * 
 * Copyright (c) 2001-2009 Anders Moeller
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 * 
 * this SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * this SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Lucene.Net.Util.Automaton
{

	/// <summary>
	/// Pair of states.
	/// 
	/// @lucene.experimental
	/// </summary>
	public class StatePair
	{
	  internal State s;
	  internal State S1;
	  internal State S2;

	  internal StatePair(State s, State s1, State s2)
	  {
		this.s = s;
		this.S1 = s1;
		this.S2 = s2;
	  }

	  /// <summary>
	  /// Constructs a new state pair.
	  /// </summary>
	  /// <param name="s1"> first state </param>
	  /// <param name="s2"> second state </param>
	  public StatePair(State s1, State s2)
	  {
		this.S1 = s1;
		this.S2 = s2;
	  }

	  /// <summary>
	  /// Returns first component of this pair.
	  /// </summary>
	  /// <returns> first state </returns>
	  public virtual State FirstState
	  {
		  get
		  {
			return S1;
		  }
	  }

	  /// <summary>
	  /// Returns second component of this pair.
	  /// </summary>
	  /// <returns> second state </returns>
	  public virtual State SecondState
	  {
		  get
		  {
			return S2;
		  }
	  }

	  /// <summary>
	  /// Checks for equality.
	  /// </summary>
	  /// <param name="obj"> object to compare with </param>
	  /// <returns> true if <tt>obj</tt> represents the same pair of states as this
	  ///         pair </returns>
	  public override bool Equals(object obj)
	  {
		if (obj is StatePair)
		{
		  StatePair p = (StatePair) obj;
		  return p.S1 == S1 && p.S2 == S2;
		}
		else
		{
			return false;
		}
	  }

	  /// <summary>
	  /// Returns hash code.
	  /// </summary>
	  /// <returns> hash code </returns>
	  public override int GetHashCode()
	  {
          return S1.GetHashCode() + S2.GetHashCode();
	  }
	}

}