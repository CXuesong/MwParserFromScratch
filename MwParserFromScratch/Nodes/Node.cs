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
    public abstract class Node : IWikitextLineInfo, IWikitextParsingInfo
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
            if (_Annotation is List<object> list)
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
            if (_Annotation is List<object> list)
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
            if (_Annotation is List<object> list)
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
            if (_Annotation is List<object> list)
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
            Debug.Assert(newNode != null);
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
            if (newValue != null) newValue = Attach(newValue);
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

        internal void SetLineInfo(int lineNumber1, int linePosition1, int lineNumber2, int linePosition2)
        {
            Debug.Assert(lineNumber1 >= 0);
            Debug.Assert(linePosition1 >= 0);
            Debug.Assert(lineNumber2 >= 0);
            Debug.Assert(linePosition2 >= 0);
            Debug.Assert(lineNumber1 < lineNumber2 || lineNumber1 == lineNumber2 && linePosition1 <= linePosition2);
            Debug.Assert(Annotation<LineInfoAnnotation>() == null);
            AddAnnotation(new LineInfoAnnotation(lineNumber1, linePosition1, lineNumber2, linePosition2));
        }

        internal void SetLineInfo(Node node)
        {
            Debug.Assert(node != null);
            var source = node.Annotation<LineInfoAnnotation>();
            Debug.Assert(Annotation<LineInfoAnnotation>() == null);
            AddAnnotation(new LineInfoAnnotation(source.StartLineNumber, source.StartLinePosition, source.EndLineNumber,
                source.EndLinePosition));
        }

        internal void ExtendLineInfo(int lineNumber2, int linePosition2)
        {
            var annotation = Annotation<LineInfoAnnotation>();
            Debug.Assert(annotation != null);
            Debug.Assert(lineNumber2 >= 0);
            Debug.Assert(linePosition2 >= 0);
            // We won't allow the span to shrink.
            Debug.Assert(annotation.StartLineNumber < lineNumber2
                         || annotation.StartLineNumber == lineNumber2 && annotation.StartLinePosition <= linePosition2);
            annotation.EndLineNumber = lineNumber2;
            annotation.EndLinePosition = linePosition2;
        }

        internal void SetInferredClosingMark()
        {
            var ext = Annotation<ExtraParsingAnnotation>();
            if (ext == null)
            {
                ext = new ExtraParsingAnnotation();
                AddAnnotation(ext);
            }
            ext.InferredClosingMark = true;
        }

        /// <inheritdoc />
        int IWikitextLineInfo.StartLineNumber => Annotation<LineInfoAnnotation>()?.StartLineNumber ?? 0;

        /// <inheritdoc />
        int IWikitextLineInfo.StartLinePosition => Annotation<LineInfoAnnotation>()?.StartLinePosition ?? 0;

        /// <inheritdoc />
        int IWikitextLineInfo.EndLineNumber => Annotation<LineInfoAnnotation>()?.EndLineNumber ?? 0;

        /// <inheritdoc />
        int IWikitextLineInfo.EndLinePosition => Annotation<LineInfoAnnotation>()?.EndLinePosition ?? 0;

        /// <inheritdoc />
        bool IWikitextLineInfo.HasLineInfo => Annotation<LineInfoAnnotation>() != null;

        /// <inheritdoc />
        bool IWikitextParsingInfo.InferredClosingMark => Annotation<ExtraParsingAnnotation>()?.InferredClosingMark ?? false;

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

        private static void DefaultNodePlainTextFormatter(Node node, StringBuilder builder)
        {
            node.ToPlainTextCore(builder, DefaultNodePlainTextFormatter);
        }

        /// <inheritdoc cref="ToPlainText(NodePlainTextFormatter)"/>
        public string ToPlainText()
        {
            return ToPlainText((NodePlainTextFormatter) null);
        }

        /// <summary>
        /// Gets the plain text without the unprintable nodes (e.g. comments, templates), with customized formatter.
        /// </summary>
        /// <param name="formatter">The formatter delegate used to format the <strong>child</strong> nodes, or <c>null</c> to use default formatter.</param>
        public string ToPlainText(NodePlainTextFormatter formatter)
        {
            var sb = new StringBuilder();
            ToPlainText(sb, formatter);
            return sb.ToString();
        }

        /// <inheritdoc cref="ToPlainTextCore"/>
        public void ToPlainText(StringBuilder builder)
        {
            ToPlainText(builder, null);
        }

        /// <summary>
        /// Gets the plain text without the unprintable nodes (e.g. comments, templates), with customized formatter.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> used to receive the content.</param>
        /// <param name="formatter">The formatter delegate used to format the <strong>child</strong> nodes, or <c>null</c> to use default formatter.</param>
        public void ToPlainText(StringBuilder builder, NodePlainTextFormatter formatter)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            ToPlainTextCore(builder, formatter ?? DefaultNodePlainTextFormatter);
        }

        internal virtual void ToPlainTextCore(StringBuilder builder, NodePlainTextFormatter formatter)
        {
            // The default implementation is to write nothing.
            Debug.Assert(builder != null);
            Debug.Assert(formatter != null);
        }

        private class LineInfoAnnotation
        {
            internal readonly int StartLineNumber;
            internal readonly int StartLinePosition;
            internal int EndLineNumber;
            internal int EndLinePosition;        // Changed in ExtendLineInfo()

            public LineInfoAnnotation(int startLineNumber, int startLinePosition, int endLineNumber, int endLinePosition)
            {
                StartLineNumber = startLineNumber;
                StartLinePosition = startLinePosition;
                EndLineNumber = endLineNumber;
                EndLinePosition = endLinePosition;
            }
        }

        private class ExtraParsingAnnotation
        {
            internal bool InferredClosingMark = false;
        }

    }

    /// <summary>
    /// Formats the specified <see cref="Node"/> into <c>string</c>.
    /// </summary>
    /// <param name="node">The node to be formatted.</param>
    /// <param name="builder"><see cref="StringBuilder"/> the formatted plaintext should be appended into.</param>
    /// <remarks>
    /// <param>A default <see cref="NodePlainTextFormatter"/> implementation is to call <c>node.ToPlainText(builder)</c> directly.</param>
    /// </remarks>
    public delegate void NodePlainTextFormatter(Node node, StringBuilder builder);

}
