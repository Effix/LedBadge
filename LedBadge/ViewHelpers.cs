using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace LedBadge
{
    /// <summary>
    /// Helper for exposing enums to drop down lists
    /// </summary>
    class EnumerationExtension: MarkupExtension
    {
        public EnumerationExtension(Type enumType)
        {
            Type = enumType;
        }

        public Type Type { get; private set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(Type);
        }
    }
}
