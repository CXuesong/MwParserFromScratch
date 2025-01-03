#nullable enable

using System;
using System.Text;
using MwParserFromScratch.Nodes;

namespace MwParserFromScratch.Rendering;

/// <summary>
/// Provides methods to recursively render wikitext nodes into plain-text content.
/// </summary>
/// <remarks>The instance methods of this class are <em>not</em> thread-safe.</remarks>
public class PlainTextNodeRenderer
{

    /// <summary>
    /// When <see cref="RenderNode(Node)"/> is being invoked,
    /// gets the string builder to receive the rendered node content in plain text.
    /// </summary>
    /// <remarks>
    /// The value of this property will be the <c>builder</c> passed to the <see cref="RenderNode(StringBuilder,Node)"/> method.
    /// </remarks>
    protected internal StringBuilder OutputBuilder { get; private set; } = null!;

    /// <summary>
    /// Recursively renders the specified wikitext node into plain text, writing the content into a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="builder">the string builder to receive the rendered plain text. Content will be appended at the end of the string builder.</param>
    /// <param name="node">the root wikitext node.</param>
    /// <exception cref="ArgumentNullException">either <paramref name="builder"/> or <paramref name="node"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// caller is attempting to access the current renderer instance concurrently.
    /// Note that the instance methods of this class are <em>not</em> thread-safe.
    /// </exception>
    /// <remarks>
    /// Derived classes of this type should override <see cref="RenderNode(Node)"/> instead.
    /// </remarks>
    public void RenderNode(StringBuilder builder, Node node)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (node == null) throw new ArgumentNullException(nameof(node));

        if (OutputBuilder != null)
            // This class is NOT thread-safe.
            throw new InvalidOperationException("Detected concurrent access to the same PlainTextNodeRenderer instance.");

        try
        {
            OutputBuilder = builder;

            // Access root node.
            RenderNode(node);
        }
        finally
        {
            OutputBuilder = null!;
        }
    }

    /// <summary>
    /// When overridden in the derived class, renders the specified wikitext node into its plain text representation.
    /// </summary>
    /// <param name="node">the node to render.</param>
    /// <remarks>
    /// Implementation should write the rendered plain text content into <see cref="OutputBuilder"/>.
    /// </remarks>
    protected internal virtual void RenderNode(Node node)
    {
        node.RenderAsPlainText(this);
    }

}
