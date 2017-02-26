# MwParserFromScratch

A .NET Library for parsing wikitext into AST. The repository is still under development, but it can already handle most part of wikitext.

## Usage

This package is now on NuGet. You may install the package using the following command in the Package Management Console

```
Install-Package CXuesong.MW.MwParserFromScratch -Pre
```

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
    // Fills the missing template parameters.
    var parser = new WikitextParser();
    var templateNames = new [] {"Expand section", "Cleanup"};
    var text = @"==Hello==<!--comment-->
{{Expand section|
date=2010-10-05
}}
{{Cleanup}}
This is a nice '''paragraph'''.
==References==
{{Reflist}}
";
    var ast = parser.Parse(text);
    // Convert the code snippets to nodes
    var dateName = parser.Parse("date");
    var dateValue = parser.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
    Console.WriteLine("Issues:");
    // Search and set
    foreach (var t in ast.EnumDescendants().OfType<Template>()
        .Where(t => templateNames.Contains(MwParserUtility.NormalizeTemplateArgumentName(t.Name))))
    {
        // Get the argument by name.
        var date = t.Arguments["date"];
        if (date != null)
        {
            // To print the wikitext instead of user-friendly text, use ToString()
            Console.WriteLine("{0} ({1})", t.Name.ToPlainText(), date.Value.ToPlainText());
        }
        // Update/Add the argument
        t.Arguments.SetValue(dateName, dateValue);
    }
    Console.WriteLine();
    Console.WriteLine("Wikitext:");
    Console.WriteLine(ast.ToString());
}
```

The console output is as follows

```wiki
Issues:
Expand section (2010-10-05)

Wikitext:
==Hello==<!--comment-->
{{Expand section|
  date=2017-02-26}}
{{Cleanup|date=2017-02-26}}
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

There's also a simple demo for fetching and parsing without the dependency of WikiClientLibrary in `ConsoleTestApplication1`, like this

```c#
/// <summary>
/// Fetches a page from en Wikipedia, and parses it.
/// </summary>
private static Wikitext FetchAndParse(string title)
{
    if (title == null) throw new ArgumentNullException(nameof(title));
    const string EndPointUrl = "https://en.wikipedia.org/w/api.php";
    var client = new HttpClient();
    var requestContent = new Dictionary<string, string>
    {
        {"format", "json"},
        {"action", "query"},
        {"prop", "revisions"},
        {"rvlimit", "1"},
        {"rvprop", "content"},
        {"titles", title}
    };
    var response = client.PostAsync(EndPointUrl, new FormUrlEncodedContent(requestContent)).Result;
    var root = JObject.Parse(response.Content.ReadAsStringAsync().Result);
    var content = (string) root["query"]["pages"].Children<JProperty>().First().Value["revisions"][0]["*"];
    var parser = new WikitextParser();
    return parser.Parse(content);
}
```

You may need `Newtonsoft.Json` NuGet package to parse JSON.

## Limitations

*   For now it does not support table syntax, but I'll work on this.
*   Text inside parser tags (rather than normal HTML tags) will not be parsed an will be preserved in `ParserTag.Content`. For certain parser tags (e.g. `<ref>`), You can parse the `Content` again to get the AST.
*   It may handle some pathological cases differently from MediaWiki parser. E.g. `{{{{{arg}}` (See Issue #1).

