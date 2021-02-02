using System;
using System.Threading;

namespace TVTComment.Model
{
    /// <summary>
    /// TVTestPlugin側からのユーザーの操作を処理する
    /// </summary>
    class CommandModule : IDisposable
    {
        private readonly IPCModule ipcModule;
        public SynchronizationContext SynchronizationContext { get; }

        public delegate void ShowWindowCommandInvokedEventHandler();
        public event ShowWindowCommandInvokedEventHandler ShowWindowCommandInvoked;

        public CommandModule(IPCModule ipcModule, SynchronizationContext synchronizationContext)
        {
            this.ipcModule = ipcModule;
            SynchronizationContext = synchronizationContext;

            ipcModule.MessageReceived += IpcModule_MessageReceived;
        }

        private void IpcModule_MessageReceived(IPC.IPCMessage.IIPCMessage message)
        {
            if (message as IPC.IPCMessage.CommandIPCMessage == null)
                return;

            switch ((message as IPC.IPCMessage.CommandIPCMessage).CommandId)
            {
                case "ShowWindow":
                    SynchronizationContext.Post(_ => ShowWindowCommandInvoked(), null);
                    break;
            }
        }

        public void Dispose()
        {
            ipcModule.MessageReceived -= IpcModule_MessageReceived;
        }
    }
}
