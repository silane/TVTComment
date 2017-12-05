using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC.IPCMessage
{
    class CurrentChannelIPCMessage : IIPCMessage
    {
        public ChannelInfo Channel;
        public EventInfo Event;

        public string MessageName => "CurrentChannel";

        public void Decode(IEnumerable<string> content)
        {
            string[] contents = content.ToArray();
            if (contents.Length != 16)
                throw new IPCMessageDecodeException("CurrentChannelのcontentの数が16以外です");

            Channel = new ChannelInfo();
            try
            {
                Channel.SpaceIndex = int.Parse(contents[0]);
                Channel.ChannelIndex = int.Parse(contents[1]);
                Channel.RemoteControlKeyId = int.Parse(contents[2]);

                Channel.NetworkId = ushort.Parse(contents[3]);
                Channel.TransportStreamId = ushort.Parse(contents[4]);
                Channel.ServiceId = ushort.Parse(contents[5]);
                
                Channel.NetworkName = contents[7];
                Channel.TransportStreamName = contents[8];
                Channel.ServiceName = contents[9];
                Channel.ChannelName = contents[10];

                Event = new EventInfo(ushort.Parse(contents[6]), contents[11], contents[12], contents[13],
                    DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeSeconds(long.Parse(contents[14])).DateTime, DateTimeKind.Local),
                    new TimeSpan(0, 0, int.Parse(contents[15])));
            }
            catch(FormatException)
            {
                throw new IPCMessageDecodeException("CurrentChannelのフォーマットが不正です");
            }
        }

        public IEnumerable<string> Encode()
        {
            throw new NotImplementedException("CurrentChannelIPCMessage::Encodeは実装されていません");
        }
    }
}
