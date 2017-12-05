using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TVTComment.Model.NichanUtils
{
    /// <summary>
    /// 2chthreads.txtのパーサ
    /// </summary>
    static class ThreadSettingFileParser
    {
        public class BoardEntriesAndThreadMappingRuleEntries
        {
            public IEnumerable<BoardEntry> BoardEntries { get; }
            public IEnumerable<ThreadMappingRuleEntry> ThreadMappingRuleEntries { get; }
            public BoardEntriesAndThreadMappingRuleEntries(IEnumerable<BoardEntry> boardEntries, IEnumerable<ThreadMappingRuleEntry> threadMappingRuleEntries)
            {
                BoardEntries = boardEntries;
                ThreadMappingRuleEntries = threadMappingRuleEntries;
            }
        }

        public static BoardEntriesAndThreadMappingRuleEntries Parse(string filePath)
        {
            List<BoardEntry> boards = new List<BoardEntry>();
            List<ThreadMappingRuleEntry> threadMapping = new List<ThreadMappingRuleEntry>();

            using (var reader = new StreamReader(filePath))
            {
                Utils.SimpleCsvReader.ReadSectionedByLine(reader, (section, cols) =>
                 {
                     if (section == "boards")
                     {
                         if (cols.Length != 4 && cols.Length!=3) return true;

                         boards.Add(new BoardEntry(cols[0],cols[1], new Uri(cols[2]), cols.Length==4 && !string.IsNullOrWhiteSpace(cols[3]) ? cols[3].Split(' '):null));
                     }
                     else if (section == "threadmapping")
                     {
                         if (cols.Length != 4 && cols.Length != 3) return true;

                         ThreadMappingRuleTarget target;
                         switch (cols[0])
                         {
                             case "flags": target = ThreadMappingRuleTarget.Flags; break;
                             case "nsid": target = ThreadMappingRuleTarget.NSId; break;
                             case "nid": target = ThreadMappingRuleTarget.NId; break;
                             default: return true;
                         }
                         threadMapping.Add(new ThreadMappingRuleEntry(target, Utils.PrefixedIntegerParser.ParseToUInt32(cols[1]), cols[2], 
                             cols.Length == 4 && !string.IsNullOrWhiteSpace(cols[3]) ? cols[3].Split(' ') : null));
                     }
                     return true;
                 }, new char[] { '\t' });
            }

            return new BoardEntriesAndThreadMappingRuleEntries(boards, threadMapping);
        }
    }
}
