namespace Lucene.Net.Search
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

	using AttributeImpl = Lucene.Net.Util.AttributeImpl;
	using BytesRef = Lucene.Net.Util.BytesRef;

	/// <summary>
	/// Implementation class for <seealso cref="MaxNonCompetitiveBoostAttribute"/>.
	/// @lucene.internal
	/// </summary>
	public sealed class MaxNonCompetitiveBoostAttributeImpl : AttributeImpl, MaxNonCompetitiveBoostAttribute
	{
	  private float MaxNonCompetitiveBoost_Renamed = float.NegativeInfinity;
	  private BytesRef CompetitiveTerm_Renamed = null;

	  public override float MaxNonCompetitiveBoost
	  {
		  set
		  {
			this.MaxNonCompetitiveBoost_Renamed = value;
		  }
		  get
		  {
			return MaxNonCompetitiveBoost_Renamed;
		  }
	  }


	  public override BytesRef CompetitiveTerm
	  {
		  set
		  {
			this.CompetitiveTerm_Renamed = value;
		  }
		  get
		  {
			return CompetitiveTerm_Renamed;
		  }
	  }


	  public override void Clear()
	  {
		MaxNonCompetitiveBoost_Renamed = float.NegativeInfinity;
		CompetitiveTerm_Renamed = null;
	  }

	  public override void CopyTo(AttributeImpl target)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final MaxNonCompetitiveBoostAttributeImpl t = (MaxNonCompetitiveBoostAttributeImpl) target;
		MaxNonCompetitiveBoostAttributeImpl t = (MaxNonCompetitiveBoostAttributeImpl) target;
		t.MaxNonCompetitiveBoost = MaxNonCompetitiveBoost_Renamed;
		t.CompetitiveTerm = CompetitiveTerm_Renamed;
	  }
	}

}