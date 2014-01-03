﻿using System.Collections.Generic;
using System.Linq;

namespace Northwind.Data.Helpers
{
    internal static class FilterParser
    {
        public static FilterNode Parse(string filterStr)
        {
            var tok = new StringTokenizer(filterStr)
                {
                    IgnoreWhiteSpace = true,
                    // the following two should be mutually exclusive
                    // if the following have a mutually shared char, Word char will take precedence
                    // when creating tokens at this time...
                    AdditionalWordChars = new char[] { '*', ' ', '.' },
                    SymbolChars = new[] { '(', ')', '^', '|', ':' }
                };
                
            var nodeid = 0;
            var nodes = new Dictionary<int, FilterNode>();

            var currentNodeId = ++nodeid;
            nodes.Add(currentNodeId, new FilterNode { ParentNodeId = 0, NodeId = currentNodeId, NodeType = FilterNodeType.Root });
            var currentNode = nodes[nodeid];

            // parse tokens
            Token token;
            do
            {
                token = tok.Next();

                if (token.Kind == TokenKind.Symbol && token.Value == "(")
                {
                    currentNodeId = ++nodeid;
                    nodes.Add(currentNodeId, new FilterNode { ParentNodeId = currentNode.NodeId, NodeId = currentNodeId });
                    currentNode = nodes[nodeid];
                }
                else if (token.Kind == TokenKind.Symbol && token.Value == ")")
                {
                    currentNode = nodes[currentNode.ParentNodeId];
                }
                else if (token.Kind == TokenKind.Symbol && token.Value == "^")
                {
                    currentNode.NodeType = FilterNodeType.AndExpression;
                }
                else if (token.Kind == TokenKind.Symbol && token.Value == "|")
                {
                    currentNode.NodeType = FilterNodeType.OrExpression;
                }
                else if (token.Kind == TokenKind.Symbol && token.Value == ":")
                {
                }
                else if (token.Kind == TokenKind.Word || token.Kind == TokenKind.Number || token.Kind == TokenKind.QuotedString)
                {
                    currentNode.Elements.Add(token.Value);
                }

            } while (token.Kind != TokenKind.EOF);

            // build the tree
            foreach (var node in nodes)
            {
                node.Value.Nodes.AddRange(nodes.Where(n => n.Value.ParentNodeId == node.Key).Select(n => n.Value));
                node.Value.ParentNode = nodes.SingleOrDefault(n => n.Key == node.Value.ParentNodeId).Value;
            }

            // return the root node
            var filterNode = nodes[1];
            return filterNode;
        }
    }

    internal class FilterNode
    {
        public FilterNode()
        {
            NodeType = FilterNodeType.Clause;
            Elements = new List<string>();
            Nodes = new List<FilterNode>();
        }

        public FilterNodeType NodeType { get; set; }
        public int NodeId { get; set; }

        public int ParentNodeId { get; set; }
        public FilterNode ParentNode { get; set; }

        public int ElementCount { get { return Elements.Count(); } }
        public List<string> Elements { get; set; }

        public int NodeCount { get { return Nodes.Count(); } }
        public List<FilterNode> Nodes { get; set; }
    }

    internal enum FilterNodeType
    {
        Root,
        AndExpression,
        OrExpression,
        Clause,
    }
}
  