using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC.IPCMessage
{
    class CloseIPCMessage:IIPCMessage
    {
        public string MessageName => "Close";
        public IEnumerable<string> Encode()
        {
            return new string[0];
        }
        public void Decode(IEnumerable<string> content)
        {
        }
    }
}
