using ObservableUtils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace TVTComment.Model
{
    /// <summary>
    /// 現在視聴中の番組やチャンネル情報についての管理をする
    /// </summary>
    class ChannelInformationModule
    {
        private readonly IPCModule ipc;

        private readonly ObservableValue<DateTime?> currentTime = new ObservableValue<DateTime?>();
        private readonly ObservableCollection<ChannelInfo> channelList = new ObservableCollection<ChannelInfo>();
        private readonly ObservableValue<ChannelInfo> currentChannel = new ObservableValue<ChannelInfo>();
        private readonly ObservableValue<EventInfo> currentEvent = new ObservableValue<EventInfo>();

        /// <summary>
        /// 現在再生中の番組の時刻
        /// <see cref="SynchronizationContext"/>上から参照する必要がある
        /// </summary>
        public ReadOnlyObservableValue<DateTime?> CurrentTime { get; }
        /// <summary>
        /// 現在選択可能なチャンネルのリスト
        /// <see cref="SynchronizationContext"/>上から参照する必要がある
        /// </summary>
        public ReadOnlyObservableCollection<ChannelInfo> ChannelList { get; }
        /// <summary>
        /// 現在視聴中のチャンネル
        /// <see cref="SynchronizationContext"/>上から参照する必要がある
        /// </summary>
        public ReadOnlyObservableValue<ChannelInfo> CurrentChannel { get; }
        /// <summary>
        /// 現在視聴中の番組
        /// <see cref="SynchronizationContext"/>上から参照する必要がある
        /// </summary>
        public ReadOnlyObservableValue<EventInfo> CurrentEvent { get; }

        /// <summary>
        /// コンストラクタの引数で指定した<seealso cref="IPCModule"/>の<seealso cref="IPCModule.MessageReceivedSynchronizationContext"/>と同じ
        /// </summary>
        public SynchronizationContext SynchronizationContext => ipc.MessageReceivedSynchronizationContext;

        public ChannelInformationModule(IPCModule ipc)
        {
            CurrentTime = new ReadOnlyObservableValue<DateTime?>(currentTime);
            ChannelList = new ReadOnlyObservableCollection<ChannelInfo>(channelList);
            CurrentChannel = new ReadOnlyObservableValue<ChannelInfo>(currentChannel);
            CurrentEvent = new ReadOnlyObservableValue<EventInfo>(currentEvent);

            this.ipc = ipc;
            ipc.MessageReceived += Ipc_MessageReceived;
        }

        public async void SetCurrentChannel(ChannelInfo channel)
        {
            IPC.IPCMessage.ChannelSelectIPCMessage msg = new IPC.IPCMessage.ChannelSelectIPCMessage { SpaceIndex = channel.SpaceIndex, ChannelIndex = channel.ChannelIndex, ServiceId = channel.ServiceId };
            await ipc.Send(msg);
        }

        private void Ipc_MessageReceived(IPC.IPCMessage.IIPCMessage message)
        {
            if (message is IPC.IPCMessage.ChannelListIPCMessage chlistmsg)
            {
                //選択できるチャンネルリストを伝えるメッセージ
                channelList.Clear();
                channelList.AddRange(chlistmsg.ChannelList);
            }
            else if (message is IPC.IPCMessage.CurrentChannelIPCMessage curchmsg)
            {
                //現在のチャンネル・番組情報を伝えるメッセージ
                //NID,TSID,SIDがすべて同じならチャンネルは同じで変わってないと判定する
                if (this.currentChannel.Value == null || (this.currentChannel.Value.NetworkId != curchmsg.Channel.NetworkId ||
                    this.currentChannel.Value.TransportStreamId != curchmsg.Channel.TransportStreamId || this.currentChannel.Value.ServiceId != curchmsg.Channel.ServiceId))
                {
                    this.currentChannel.Value = curchmsg.Channel;
                }

                var currentChannel = ChannelList.Select((ch, idx) => new { Channel = ch, Index = idx }).FirstOrDefault((x) =>
                     curchmsg.Channel.NetworkId == x.Channel.NetworkId && curchmsg.Channel.TransportStreamId == x.Channel.TransportStreamId &&
                     curchmsg.Channel.ServiceId == x.Channel.ServiceId);

                //チャンネルリスト内に同じチャンネルがあればそれを今回のメッセージで得たインスタンスに置き換える
                if (currentChannel != null)
                    channelList[currentChannel.Index] = this.currentChannel.Value;

                currentEvent.Value = curchmsg.Event;
            }
            else if (message is IPC.IPCMessage.TimeIPCMessage timemsg)
            {
                //TOTを伝えるメッセージ
                //精度は1秒単位で値が変わるごとに来る
                currentTime.Value = timemsg.Time;
            }
        }
    }
}
