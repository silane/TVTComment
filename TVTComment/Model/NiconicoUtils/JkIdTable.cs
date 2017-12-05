using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TVTComment.Model.NiconicoUtils
{
    /// <summary>
    /// <see cref="ChannelEntry"/>とニコニコ実況IDの対応を表す
    /// </summary>
    class JkIdTable
    {
        private enum RuleTarget { Flags,NSId,NId}

        private List<Tuple<RuleTarget,uint,int>> rules=new List<Tuple<RuleTarget, uint, int>>();
        private Dictionary<ChannelEntry, int> tableCache = new Dictionary<ChannelEntry, int>();

        public JkIdTable(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                Utils.SimpleCsvReader.ReadByLine(reader, cols =>
                 {
                     if (cols.Length != 3) return true;
                     RuleTarget target;
                     switch(cols[0])
                     {
                         case "flags":target = RuleTarget.Flags;break;
                         case "nsid":target = RuleTarget.NSId;break;
                         case "nid":target = RuleTarget.NId;break;
                         default:return true;
                     }
                     rules.Add(new Tuple<RuleTarget, uint, int>(target,Utils.PrefixedIntegerParser.ParseToUInt32(cols[1]),int.Parse(cols[2])));
                     return true;
                 }, new char[] { '\t' });
            }
        }

        /// <summary>
        /// 対応する実況IDを返す（対応がなければ0）
        /// </summary>
        public int GetJkId(ChannelEntry channel)
        {
            int ret;
            if (tableCache.TryGetValue(channel, out ret))
                return ret;

            ret = 0;
            foreach (Tuple<RuleTarget, uint, int> rule in rules)
            {
                switch (rule.Item1)
                {
                    case RuleTarget.Flags:
                        if ((channel.Flags & (ChannelFlags)rule.Item2) != 0)
                        {
                            ret = rule.Item3;
                        }
                        break;
                    case RuleTarget.NSId:
                        if (channel.NetworkId == (rule.Item2>>16) && channel.ServiceId == (rule.Item2 & 0xFFFF))
                        {
                            ret = rule.Item3;
                        }
                        break;
                    case RuleTarget.NId:
                        if (channel.NetworkId == rule.Item2)
                        {
                            ret = rule.Item3;
                        }
                        break;
                }
            }

            tableCache.Add(channel, ret);
            return ret;
        }
    }
}
