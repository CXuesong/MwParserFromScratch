using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Represents a collection of nodes.
    /// The children are maintained as a bi-directional linked list.
    /// </summary>
    public class NodeCollection : IEnumerable<Node>
    {
        internal NodeCollection()
        {
            
        }

        /// <summary>
        /// The first node.
        /// </summary>
        public Node FirstNode { get; internal set; }

        /// <summary>
        /// The last node.
        /// </summary>
        public Node LastNode { get; internal set; }

        /// <summary>
        /// 返回一个循环访问集合的枚举器。
        /// </summary>
        /// <returns>
        /// 可用于循环访问集合的 <see cref="T:System.Collections.Generic.IEnumerator`1"/>。
        /// </returns>
        public IEnumerator<Node> GetEnumerator()
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

        private sealed class MyEnumerator : IEnumerator<Node>
        {
            private readonly NodeCollection _Owner;
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
                    Current = Current.NextNode;
                }
                return Current != null;
            }

            public void Reset()
            {
                needsReset = true;
            }

            public Node Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {

            }

            public MyEnumerator(NodeCollection owner)
            {
                Debug.Assert(owner != null);
                _Owner = owner;
            }
        }
    }
}
