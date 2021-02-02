using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVTComment.Model.NiconicoUtils
{
    /// <summary>
    /// <see cref="ChannelEntry"/>とニコニコ生放送IDの対応を表す
    /// </summary>
    class LiveIdTable
    {
        private enum RuleTarget { Flags, NSId, NId }

        private readonly List<Tuple<RuleTarget, uint, string>> rules = new List<Tuple<RuleTarget, uint, string>>();
        private readonly Dictionary<ChannelEntry, string> tableCache = new Dictionary<ChannelEntry, string>();

        public LiveIdTable(string filePath)
        {
            using var reader = new StreamReader(filePath);
            Utils.SimpleCsvReader.ReadByLine(reader, cols =>
            {
                if (cols.Length != 3) return true;
                RuleTarget target;
                switch (cols[0])
                {
                    case "flags": target = RuleTarget.Flags; break;
                    case "nsid": target = RuleTarget.NSId; break;
                    case "nid": target = RuleTarget.NId; break;
                    default: return true;
                }
                rules.Add(new Tuple<RuleTarget, uint, string>(target, Utils.PrefixedIntegerParser.ParseToUInt32(cols[1]), cols[2]));
                return true;
            }, new char[] { '\t' });
        }

        /// <summary>
        /// 対応する生放送IDを返す（対応がなければ空文字）
        /// </summary>
        public string GetLiveId(ChannelEntry channel)
        {
            if (tableCache.TryGetValue(channel, out string ret))
                return ret;

            ret = "";
            foreach (Tuple<RuleTarget, uint, string> rule in rules)
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
                        if (channel.NetworkId == (rule.Item2 >> 16) && channel.ServiceId == (rule.Item2 & 0xFFFF))
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

        /// <summary>
        /// 生放送IDの一覧を返す
        /// </summary>
        public IEnumerable<string> GetLiveIdList()
        {
            return rules.Select(x => x.Item3);
        }
    }
}
