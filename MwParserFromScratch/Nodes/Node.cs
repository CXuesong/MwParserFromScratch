using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Represents the abstract concept of a node in the syntax tree.
    /// </summary>
    public abstract class Node : IWikitextLineInfo, IWikitextSpanInfo
    {
        private object _Annotation;

        #region Annotations

        /// <summary>
        /// Adds an object to the annotation list of this <see cref="Node"/>.
        /// </summary>
        /// <param name="annotation">The annotation to add.</param>
        public void AddAnnotation(object annotation)
        {
            if (annotation == null) throw new ArgumentNullException(nameof(annotation));
            if (_Annotation == null)
            {
                if (!(annotation is List<object>))
                {
                    _Annotation = annotation;
                    return;
                }
            }
            var list = _Annotation as List<object>;
            if (list == null)
            {
                list = new List<object>(2);
                if (_Annotation != null) list.Add(_Annotation);
                _Annotation = list;
            }
            list.Add(annotation);
        }

        /// <summary>
        /// Returns the first annotation object of the specified type from the list of annotations
        /// of this <see cref="Node"/>.
        /// </summary>
        /// <param name="type">The type of the annotation to retrieve.</param>
        /// <returns>
        /// The first matching annotation object, or null
        /// if no annotation is the specified type.
        /// </returns>
        public object Annotation(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (_Annotation == null) return null;
            var ti = type.GetTypeInfo();
            var list = _Annotation as List<object>;
            if (list != null)
                return list.FirstOrDefault(i => ti.IsAssignableFrom(i.GetType().GetTypeInfo()));
            if (ti.IsAssignableFrom(_Annotation.GetType().GetTypeInfo()))
                return _Annotation;
            return null;
        }

        /// <summary>
        /// Returns the first annotation object of the specified type from the list of annotations
        /// of this <see cref="Node"/>.
        /// </summary>
        /// <typeparam name="T">The type of the annotation to retrieve.</typeparam>
        /// <returns>
        /// The first matching annotation object, or null if no annotation
        /// is the specified type.
        /// </returns>
        public T Annotation<T>() where T : class
        {
            if (_Annotation == null) return null;
            var list = _Annotation as List<object>;
            if (list == null) return _Annotation as T;
            return list.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Returns an enumerable collection of annotations of the specified type
        /// for this <see cref="Node"/>.
        /// </summary>
        /// <param name="type">The type of the annotations to retrieve.</param>
        /// <returns>An enumerable collection of annotations for this XObject.</returns>
        public IEnumerable<object> Annotations(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var ti = type.GetTypeInfo();
            var list = _Annotation as List<object>;
            if (list != null)
                return list.Where(i => ti.IsAssignableFrom(i.GetType().GetTypeInfo()));
            if (ti.IsAssignableFrom(_Annotation.GetType().GetTypeInfo()))
                return Utility.Singleton(_Annotation);
            return Enumerable.Empty<object>();
        }

        /// <summary>
        /// Returns an enumerable collection of annotations of the specified type
        /// for this <see cref="XObject"/>.
        /// </summary>
        /// <typeparam name="T">The type of the annotations to retrieve.</typeparam>
        /// <returns>An enumerable collection of annotations for this XObject.</returns>
        public IEnumerable<T> Annotations<T>() where T : class
        {
            var list = _Annotation as List<object>;
            if (list == null)
            {
                var a = _Annotation as T;
                return a == null ? Enumerable.Empty<T>() : Utility.Singleton(a);
            }
            return list.OfType<T>();
        }

        /// <summary>
        /// Removes the annotations of the specified type from this <see cref="Node"/>.
        /// </summary>
        /// <param name="type">The type of annotations to remove.</param>
        public void RemoveAnnotations(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var ti = type.GetTypeInfo();
            var list = _Annotation as List<object>;
            if (list != null)
            {
                list.RemoveAll(i => ti.IsAssignableFrom(i.GetType().GetTypeInfo()));
            }
            else
            {
                if (ti.IsAssignableFrom(_Annotation.GetType().GetTypeInfo()))
                    _Annotation = null;
            }
        }

        /// <summary>
        /// Removes the annotations of the specified type from this <see cref="Node"/>.
        /// </summary>
        /// <typeparam name="T">The type of annotations to remove.</typeparam>
        public void RemoveAnnotations<T>()
        {
            var list = _Annotation as List<object>;
            if (list != null)
            {
                list.RemoveAll(i => i is T);
            }
            else
            {
                if (_Annotation is T) _Annotation = null;
            }
        }

        #endregion

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

        #region IWikitextLineInfo

        /// <inheritdoc />
        int IWikitextLineInfo.LineNumber => Annotation<LineInfoAnnotation>()?.LineNumber ?? 0;

        /// <inheritdoc />
        int IWikitextLineInfo.LinePosition => Annotation<LineInfoAnnotation>()?.LinePosition ?? 0;

        /// <inheritdoc />
        bool IWikitextLineInfo.HasLineInfo() => Annotation<LineInfoAnnotation>() != null;

        internal void SetLineInfo(int lineNumber, int linePosition, int start, int length)
        {
            Debug.Assert(lineNumber > 0);
            Debug.Assert(linePosition > 0);
            Debug.Assert(start >= 0);
            Debug.Assert(length >= 0);
            AddAnnotation(new LineInfoAnnotation(lineNumber, linePosition, start, length));
        }

        internal void SetLineInfo(Node node)
        {
            Debug.Assert(node != null);
            var annotation = node.Annotation<LineInfoAnnotation>();
            AddAnnotation(annotation);
        }

        internal void ExtendLineInfo(int extendingLength)
        {
            Debug.Assert(extendingLength > 0);
            var lineInfo = Annotation<LineInfoAnnotation>();
            Debug.Assert(lineInfo != null);
            lineInfo.Length += extendingLength;
        }

        #endregion

        #region IWikitextSpanInfo

        /// <inheritdoc />
        int IWikitextSpanInfo.Start => Annotation<LineInfoAnnotation>()?.Start ?? 0;

        /// <inheritdoc />
        int IWikitextSpanInfo.Length => Annotation<LineInfoAnnotation>()?.Length ?? 0;

        /// <inheritdoc />
        bool IWikitextSpanInfo.HasSpanInfo => Annotation<LineInfoAnnotation>() != null;

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

        /// <summary>
        /// Gets the plain text without the unprintable nodes (e.g. comments, templates).
        /// </summary>
        public string ToPlainText()
        {
            return ToPlainText(NodePlainTextOptions.None);
        }

        /// <summary>
        /// Gets the plain text without the unprintable nodes (e.g. comments, templates).
        /// </summary>
        public abstract string ToPlainText(NodePlainTextOptions options);

        private class LineInfoAnnotation
        {
            internal readonly int LineNumber;
            internal readonly int LinePosition;
            internal readonly int Start;
            internal int Length;        // Changed in ExtendLineInfo()

            public LineInfoAnnotation(int lineNumber, int linePosition, int start, int length)
            {
                LineNumber = lineNumber;
                LinePosition = linePosition;
                Start = start;
                Length = length;
            }
        }
    }

    /// <summary>
    /// Options used in <see cref="Node.ToPlainText(NodePlainTextOptions)"/>.
    /// </summary>
    [Flags]
    public enum NodePlainTextOptions
    {
        /// <summary>
        /// Default behavior.
        /// </summary>
        None = 0,
        /// <summary>
        /// Remove the content of &lt;ref&gt; parser tags.
        /// </summary>
        RemoveRefTags = 1
    }
}
