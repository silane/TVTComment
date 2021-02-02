using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC
{
    //0x1E(Record Separator),0x1F(Unit Separator)文字は伝送データに含めてはならない
    class IPCProtocolStream : IDisposable
    {
        readonly StringBuilder buffer = new StringBuilder();//受信バッファ
        readonly Encoding encoding = new UTF8Encoding(false);
        readonly Decoder decoder;

        public Stream BaseStream { get; }

        public IPCProtocolStream(Stream baseStream)
        {
            BaseStream = baseStream;
            decoder = encoding.GetDecoder();
        }

        public async Task<RawIPCMessage> Read(CancellationToken cancellationToken)
        {
            int sepIdx = buffer.ToString().IndexOf('\u001E');
            while (sepIdx < 0)
            {
                byte[] buf = new byte[2048];
                char[] charBuf = new char[2048];
                int readCount = await BaseStream.ReadAsync(buf.AsMemory(0, buf.Length), cancellationToken).ConfigureAwait(false);
                int readCharCount = decoder.GetChars(buf, 0, readCount, charBuf, 0);

                //int readCount = await reader.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false);
                if (readCount == 0)
                    return null;//ストリームが終端に達した
                sepIdx = Array.IndexOf(charBuf, '\u001E', 0, readCharCount);
                if (sepIdx >= 0)
                {
                    sepIdx += buffer.Length;
                }
                buffer.Append(charBuf, 0, readCharCount);
            }

            string line = buffer.ToString().Substring(0, sepIdx);
            buffer.Remove(0, sepIdx + 1);
            List<string> texts = line.Split('\u001F').ToList();
            return new RawIPCMessage { MessageName = texts[0], Contents = texts.Skip(1) };
        }

        public async Task Write(RawIPCMessage msg, CancellationToken cancellationToken)
        {
            string[] texts = new string[] { msg.MessageName };
            byte[] dataBytes = encoding.GetBytes(string.Join("\u001F", texts.Concat(msg.Contents)) + '\u001E');

            await BaseStream.WriteAsync(dataBytes.AsMemory(0, dataBytes.Length), cancellationToken).ConfigureAwait(false);
            await BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            ((IDisposable)BaseStream).Dispose();
        }
    }
}
