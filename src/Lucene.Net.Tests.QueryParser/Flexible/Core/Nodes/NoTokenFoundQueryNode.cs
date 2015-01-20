/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="NoTokenFoundQueryNode">NoTokenFoundQueryNode</see>
	/// is used if a term is convert into no tokens
	/// by the tokenizer/lemmatizer/analyzer (null).
	/// </summary>
	public class NoTokenFoundQueryNode : DeletedQueryNode
	{
		public NoTokenFoundQueryNode() : base()
		{
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			return "[NTF]";
		}

		public override string ToString()
		{
			return "<notokenfound/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.NoTokenFoundQueryNode clone = (
				Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.NoTokenFoundQueryNode)base.CloneTree
				();
			// nothing to do here
			return clone;
		}
	}
}
