using System.Text;

namespace MwParserFromScratch.Nodes;

public class TagAttributeCollection : NodeCollection<TagAttribute>
{

    private string? _TrailingWhitespace;

    internal TagAttributeCollection(Node owner) : base(owner)
    {
    }

    /// <summary>
    /// The trailing whitespace after the last tag attribute.
    /// </summary>
    /// <exception cref="ArgumentException">The string contains non-white-space characters.</exception>
    public string? TrailingWhitespace
    {
        get { return _TrailingWhitespace; }
        set
        {
            Utility.AssertNullOrWhiteSpace(value);
            _TrailingWhitespace = value;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var attr in this)
            sb.Append(attr);
        sb.Append(_TrailingWhitespace);
        return sb.ToString();
    }

}
