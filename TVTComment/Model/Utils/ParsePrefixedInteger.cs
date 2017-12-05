using System;

namespace TVTComment.Model.Utils
{
    static class PrefixedIntegerParser
    {
        public static bool TryParseToUInt16(string s,out ushort result)
        {
            if (s.StartsWith("0x") || s.StartsWith("0X"))
                return ushort.TryParse(s.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out result);
            else
                return ushort.TryParse(s, out result);
        }

        public static bool TryParseToUInt32(string s, out uint result)
        {
            if (s.StartsWith("0x") || s.StartsWith("0X"))
                return uint.TryParse(s.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier, null, out result);
            else
                return uint.TryParse(s, out result);
        }

        public static ushort ParseToUInt16(string s)
        {
            ushort ret;
            if (!TryParseToUInt16(s, out ret))
                throw new FormatException($"Cannot parse \"{s}\" to UInt16");
            return ret;
        }

        public static uint ParseToUInt32(string s)
        {
            uint ret;
            if (!TryParseToUInt32(s, out ret))
                throw new FormatException($"Cannot parse \"{s}\" to UInt32");
            return ret;
        }
    }
}
