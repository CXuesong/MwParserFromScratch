using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Represents the abstract concept of a node in the syntax tree.
    /// </summary>
    public abstract class Node : IWikitextLineInfo
    {
        private int _LineNumber = 0;
        private int _LinePosition = 0;

        #region Tree
        /// <summary>
        /// The previous sibling node.
        /// </summary>
        public Node PreviousNode { get; internal set; }

        /// <summary>
        /// The next sibling node.
        /// </summary>
        public Node NextNode { get; internal set; }

        /// <summary>
        /// The parent node.
        /// </summary>
        public Node ParentNode { get; internal set; }

        /// <summary>
        /// Enumerates the sibling nodes after this node.
        /// </summary>
        public IEnumerable<Node> EnumNextNodes()
        {
            var node = NextNode;
            while (node != null)
            {
                yield return node;
                node = node.NextNode;
            }
        }

        /// <summary>
        /// Enumerates the children of this node.
        /// </summary>
        public abstract IEnumerable<Node> EnumChildren();

        /// <summary>
        /// Enumerates the descendants of this node.
        /// </summary>
        /// <returns>A sequence of nodes, in document order.</returns>
        public IEnumerable<Node> EnumDescendants()
        {
            // In document order == DFS
            var stack = new Stack<IEnumerator<Node>>();
            stack.Push(EnumChildren().GetEnumerator());
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                yield return top.Current;
                stack.Push(top.Current.EnumChildren().GetEnumerator());
            }
        }

        /// <summary>
        /// The parent node.
        /// </summary>
        internal INodeCollection ParentCollection { get; set; }

        /// <summary>
        /// Inserts a sibling node before the current node.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ParentNode"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="node"/> is invalid for the container.</exception>
        public void InsertBefore(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ParentCollection == null) throw new InvalidOperationException("Cannot insert the sibling node.");
            ParentCollection.InsertBefore(this, node);
        }

        /// <summary>
        /// Inserts a sibling node after the current node.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ParentNode"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="node"/> is invalid for the container.</exception>
        public void InsertAfter(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ParentCollection == null) throw new InvalidOperationException("Cannot insert the sibling node.");
            ParentCollection.InsertAfter(this, node);
        }

        /// <summary>
        /// Remove this node from its parent collection.
        /// </summary>
        /// <remarks>
        /// To remove this node from its parent property (e.g. <see cref="Template.Name"/>),
        /// please set the property value to <c>null</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">This node is not attached to its parent via a collection.</exception>
        public void Remove()
        {
            if (ParentCollection == null)
                throw new InvalidOperationException("Cannot remove the node that is not attached to its parent via a collection.");
            var result = ParentCollection.Remove(this);
            Debug.Assert(result);
        }

        internal TNode Attach<TNode>(TNode newNode)
            where TNode : Node
        {
            // Make a deep copy, if needed.
            if (newNode.ParentNode != null)
                newNode = (TNode) newNode.Clone();
            newNode.ParentNode = this;
            return newNode;
        }

        internal void Attach<TNode>(ref TNode nodeStorge, TNode newValue)
            where TNode : Node
        {
            if (newValue == nodeStorge) return;
            newValue = Attach(newValue);
            if (nodeStorge != null) Detach(nodeStorge);
            nodeStorge = newValue;
        }

        internal void Detach(Node node)
        {
            node.ParentNode = null;
            // Then disconnect the node in caller function.
        }
        #endregion

        #region LineInfo
        /// <summary>
        /// Gets the current line number. 
        /// </summary>
        /// <remarks>The current line number or 0 if no line information is available (for example, HasLineInfo returns false).</remarks>
        public int LineNumber => _LineNumber;

        /// <summary>
        /// Gets the current line position. 
        /// </summary>
        /// <remarks>The current line position or 0 if no line information is available (for example, HasLineInfo returns false).</remarks>
        public int LinePosition => _LinePosition;

        /// <summary>
        /// Gets a value indicating whether the class can return line information.
        /// </summary>
        public bool HasLineInfo()
        {
            return _LineNumber > 0;
        }

        internal void SetLineInfo(int lineNumber, int linePosition)
        {
            Debug.Assert(lineNumber > 0);
            Debug.Assert(linePosition > 0);
            _LineNumber = lineNumber;
            _LinePosition = linePosition;
        }
        #endregion

        protected abstract Node CloneCore();

        /// <summary>
        /// Makes a deep copy of the node.
        /// </summary>
        public Node Clone()
        {
            var newInst = CloneCore();
            Debug.Assert(newInst != null);
            Debug.Assert(newInst.GetType() == this.GetType());
            return newInst;
        }
    }
}
