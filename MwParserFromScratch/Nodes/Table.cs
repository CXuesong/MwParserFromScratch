using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Wikitable.
    /// </summary>
    public class Table : LineNode
    {
        private TableCaption _Caption;

        public Table()
        {
            Attributes = new TagAttributeCollection(this);
            Rows = new NodeCollection<TableRow>(this);
        }

        public TagAttributeCollection Attributes { get; }

        public TableCaption Caption
        {
            get { return _Caption; }
            set { Attach(ref _Caption, value); }
        }

        public NodeCollection<TableRow> Rows { get; }

        public override IEnumerable<Node> EnumChildren()
        {
            foreach (var a in Attributes) yield return a;
            if (_Caption != null) yield return _Caption;
            foreach (var r in Rows) yield return r;
        }

        protected override Node CloneCore()
        {
            var n = new Table {Caption = Caption};
            n.Attributes.Add(Attributes);
            n.Attributes.TrailingWhitespace = Attributes.TrailingWhitespace;
            return n;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{|");
            sb.Append(Attributes);
            sb.AppendLine();
            if (_Caption != null) sb.AppendLine(_Caption.ToString());
            foreach (var r in Rows)
            {
                sb.AppendLine(r.ToString());
            }
            sb.Append("|}");
            return sb.ToString();
        }

        public override string ToPlainText(NodePlainTextOptions options)
        {
            var sb = new StringBuilder();
            if (Caption != null) sb.AppendLine(_Caption.ToPlainText(options));
            var firstRow = true;
            foreach (var r in Rows)
            {
                if (firstRow)
                    firstRow = false;
                else
                    sb.AppendLine();
                sb.Append(r.ToPlainText(options));
            }
            return sb.ToString();
        }
    }

    public abstract class TableContentNode : Node
    {
        private Run _Content;

        public TableContentNode() : this(null)
        {
        }

        public TableContentNode(Run content)
        {
            Attributes = new TagAttributeCollection(this);
            Content = content;
        }

        public TagAttributeCollection Attributes { get; set; }

        public Run Content
        {
            get { return _Content; }
            set { Attach(ref _Content, value); }
        }

        public bool HasAttributePipe
        {
            get { return Attributes.Count > 0 || Attributes.TrailingWhitespace != null; }
            set
            {
                if (Attributes.Count > 0)
                    throw new InvalidOperationException("Cannot remove attribute pipe when attributes are not empty.");
                Attributes.TrailingWhitespace = null;
            }
        }

        public override IEnumerable<Node> EnumChildren()
        {
            foreach (var attr in Attributes) yield return attr;
            if (_Content != null) yield return _Content;
        }

        public override string ToPlainText(NodePlainTextOptions options)
        {
            return Content?.ToPlainText(options);
        }
    }

    public class TableCaption : TableContentNode
    {
        public TableCaption() : base(null)
        {
        }

        public TableCaption(Run content) : base(content)
        {
        }

        protected override Node CloneCore()
        {
            var n = new TableCaption(Content);
            n.Attributes.Add(Attributes);
            n.Attributes.TrailingWhitespace = Attributes.TrailingWhitespace;
            return n;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("|+");
            sb.Append(Attributes);
            if (HasAttributePipe) sb.Append('|');
            sb.Append(Content);
            return sb.ToString();
        }
    }

    public class TableRow : Node
    {

        public TableRow()
        {
            Attributes = new TagAttributeCollection(this);
            Cells = new NodeCollection<TableCell>(this);
        }

        public TagAttributeCollection Attributes { get; }

        public NodeCollection<TableCell> Cells { get; }

        public override IEnumerable<Node> EnumChildren()
        {
            foreach (var attr in Attributes) yield return attr;
            foreach (var cell in Cells) yield return cell;
        }

        protected override Node CloneCore()
        {
            var n = new TableRow();
            n.Attributes.Add(Attributes);
            n.Attributes.TrailingWhitespace = Attributes.TrailingWhitespace;
            n.Cells.Add(Cells);
            return n;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("|-");
            sb.Append(Attributes);
            var isFirst = true;
            sb.Append('\n');
            foreach (var c in Cells)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else if (!c.HasAttributePipe)
                {
                    sb.Append('\n');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public override string ToPlainText(NodePlainTextOptions options)
        {
            return string.Join("\t", Cells.Select(c => c.ToPlainText(options)));
        }
    }

    public class TableCell : TableContentNode
    {

        public TableCell() : base(null)
        {
        }

        public TableCell(Run content) : base(content)
        {
        }

        public bool IsHeader { get; set; }

        protected override Node CloneCore()
        {
            var n = new TableCell(Content) {IsHeader = IsHeader};
            n.Attributes.Add(Attributes);
            n.Attributes.TrailingWhitespace = Attributes.TrailingWhitespace;
            return n;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (HasAttributePipe)
            {
                sb.Append(IsHeader ? '!' : '|');
                sb.Append(Attributes);
            }
            else
            {
                Debug.Assert(Attributes.Count == 0);
            }
            sb.Append(IsHeader ? '!' : '|');
            sb.Append(Content);
            return sb.ToString();
        }

    }

}
