using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC
{
    static class IPCMessageFactory
    {
        public static IPCMessage.IIPCMessage FromRawIPCMessage(RawIPCMessage rawmsg)
        {
            IPCMessage.IIPCMessage ret;
            switch(rawmsg.MessageName)
            {
                case "Chat":
                    ret = new IPCMessage.ChatIPCMessage();
                    break;
                case "ChannelList":
                    ret = new IPCMessage.ChannelListIPCMessage();
                    break;
                case "ChannelSelect":
                    ret = new IPCMessage.ChannelSelectIPCMessage();
                    break;
                case "CurrentChannel":
                    ret = new IPCMessage.CurrentChannelIPCMessage();
                    break;
                case "Time":
                    ret = new IPCMessage.TimeIPCMessage();
                    break;
                case "Close":
                    ret = new IPCMessage.CloseIPCMessage();
                    break;
                case "SetChatOpacity":
                    ret = new IPCMessage.SetChatOpacityIPCMessage();
                    break;
                case "Command":
                    ret = new IPCMessage.CommandIPCMessage();
                    break;
                default:
                    throw new IPCMessage.IPCMessageDecodeException("不明なMessageNameです: "+rawmsg.ToString());
            }
            ret.Decode(rawmsg.Contents);
            return ret;
        }
    }
}
