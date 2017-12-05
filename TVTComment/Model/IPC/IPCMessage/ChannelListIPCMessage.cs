using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC.IPCMessage
{
    class ChannelListIPCMessage : IIPCMessage
    {
        public string MessageName => "ChannelList";

        public IList<ChannelInfo> ChannelList { get; set; } = new List<ChannelInfo>();

        public void Decode(IEnumerable<string> content)
        {
            ChannelInfo channelInfo=null;
            List<ChannelInfo> channelList = new List<ChannelInfo>();

            try
            {
                foreach (var item in content.Select((item, i) => new { Value = item, Index = i }))
                {
                    switch (item.Index % 11)
                    {
                        case 0:
                            channelInfo = new ChannelInfo();

                            channelInfo.SpaceIndex = int.Parse(item.Value);
                            break;
                        case 1:
                            channelInfo.ChannelIndex = int.Parse(item.Value);
                            break;

                        case 2:
                            switch (item.Value)
                            {
                                case "Unknown":channelInfo.TuningSpace = ChannelInfo.TuningSpaceType.Unknown;break;
                                case "Terrestrial": channelInfo.TuningSpace = ChannelInfo.TuningSpaceType.Terrestrial; break;
                                case "BS": channelInfo.TuningSpace = ChannelInfo.TuningSpaceType.BS; break;
                                case "CS": channelInfo.TuningSpace = ChannelInfo.TuningSpaceType.CS; break;
                                default: throw new IPCMessageDecodeException("ChannelListのTuningSpaceの値が不正です");
                            }
                            break;
                        case 3:
                            channelInfo.RemoteControlKeyId = int.Parse(item.Value);
                            break;
                        case 4:
                            channelInfo.NetworkId = ushort.Parse(item.Value);
                            break;
                        case 5:
                            channelInfo.TransportStreamId = ushort.Parse(item.Value);
                            break;
                        case 6:
                            channelInfo.NetworkName = item.Value;
                            break;
                        case 7:
                            channelInfo.TransportStreamName = item.Value;
                            break;
                        case 8:
                            channelInfo.ChannelName = item.Value;
                            break;
                        case 9:
                            channelInfo.ServiceId = ushort.Parse(item.Value);
                            break;
                        case 10:
                            if (item.Value == "F")
                                channelInfo.Hidden = false;
                            else if (item.Value == "T")
                                channelInfo.Hidden = true;
                            else
                                throw new FormatException();
                            channelList.Add(channelInfo);
                            break;
                    }
                }
            }
            catch(FormatException e)
            {
                throw new IPCMessageDecodeException("ChannelListのフォーマットが異常です", e);
            }

            ChannelList = channelList;
        }

        public IEnumerable<string> Encode()
        {
            throw new NotImplementedException("ChannelListIPCMessage::Encode is not implemented");
        }
    }
}
