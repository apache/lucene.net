﻿namespace org.apache.lucene.analysis.cz
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

	using org.apache.lucene.analysis.util;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.util.StemmerUtil.*;

	/// <summary>
	/// Light Stemmer for Czech.
	/// <para>
	/// Implements the algorithm described in:  
	/// <i>
	/// Indexing and stemming approaches for the Czech language
	/// </i>
	/// http://portal.acm.org/citation.cfm?id=1598600
	/// </para>
	/// </summary>
	public class CzechStemmer
	{

	  /// <summary>
	  /// Stem an input buffer of Czech text.
	  /// </summary>
	  /// <param name="s"> input buffer </param>
	  /// <param name="len"> length of input buffer </param>
	  /// <returns> length of input buffer after normalization
	  /// 
	  /// <para><b>NOTE</b>: Input is expected to be in lowercase, 
	  /// but with diacritical marks</para> </returns>
	  public virtual int stem(char[] s, int len)
	  {
		len = removeCase(s, len);
		len = removePossessives(s, len);
		if (len > 0)
		{
		  len = normalize(s, len);
		}
		return len;
	  }

	  private int removeCase(char[] s, int len)
	  {
		if (len > 7 && StemmerUtil.EndsWith(s, len, "atech"))
		{
		  return len - 5;
		}

		if (len > 6 && (StemmerUtil.EndsWith(s, len,"ětem") || StemmerUtil.EndsWith(s, len,"etem") || StemmerUtil.EndsWith(s, len,"atům")))
		{
		  return len - 4;
		}

		if (len > 5 && (StemmerUtil.EndsWith(s, len, "ech") || StemmerUtil.EndsWith(s, len, "ich") || StemmerUtil.EndsWith(s, len, "ích") || StemmerUtil.EndsWith(s, len, "ého") || StemmerUtil.EndsWith(s, len, "ěmi") || StemmerUtil.EndsWith(s, len, "emi") || StemmerUtil.EndsWith(s, len, "ému") || StemmerUtil.EndsWith(s, len, "ěte") || StemmerUtil.EndsWith(s, len, "ete") || StemmerUtil.EndsWith(s, len, "ěti") || StemmerUtil.EndsWith(s, len, "eti") || StemmerUtil.EndsWith(s, len, "ího") || StemmerUtil.EndsWith(s, len, "iho") || StemmerUtil.EndsWith(s, len, "ími") || StemmerUtil.EndsWith(s, len, "ímu") || StemmerUtil.EndsWith(s, len, "imu") || StemmerUtil.EndsWith(s, len, "ách") || StemmerUtil.EndsWith(s, len, "ata") || StemmerUtil.EndsWith(s, len, "aty") || StemmerUtil.EndsWith(s, len, "ých") || StemmerUtil.EndsWith(s, len, "ama") || StemmerUtil.EndsWith(s, len, "ami") || StemmerUtil.EndsWith(s, len, "ové") || StemmerUtil.EndsWith(s, len, "ovi") || StemmerUtil.EndsWith(s, len, "ými")))
		{
		  return len - 3;
		}

		if (len > 4 && (StemmerUtil.EndsWith(s, len, "em") || StemmerUtil.EndsWith(s, len, "es") || StemmerUtil.EndsWith(s, len, "ém") || StemmerUtil.EndsWith(s, len, "ím") || StemmerUtil.EndsWith(s, len, "ům") || StemmerUtil.EndsWith(s, len, "at") || StemmerUtil.EndsWith(s, len, "ám") || StemmerUtil.EndsWith(s, len, "os") || StemmerUtil.EndsWith(s, len, "us") || StemmerUtil.EndsWith(s, len, "ým") || StemmerUtil.EndsWith(s, len, "mi") || StemmerUtil.EndsWith(s, len, "ou")))
		{
		  return len - 2;
		}

		if (len > 3)
		{
		  switch (s[len - 1])
		  {
			case 'a':
			case 'e':
			case 'i':
			case 'o':
			case 'u':
			case 'ů':
			case 'y':
			case 'á':
			case 'é':
			case 'í':
			case 'ý':
			case 'ě':
			  return len - 1;
		  }
		}

		return len;
	  }

	  private int removePossessives(char[] s, int len)
	  {
		if (len > 5 && (StemmerUtil.EndsWith(s, len, "ov") || StemmerUtil.EndsWith(s, len, "in") || StemmerUtil.EndsWith(s, len, "ův")))
		{
		  return len - 2;
		}

		return len;
	  }

	  private int normalize(char[] s, int len)
	  {
		if (StemmerUtil.EndsWith(s, len, "čt")) // čt -> ck
		{
		  s[len - 2] = 'c';
		  s[len - 1] = 'k';
		  return len;
		}

		if (StemmerUtil.EndsWith(s, len, "št")) // št -> sk
		{
		  s[len - 2] = 's';
		  s[len - 1] = 'k';
		  return len;
		}

		switch (s[len - 1])
		{
		  case 'c': // [cč] -> k
		  case 'č':
			s[len - 1] = 'k';
			return len;
		  case 'z': // [zž] -> h
		  case 'ž':
			s[len - 1] = 'h';
			return len;
		}

		if (len > 1 && s[len - 2] == 'e')
		{
		  s[len - 2] = s[len - 1]; // e* > *
		  return len - 1;
		}

		if (len > 2 && s[len - 2] == 'ů')
		{
		  s[len - 2] = 'o'; // *ů* -> *o*
		  return len;
		}

		return len;
	  }
	}

}