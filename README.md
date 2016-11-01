# MwParserFromScratch

A .NET Library for parsing wikitext into AST. The repository is still under development, but it can already handle most part of wikitext.

## Usage

After adding reference to this library, import the namespaces

```c#
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
```

Then just pass the text to the parser

```c#
var parser = new WikitextParser();
var text = "Paragraph.\n* Item1\n* Item2\n";
var ast = parser.Parse(text);
```

Now `ast` contains the `Wikitext` instance, the root of AST.

You can also take a look at `ConsoleTestApplication1`, where there're some demos. `SimpleDemo` illustrates how to search and replace in the AST.

```c#
static void SimpleDemo()
{
    // Fill the missing template parameters.
    var parser = new WikitextParser();
    var templateNames = new [] {"Expand section", "Cleanup"};
    var text = @"==Hello==<!--comment-->
{{Expand section|date=2010-10-05}}
{{Cleanup}}
This is a nice '''paragraph'''.
==References==
{{Reflist}}
";
    var ast = parser.Parse(text);
    // Convert the code snippets to nodes
    var dateName = parser.Parse("date");
    var dateValue = parser.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
    // Find and set
    foreach (var t in ast.EnumDescendants().OfType<Template>()
        .Where(t => templateNames.Contains(t?.Name.ToString().Trim())))
    {
        var date = t.Arguments.FirstOrDefault(a => a.Name?.ToString() == "date");
        if (date == null)
        {
            date = new TemplateArgument {Name = dateName};
            t.Arguments.Add(date);
        }
        date.Value = dateValue;
    }
    Console.WriteLine(ast.ToString());
}
```

The console output is as follows

```wiki
==Hello==<!--comment-->
{{Expand section|date=2016-11-02}}
{{Cleanup|date=2016-11-02}}
This is a nice '''paragraph'''.
==References==
{{Reflist}}
```

`ParseAndPrint` can roughly print out the parsed tree. Here's a runtime example

```
Please input the wikitext to parse, use EOF (Ctrl+Z) to accept:
==Hello==
* ''Item1''
* [[Item2]]
---------
<span style="background:red;">test</span>
^Z
Parsed AST
Wikitext             [==Hello==\r\n* ''Item1]
.Paragraph           [==Hello==\r]
..PlainText          [==Hello==\r]
.ListItem            [* ''Item1''\r]
..PlainText          [ ]
..FormatSwitch       ['']
..PlainText          [Item1]
..FormatSwitch       ['']
..PlainText          [\r]
.ListItem            [* [[Item2]]\r]
..PlainText          [ ]
..WikiLink           [[[Item2]]]
...Run               [Item2]
....PlainText        [Item2]
..PlainText          [\r]
.ListItem            [---------\r]
..PlainText          [\r]
.Paragraph           [<span style="backgro]
..HtmlTag            [<span style="backgro]
...TagAttribute      [ style="background:r]
....Run              [style]
.....PlainText       [style]
....Wikitext         [background:red;]
.....Paragraph       [background:red;]
......PlainText      [background:red;]
...Wikitext          [test]
....Paragraph        [test]
.....PlainText       [test]
..PlainText          [\r\n]
```

## That's fine, but where to get wikitext?

You can use MediaWiki API to acquire the wikitext. For .NET programmers, I've made a client, [WikiClientLibrary](https://github.com/CXuesong/WikiClientLibrary), that lies beside this repository. There're also MediaWiki API clients in [API:Client code](https://www.mediawiki.org/wiki/API:Client_code).

## Limitations

*   For now it does not support table syntax, but I'll work on this.
*   Text inside parser tags (rather than normal HTML tags) will not be parsed an will be preserved in `ParserTag.Content`. For certain parser tags (e.g. `<ref>`), You can parse the `Content` again to get the AST.
*   It may handle some pathological cases differently from MediaWiki parser. E.g. `{{{{{arg}}` (See #1).

