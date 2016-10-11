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
        public ContainerNode ParentNode { get; internal set; }

        /// <summary>
        /// Inserts a sibling node before the current node.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ParentNode"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="node"/> is invalid for the container.</exception>
        public void InsertBefore(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (ParentNode == null) throw new InvalidOperationException("Cannot insert the sibling node when ParentNode is null.");
            ParentNode.InsertBefore(this, node);
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
            if (ParentNode == null) throw new InvalidOperationException("Cannot insert the sibling node when ParentNode is null.");
            ParentNode.InsertAfter(this, node);
        }
        #endregion

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

    /// <summary>
    /// Represents a node that can contain other nodes.
    /// </summary>
    public abstract class ContainerNode : Node
    {
        /// <summary>
        /// Initializes a <see cref="ContainerNode"/> with no children.
        /// </summary>
        protected ContainerNode()
        {

        }

        /// <summary>
        /// The collection of children.
        /// </summary>
        public NodeCollection Children { get; } = new NodeCollection();

        private void AssertCanInsert(Node newNode)
        {
            Debug.Assert(newNode != null);
            var nodeType = newNode.GetType().GetTypeInfo();
            if (!GetType().GetTypeInfo().GetCustomAttributes<ChildrenTypeAttribute>().Any(a =>
                a.ChildrenType.GetTypeInfo().IsAssignableFrom(nodeType)))
                throw new ArgumentException($"Invalid node type: {newNode} .");
        }

        /// <summary>
        /// Appends a new node into the children collection.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        /// <remarks>A clone of <paramref name="node"/> will be added into the children collection if the node has already attached to the syntax tree.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="node"/> is invalid for the container.</exception>
        public void Add(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            AssertCanInsert(node);
            // Make a deep copy, if needed.
            if (node.ParentNode != null)
                node = node.Clone();
            // Add the child.
            node.ParentNode = this;
            node.PreviousNode = Children.LastNode;
            Children.LastNode.NextNode = node;
            Children.LastNode = node;
        }

        /// <summary>
        /// Appends new nodes into the children collection.
        /// </summary>
        /// <param name="nodes">The nodes to be added.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of a element in <paramref name="nodes"/> is invalid for the container.</exception>
        public void Add(IEnumerable<Node> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            foreach (var n in nodes)
                Add(n);
        }

        internal void InsertBefore(Node node, Node newNode)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.ParentNode == this);
            Debug.Assert(newNode != null);
            AssertCanInsert(node);
            // Make a deep copy, if needed.
            if (newNode.ParentNode != null)
                newNode = newNode.Clone();
            var prev = node.PreviousNode;
            if (prev != null)
            {
                prev.NextNode = newNode;
                newNode.PreviousNode = prev;
            }
            else
            {
                Debug.Assert(Children.FirstNode == node);
                Children.FirstNode = newNode;
                newNode.PreviousNode = null;
            }
            newNode.NextNode = node;
            node.PreviousNode = newNode;
        }

        internal void InsertAfter(Node node, Node newNode)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.ParentNode == this);
            Debug.Assert(newNode != null);
            AssertCanInsert(node);
            // Make a deep copy, if needed.
            if (newNode.ParentNode != null)
                newNode = newNode.Clone();
            node.NextNode = newNode;
            newNode.PreviousNode = node;
            var next = node.NextNode;
            if (next != null)
            {
                newNode.NextNode = next;
                next.PreviousNode = newNode;
            }
            else
            {
                Debug.Assert(Children.LastNode == node);
                newNode.NextNode = null;
                Children.LastNode = newNode;
            }
        }
    }
}
