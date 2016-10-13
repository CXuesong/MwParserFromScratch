using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    internal interface IInsertItem
    {
        void InsertBefore(Node node, Node newNode);

        void InsertAfter(Node node, Node newNode);
    }

    /// <summary>
    /// Represents a collection of nodes.
    /// The children are maintained as a bi-directional linked list.
    /// </summary>
    public class NodeCollection<TNode> : IEnumerable<TNode>, IInsertItem
        where TNode : Node
    {
        private readonly Node _Owner;

        internal NodeCollection(Node owner)
        {
            _Owner = owner;
        }

        /// <summary>
        /// The first node.
        /// </summary>
        public TNode FirstNode { get; internal set; }

        /// <summary>
        /// The last node.
        /// </summary>
        public TNode LastNode { get; internal set; }

        /// <summary>
        /// Appends a new node into the children collection.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        /// <remarks>A clone of <paramref name="node"/> will be added into the children collection if the node has already attached to the syntax tree.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="node"/> is invalid for the container.</exception>
        public void Add(TNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            node = _Owner.Attach(node);
            // Add the child.
            node.ParentItemInserter = this;
            node.PreviousNode = LastNode;
            LastNode.NextNode = node;
            LastNode = node;
        }

        /// <summary>
        /// Appends new nodes into the children collection.
        /// </summary>
        /// <param name="nodes">The nodes to be added.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nodes"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The type of a element in <paramref name="nodes"/> is invalid for the container.</exception>
        public void Add(IEnumerable<TNode> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            foreach (var n in nodes)
                Add(n);
        }

        internal void InsertBefore(TNode node, TNode newNode)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.ParentNode == _Owner);
            Debug.Assert(newNode != null);
            newNode = _Owner.Attach(newNode);
            newNode.ParentItemInserter = this;
            var prev = node.PreviousNode;
            if (prev != null)
            {
                prev.NextNode = newNode;
                newNode.PreviousNode = prev;
            }
            else
            {
                Debug.Assert(FirstNode == node);
                FirstNode = newNode;
                newNode.PreviousNode = null;
            }
            newNode.NextNode = node;
            node.PreviousNode = newNode;
        }

        internal void InsertAfter(TNode node, TNode newNode)
        {
            Debug.Assert(node != null);
            Debug.Assert(node.ParentNode == _Owner);
            Debug.Assert(newNode != null);
            newNode = _Owner.Attach(newNode);
            newNode.ParentItemInserter = this;
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
                Debug.Assert(LastNode == node);
                newNode.NextNode = null;
                LastNode = newNode;
            }
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// </summary>
        /// <returns>
        /// 可用于循环访问集合的 <see cref="T:System.Collections.Generic.IEnumerator`1"/>。
        /// </returns>
        public IEnumerator<TNode> GetEnumerator()
        {
            return new MyEnumerator(this);
        }

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// </summary>
        /// <returns>
        /// 可用于循环访问集合的 <see cref="T:System.Collections.IEnumerator"/> 对象。
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class MyEnumerator : IEnumerator<TNode>
        {
            private readonly NodeCollection<TNode> _Owner;
            private bool needsReset = true;

            public bool MoveNext()
            {
                if (needsReset)
                {
                    Current = _Owner.FirstNode;
                    needsReset = false;
                }
                else
                {
                    Current = (TNode) Current.NextNode;
                }
                return Current != null;
            }

            public void Reset()
            {
                needsReset = true;
            }

            public TNode Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }

            public MyEnumerator(NodeCollection<TNode> owner)
            {
                Debug.Assert(owner != null);
                _Owner = owner;
            }
        }

        void IInsertItem.InsertBefore(Node node, Node newNode)
        {
            InsertBefore((TNode) node, (TNode) newNode);
        }

        void IInsertItem.InsertAfter(Node node, Node newNode)
        {
            InsertBefore((TNode) node, (TNode) newNode);
        }
    }
}

