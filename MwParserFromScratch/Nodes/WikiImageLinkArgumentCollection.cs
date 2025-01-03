using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MwParserFromScratch.Nodes;

/// <summary>
/// Represents a collection of <see cref="WikiImageLinkArgument"/>
/// that can be accessed via argument names.
/// </summary>
public class WikiImageLinkArgumentCollection : NodeCollection<WikiImageLinkArgument>
{

    /// <inheritdoc />
    internal WikiImageLinkArgumentCollection(Node owner) : base(owner)
    {
    }

    private IEnumerable<KeyValuePair<string, WikiImageLinkArgument>> EnumNameArgumentPairs(bool reverse)
    {
        return (reverse ? Reverse() : this).Select(arg =>
            new KeyValuePair<string, WikiImageLinkArgument>(MwParserUtility.NormalizeImageLinkArgumentName(arg.Name), arg));
    }

    /// <summary>
    /// Enumerates the normalized name-<see cref="WikiImageLinkArgument"/> pairs in the collection.
    /// </summary>
    /// <remarks>If there are arguments with duplicate names, they will nonetheless be included in the sequence.</remarks>
    public IEnumerable<KeyValuePair<string, WikiImageLinkArgument>> EnumNameArgumentPairs()
    {
        return EnumNameArgumentPairs(false);
    }

    /// <summary>
    /// Gets an named argument (<c>name=value</c>) with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of argument that will be tested. Leading and trailing white spaces will be ignored. First letter will be normalized.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    /// <returns>A matching <see cref="WikiImageLinkArgument"/> with the specified name, or <c>null</c> if no matching template is found.</returns>
    public WikiImageLinkArgument this[string name]
    {
        get
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            name = MwParserUtility.NormalizeImageLinkArgumentName(name);
            // We want to choose the last matching arguments, if there are multiple choices.
            return EnumNameArgumentPairs(true).FirstOrDefault(p => p.Key == name).Value;
        }
    }

    /// <summary>
    /// Gets the value of <c>link=</c> option, if available.
    /// </summary>
    public Wikitext Link => this["link"]?.Value;

    /// <summary>
    /// Gets the value of <c>alt=</c> option, if available.
    /// </summary>
    public Wikitext Alt => this["alt"]?.Value;

    /// <summary>
    /// Gets the value of <c>page=</c> option, if available.
    /// </summary>
    public Wikitext Page => this["page"]?.Value;

    /// <summary>
    /// Gets the value of <c>class=</c> option, if available.
    /// </summary>
    public Wikitext ClassName => this["class"]?.Value;

    /// <summary>
    /// Gets the value of <c>lang=</c> option, if available.
    /// </summary>
    public Wikitext Lang => this["lang"]?.Value;

    /// <summary>
    /// Gets the image caption, if available.
    /// </summary>
    /// <remarks>The caption of the image is the last unnamed argument that is also after the last named argument.</remarks>
    public Wikitext Caption
    {
        get
        {
            return EnumNameArgumentPairs(true)
                .TakeWhile(p => p.Key == null)
                .FirstOrDefault()
                .Value?.Value;
        }
    }

}
