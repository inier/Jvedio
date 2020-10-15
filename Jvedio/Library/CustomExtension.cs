using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Jvedio.StaticVariable;

namespace Jvedio
{
    public static class CustomExtension
    {
        public static string ToJav321(this string ID)
        {
            ID = ID.ToUpper();
            if (Jav321IDDict.ContainsKey(ID))
                return Jav321IDDict[ID];
            else
                return ID;

        }

    }
}
