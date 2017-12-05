using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    /// <summary>
    /// チャンネル情報
    /// </summary>
    public class ChannelInfo
    {
        /// <summary>
        /// チューニング空間の種類
        /// </summary>
        public enum TuningSpaceType
        {
            /// <summary>
            /// 不明
            /// </summary>
            Unknown,
            /// <summary>
            /// 地上波
            /// </summary>
            Terrestrial,
            /// <summary>
            /// BS
            /// </summary>
            BS,
            /// <summary>
            /// 110度CS
            /// </summary>
            CS
        }

        /// <summary>
        /// チューニング空間のインデックス
        /// </summary>
        public int SpaceIndex { get; set; }

        /// <summary>
        /// チャンネルのインデックス
        /// </summary>
        public int ChannelIndex { get; set; }

        /// <summary>
        /// チューニング空間の種類
        /// </summary>
        public TuningSpaceType TuningSpace { get; set; }

        /// <summary>
        /// リモコンID
        /// </summary>
        public int RemoteControlKeyId { get; set; }

        /// <summary>
        /// ネットワークID(録画ファイルでは0)
        /// </summary>
        public ushort NetworkId { get; set; }

        /// <summary>
        /// トランスポートストリームID(録画ファイルでは0)
        /// </summary>
        public ushort TransportStreamId { get; set; }

        /// <summary>
        /// ネットワーク名(録画ファイルでは空文字)
        /// </summary>
        public string NetworkName { get; set; }

        /// <summary>
        /// トランスポートストリーム名(録画ファイルでは空文字)
        /// </summary>
        public string TransportStreamName { get; set; }

        /// <summary>
        /// チャンネル名
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// サービスID
        /// </summary>
	    public ushort ServiceId { get; set; }

        /// <summary>
        /// サービス名（ChannelListIPCMessageではnull）
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// TVTestの設定で非表示になっているか（CurrentChannelIPCMessageでは常にfalse）
        /// </summary>
        public bool Hidden { get; set; }
    }
}
