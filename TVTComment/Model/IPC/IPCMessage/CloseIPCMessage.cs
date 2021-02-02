using System.Collections.Generic;

namespace TVTComment.Model.IPC.IPCMessage
{
    class CloseIPCMessage : IIPCMessage
    {
        public string MessageName => "Close";
        public IEnumerable<string> Encode()
        {
            return System.Array.Empty<string>();
        }
        public void Decode(IEnumerable<string> content)
        {
        }
    }
}
