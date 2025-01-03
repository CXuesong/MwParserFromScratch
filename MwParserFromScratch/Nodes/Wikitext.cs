using System.Diagnostics;
using System.Text.RegularExpressions;
using MwParserFromScratch.Rendering;

namespace MwParserFromScratch.Nodes;

/// <summary>
/// A multiline wikitext block.
/// </summary>
public class Wikitext : Node
{
    public Wikitext(string plainTextContent) : this(new Paragraph(new PlainText(plainTextContent)))
    {
    }

    public Wikitext(params InlineNode[] inlines) : this((IEnumerable<InlineNode>)inlines)
    {
    }

    public Wikitext(IEnumerable<InlineNode> inlines) : this(new Paragraph(inlines))
    {
    }

    public Wikitext(params LineNode[] lines) : this((IEnumerable<LineNode>)lines)
    {
    }

    public Wikitext(IEnumerable<LineNode> lines)
    {
        Lines = new NodeCollection<LineNode>(this);
        if (lines != null) Lines.Add(lines);
    }

    public Wikitext() : this((IEnumerable<LineNode>)null)
    {
    }

    public NodeCollection<LineNode> Lines { get; }

    /// <summary>
    /// Enumerates the children of this node.
    /// </summary>
    public override IEnumerable<Node> EnumChildren() => Lines;

    /// <inheritdoc/>
    protected override Node CloneCore()
    {
        var n = new Wikitext(Lines);
        return n;
    }

    /// <summary>
    /// Gets the wikitext representation of this node.
    /// </summary>
    public override string ToString() => string.Join("\n", Lines);

    /// <inheritdoc />
    internal override void RenderAsPlainText(PlainTextNodeRenderer renderer)
    {
        var isFirst = true;
        foreach (var line in Lines)
        {
            if (isFirst)
                isFirst = false;
            else
                renderer.OutputBuilder.Append('\n');
            renderer.RenderNode(line);
        }
    }
}

public interface IInlineContainer
{
    /// <summary>
    /// Content of the inline container.
    /// </summary>
    NodeCollection<InlineNode> Inlines { get; }
}

public static class InlineContainerExtensions
{

    /// <summary>
    /// Append a <see cref="PlainText"/> node to the beginning of the paragraph.
    /// </summary>
    /// <param name="text">The text to be inserted.</param>
    /// <returns>Either the new <see cref="PlainText"/> node inserted, or the existing <see cref="PlainText"/> at the beginning of the paragraph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
    public static PlainText Prepend(this IInlineContainer container, string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var pt = container.Inlines.FirstNode as PlainText;
        if (pt == null) container.Inlines.AddFirst(pt = new PlainText());
        pt.Content = text + pt.Content;
        return pt;
    }

    /// <summary>
    /// Append a <see cref="PlainText"/> node to the end of the paragraph.
    /// </summary>
    /// <param name="text">The text to be inserted.</param>
    /// <returns>Either the new <see cref="PlainText"/> node inserted, or the existing <see cref="PlainText"/> at the end of the paragraph.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
    public static PlainText Append(this IInlineContainer container, string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var pt = container.Inlines.LastNode as PlainText;
        if (pt == null) container.Inlines.Add(pt = new PlainText());
        pt.Content += text;
        return pt;
    }

    internal static PlainText AppendWithLineInfo(this IInlineContainer container, string text, int lineNumber1, int linePosition1,
        int lineNumber2, int linePosition2)
    {
        Debug.Assert(container != null);
        Debug.Assert(text != null);
        Debug.Assert(lineNumber1 >= 0);
        Debug.Assert(linePosition1 >= 0);
        Debug.Assert(lineNumber2 >= 0);
        Debug.Assert(linePosition2 >= 0);
        Debug.Assert(lineNumber1 < lineNumber2 || lineNumber1 == lineNumber2 && linePosition1 <= linePosition2);
        if (container.Inlines.LastNode is PlainText pt)
        {
            if (text.Length == 0) return pt; // ExtendLineInfo won't accept (0)
            pt.Content += text;
            pt.ExtendLineInfo(lineNumber2, linePosition2);
        }
        else
        {
            container.Inlines.Add(pt = new PlainText(text));
            pt.SetLineInfo(lineNumber1, linePosition1, lineNumber2, linePosition2);
        }
        ((Node)container).ExtendLineInfo(lineNumber2, linePosition2);
        return pt;
    }

}

/// <summary>
/// A single-line (or multi-line) RUN.
/// </summary>
/// <remarks>
/// In some cases (e.g. the text of WIKILINK or the caption of TABLE), line-breaks are
/// allowed, but they will not be treated as paragraph breaks.
/// </remarks>
public class Run : Node, IInlineContainer
{
    public Run() : this((IEnumerable<InlineNode>?)null)
    {
    }

    public Run(params InlineNode[] nodes) : this((IEnumerable<InlineNode>)nodes)
    {
    }

    public Run(IEnumerable<InlineNode>? nodes)
    {
        Inlines = new NodeCollection<InlineNode>(this);
        if (nodes != null) Inlines.Add(nodes);
    }

    public NodeCollection<InlineNode> Inlines { get; }

    public override IEnumerable<Node> EnumChildren() => Inlines;

    protected override Node CloneCore() => new Run(Inlines);

