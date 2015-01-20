/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.RegexpQuery">Org.Apache.Lucene.Search.RegexpQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.RegexpQueryNode"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.RegexpQueryNode</see>
	/// object.
	/// </summary>
	public class RegexpQueryNodeBuilder : StandardQueryBuilder
	{
		public RegexpQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual RegexpQuery Build(QueryNode queryNode)
		{
			RegexpQueryNode regexpNode = (RegexpQueryNode)queryNode;
			RegexpQuery q = new RegexpQuery(new Term(regexpNode.GetFieldAsString(), regexpNode
				.TextToBytesRef()));
			MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag
				(MultiTermRewriteMethodProcessor.TAG_ID);
			if (method != null)
			{
				q.SetRewriteMethod(method);
			}
			return q;
		}
	}
}
