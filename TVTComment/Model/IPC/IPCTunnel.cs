using System;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.IPC
{
    /// <summary>
    /// 表示側（プラグイン側）とやり取りする通信トンネル
    /// </summary>
    class IPCTunnel:IDisposable
    {
        IPCProtocolStream upStream;
        IPCProtocolStream downStream;
        
        public IPCTunnel(string sendPipeName,string receivePipeName)
        {
            upStream = new IPCProtocolStream(new NamedPipeClientStream(".",sendPipeName,PipeDirection.InOut));
            downStream = new IPCProtocolStream(new NamedPipeClientStream(".", receivePipeName, PipeDirection.InOut));
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            await ((NamedPipeClientStream)upStream.BaseStream).ConnectAsync(cancellationToken);
            await ((NamedPipeClientStream)downStream.BaseStream).ConnectAsync(cancellationToken);
        }

        public async Task Send(IPCMessage.IIPCMessage msg,CancellationToken cancellationToken)
        {
            RawIPCMessage rawMsg = new RawIPCMessage { MessageName = msg.MessageName, Contents = msg.Encode() };
            await upStream.Write(rawMsg,cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// IPCMessageを受信する
        /// </summary>
        /// <returns>受信したIPCMessage</returns>
        /// <exception cref="EndOfStreamException">接続が切断された</exception>
        public async Task<IPCMessage.IIPCMessage> Receive(CancellationToken cancellationToken)
        {
            RawIPCMessage rawmsg;
            rawmsg = await downStream.Read(cancellationToken).ConfigureAwait(false);

            if(rawmsg==null)
            {
                //接続が切れた
                throw new EndOfStreamException("Connection to server was down");
            }
            return IPCMessageFactory.FromRawIPCMessage(rawmsg);
        }

        public void Dispose()
        {
            //IO関係のエラーは無視
            //Connectする前だとInvalidOperationExceptionが出るのでそれも無視
            try
            {
                ((IDisposable)upStream).Dispose();
            }
            catch(IOException) { }
            catch (InvalidOperationException) { }
            try
            {
                ((IDisposable)downStream).Dispose();
            }
            catch (IOException) { }
            catch (InvalidOperationException) { }
        }
    }
}