    /// <inheritdoc />
    internal override void RenderAsPlainText(PlainTextNodeRenderer renderer)
    {
        foreach (var inline in Inlines)
        {
            renderer.RenderNode(inline);
        }
    }

    public override string ToString()
    {
        return string.Join(null, Inlines);
    }
}

/// <summary>
/// Represents nodes that should be written in a stand-alone block of lines in WIKITEXT.
/// </summary>
public abstract class LineNode : Node
{

    public LineNode()
    {
    }

    public LineNode(IEnumerable<InlineNode> nodes)
    {
    }

}

public abstract class InlineContainerLineNode : LineNode, IInlineContainer
{
    public InlineContainerLineNode() : this((IEnumerable<InlineNode>?)null)
    {
    }

    public InlineContainerLineNode(params InlineNode[] nodes) : this((IEnumerable<InlineNode>)nodes)
    {
    }

    public InlineContainerLineNode(IEnumerable<InlineNode> nodes)
    {
        Inlines = new NodeCollection<InlineNode>(this);
        if (nodes != null) Inlines.Add(nodes);
    }

    public NodeCollection<InlineNode> Inlines { get; }

    public override IEnumerable<Node> EnumChildren() => Inlines;

    protected override Node CloneCore() => new Run(Inlines);

    /// <inheritdoc />
    internal override void RenderAsPlainText(PlainTextNodeRenderer renderer)
    {
        foreach (var inline in Inlines)
        {
            renderer.RenderNode(inline);
        }
    }

    public override string ToString()
    {
        return string.Join(null, Inlines);
    }
}

public class ListItem : InlineContainerLineNode
{
    public ListItem()
    {
    }

    public ListItem(params InlineNode[] nodes) : base(nodes)
    {
    }

    public ListItem(IEnumerable<InlineNode> nodes) : base(nodes)
    {
    }

    /// <summary>
    /// Prefix of the item.
    /// </summary>
    /// <remarks>The prefix consists of one or more *#:;, or is simply a space. For HR, the prefix is at least 4 dashes.</remarks>
    public required string Prefix { get; set; }

    protected override Node CloneCore()
    {
        var n = new ListItem(Inlines) { Prefix = Prefix };
        return n;
    }

    /// <summary>
    /// Gets the wikitext representation of the node.
    /// </summary>

    public override string ToString()
    {
        return Prefix + string.Join(null, Inlines);
    }
}

public class Heading : InlineContainerLineNode
{
    private int _Level;
    private Run? _Suffix;

    public Heading()
    {
    }

    public Heading(params InlineNode[] nodes) : base(nodes)
    {
    }

    public Heading(IEnumerable<InlineNode> nodes) : base(nodes)
    {
    }


    /// <summary>
    /// Heading level.
    /// </summary>
    /// <value>
    /// The level of the heading, which equals to the number of
    /// equal signs (=) before or after the heading text.
    /// The value is between 1 and 6.
    /// </value>
    public int Level
    {
        get => _Level;
        set
        {
            if (value < 1 || value > 6)
                throw new ArgumentOutOfRangeException(nameof(value));
            _Level = value;
        }
    }

    /// <summary>
    /// The text after the heading expression.
    /// E.g. <c>&lt;!--comment--&gt;</c> in <c>=== abc === &lt;!--comment--&gt;</c>.
    /// </summary>
    public Run? Suffix
    {
        get => _Suffix;
        set => Attach(ref _Suffix, value);
    }

    /// <inheritdoc />
    public override IEnumerable<Node> EnumChildren()
    {
        foreach (var il in Inlines)
        {
            yield return il;
        }
        if (_Suffix != null) yield return _Suffix;
    }

    protected override Node CloneCore()
    {
        var n = new Heading(Inlines) { Level = Level, Suffix = Suffix };
        return n;
    }

    public override string ToString()
    {
        var bar = new string('=', Level);
        return bar + string.Join(null, Inlines) + bar + Suffix;
    }
}

public class Paragraph : InlineContainerLineNode
{

    public Paragraph()
    {
    }

    public Paragraph(params InlineNode[] nodes) : base(nodes)
    {
    }

    public Paragraph(IEnumerable<InlineNode> nodes) : base(nodes)
    {
    }

    private static readonly Regex paragraphCloseMatcher = new Regex(@"\n\s*$");

    /// <summary>
    /// Whether to remove one trailing new-line, if possible.
    /// </summary>
    /// <remarks>
    /// <para>There should be 2 new-line characters (\n\n) after a paragraph. But if the next line
    /// is LIST_ITEM or HEADING, use 1 new-line character to end a paragraph is possible.</para>
    /// <para>For the last paragraph in the <see cref="Wikitext"/>, the expected number of new-line
    /// characters decreases by 1. That is, 1 for normal, 0 for compact.</para>
    /// <para>This property is <c>false</c> only if the last node of the paragraph is
    /// <see cref="PlainText"/>, and it ends with \n\s*. </para>
    /// </remarks>
    public bool Compact
    {
        get
        {
            var pt = Inlines.LastNode as PlainText;
            if (pt == null) return true;
            return !paragraphCloseMatcher.IsMatch(pt.Content);
        }
    }

    protected override Node CloneCore()
    {
        var n = new Paragraph(Inlines);
        return n;
    }

    public override string ToString()
    {
        return string.Join(null, Inlines);
    }

}
