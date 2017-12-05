using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    /// <summary>
    /// TVTestPlugin側からのユーザーの操作を処理する
    /// </summary>
    class CommandModule:IDisposable
    {
        private IPCModule ipcModule;
        public SynchronizationContext SynchronizationContext { get; }

        public delegate void ShowWindowCommandInvokedEventHandler();
        public event ShowWindowCommandInvokedEventHandler ShowWindowCommandInvoked;

        public CommandModule(IPCModule ipcModule, SynchronizationContext synchronizationContext)
        {
            this.ipcModule = ipcModule;
            this.SynchronizationContext = synchronizationContext;

            ipcModule.MessageReceived += ipcModule_MessageReceived;
        }

        private void ipcModule_MessageReceived(IPC.IPCMessage.IIPCMessage message)
        {
            var commandMessage = message as IPC.IPCMessage.CommandIPCMessage;
            if (commandMessage == null)
                return;

            switch(commandMessage.CommandId)
            {
                case "ShowWindow":
                    SynchronizationContext.Post(_ => ShowWindowCommandInvoked(),null);
                    break;
            }
        }

        public void Dispose()
        {
            ipcModule.MessageReceived -= ipcModule_MessageReceived;
        }
    }
}
