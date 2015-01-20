/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor removes every
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// that contains only one
	/// child and returns this child. If this child is
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// that
	/// was defined by the user. A modifier is not defined by the user when it's a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.BooleanModifierNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.BooleanModifierNode</see>
	/// <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode</seealso>
	public class BooleanSingleChildOptimizationQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public BooleanSingleChildOptimizationQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is BooleanQueryNode)
			{
				IList<QueryNode> children = node.GetChildren();
				if (children != null && children.Count == 1)
				{
					QueryNode child = children[0];
					if (child is ModifierQueryNode)
					{
						ModifierQueryNode modNode = (ModifierQueryNode)child;
						if (modNode is BooleanModifierNode || modNode.GetModifier() == ModifierQueryNode.Modifier
							.MOD_NONE)
						{
							return child;
						}
					}
					else
					{
						return child;
					}
				}
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
