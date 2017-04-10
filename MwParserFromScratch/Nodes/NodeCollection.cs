using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    internal interface INodeCollection
    {
        void InsertBefore(Node node, Node newNode);

        void InsertAfter(Node node, Node newNode);

        bool Remove(Node node);
    }

    /// <summary>
    /// Represents a collection of nodes.
    /// The children are maintained as a bi-directional linked list.
    /// </summary>
    public class NodeCollection<TNode> : ICollection<TNode>, INodeCollection
        where TNode : Node
    {
        private readonly Node _Owner;
        private int _Count;

        internal NodeCollection(Node owner)
        {
            Debug.Assert(owner != null);
            _Owner = owner;
        }

        /// <summary>
        /// The first node.
        /// </summary>
        public TNode FirstNode { get; private set; }

        /// <summary>
        /// The last node.
        /// </summary>
        public TNode LastNode { get; private set; }

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
            // Attach the child. Copy node if necessary.
            node = _Owner.Attach(node);
            node.ParentCollection = this;
            // Add the child.
            node.PreviousNode = LastNode;
            if (LastNode == null)
            {
                Debug.Assert(FirstNode == null);
                FirstNode = node;
                LastNode = node;
            }
            else
            {
                LastNode.NextNode = node;
                LastNode = node;
            }
            _Count++;
        }

        /// <summary>
        /// Appends a node to the head of the collection.
        /// </summary>
        /// <param name="node">The node to be added.</param>
        public void AddFirst(TNode node)
        {
            if (FirstNode == null) Add(node);
            else InsertBefore(FirstNode, node);
        }

        /// <summary>
        /// Adds nodes directly from source collection and clears source collection.
        /// </summary>
        internal void AddFrom(NodeCollection<TNode> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Count == 0) return;
            foreach (var node in source)
            {
                node.ParentNode = _Owner;
            }
            if (LastNode == null)
            {
                Debug.Assert(FirstNode == null);
                FirstNode = source.FirstNode;
                LastNode = source.LastNode;
            }
            else
            {
                LastNode.NextNode = source.FirstNode;
                source.FirstNode = LastNode;
                LastNode = source.LastNode;
            }
            _Count += source._Count;
            source.FirstNode = source.LastNode = null;
            source._Count = 0;
        }

        public void Clear()
        {
            Node node = FirstNode;
            while (node != null)
            {
                var nextNode = node.NextNode;
                node.Remove();
                node = nextNode;
            }
            FirstNode = LastNode = null;
            _Count = 0;
        }

        public bool Contains(TNode item)
        {
            if (item == null) return false;
            return ((IEnumerable<TNode>)this).Contains(item);
        }

        public void CopyTo(TNode[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException(nameof(arrayIndex));
            foreach (var node in this)
            {
                array[arrayIndex] = node;
                arrayIndex++;
            }
        }

        public int Count => _Count;

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
            newNode.ParentCollection = this;
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
            newNode.ParentCollection = this;
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
        /// Returns a reversed sequence of the collection items.
        /// </summary>
        public IEnumerable<TNode> Reverse()
        {
            var node = LastNode;
            while (node != null)
            {
                yield return node;
                node = (TNode)node.PreviousNode;
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

        /// <summary>
        /// Please use <see cref="Node.Remove"/> instead.
        /// </summary>
        bool ICollection<TNode>.Remove(TNode item)
        {
            return ((INodeCollection)this).Remove(item);
        }

        bool ICollection<TNode>.IsReadOnly => false;

        void INodeCollection.InsertBefore(Node node, Node newNode)
        {
            InsertBefore((TNode)node, (TNode)newNode);
        }

        void INodeCollection.InsertAfter(Node node, Node newNode)
        {
            InsertBefore((TNode)node, (TNode)newNode);
        }

        bool INodeCollection.Remove(Node item)
        {
            if (item.ParentCollection != this) return false;
            Debug.Assert(item.ParentNode == _Owner);
            item.ParentCollection = null;
            _Owner.Detach(item);
            if (item == FirstNode) FirstNode = (TNode)item.NextNode;
            if (item == LastNode) LastNode = (TNode)item.PreviousNode;
            item.PreviousNode = item.NextNode = null;
            _Count--;
            return true;
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
                    Current = (TNode)Current.NextNode;
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
    }
}

