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
        /// The parent node.
        /// </summary>
        internal IInsertItem ParentItemInserter { get; set; }

        /// <summary>
        /// Inserts a sibling node before the current node.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ParentNode"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="node"/> is invalid for the container.</exception>
        public void InsertBefore(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ParentItemInserter == null) throw new InvalidOperationException("Cannot insert the sibling node.");
            ParentItemInserter.InsertBefore(this, node);
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
            if (ParentItemInserter == null) throw new InvalidOperationException("Cannot insert the sibling node.");
            ParentItemInserter.InsertAfter(this, node);
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
