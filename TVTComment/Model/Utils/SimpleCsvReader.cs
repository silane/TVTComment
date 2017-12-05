using System.IO;

namespace TVTComment.Model.Utils
{
    static class SimpleCsvReader
    {
        public delegate bool ReadByLineEventHandler(string[] columns);
        public static void ReadByLine(StreamReader reader,ReadByLineEventHandler handler,char[] separator)
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                string[] cols = line.Split(separator);

                if (!handler(cols))
                    break;
            }
        }

        public delegate bool ReadSectionedByLineEventHandler(string section,string[] columns);
        /// <summary>
        /// iniファイルのようにセクションで区切られたcsvファイルのパーサー
        /// </summary>
        public static void ReadSectionedByLine(StreamReader reader,ReadSectionedByLineEventHandler handler,char[] separator)
        {
            string section=null;
            while(!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                if(line[0]=='[')
                {
                    line=line.Trim();
                    if (line[line.Length - 1] == ']')
                    {
                        section = line.Substring(1, line.Length - 2);
                        continue;
                    }
                }

                string[] cols = line.Split(separator);

                if (!handler(section,cols))
                    break;
            }
        }
    }
}
