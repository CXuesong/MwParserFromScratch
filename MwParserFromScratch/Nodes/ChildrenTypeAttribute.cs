using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwParserFromScratch.Nodes
{
    /// <summary>
    /// Specifies a supported child node type of the derived class of <see cref="ContainerNode"/> .
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ChildrenTypeAttribute : Attribute
    {
        public ChildrenTypeAttribute(Type childrenType)
        {
            if (childrenType == null) throw new ArgumentNullException(nameof(childrenType));
            ChildrenType = childrenType;
        }

        public Type ChildrenType { get; }
    }
}
