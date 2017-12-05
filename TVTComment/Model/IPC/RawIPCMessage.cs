using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC
{
    /// <summary>
    /// 生のIPCMessage
    /// </summary>
    /// <remarks>
    /// <see cref="MessageName"/>と<see cref="Contents"/>はrecord separatorとunit separatorを含んではならない
    /// </remarks>
    class RawIPCMessage
    {
        public string MessageName { get; set; }
        public IEnumerable<string> Contents { get; set; }

        public override string ToString()
        {
            return $"MessageName={MessageName},Contents={{{string.Join(" ", Contents)}}}";
        }
    }
}
