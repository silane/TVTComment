using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVTComment.Model
{
    /// <summary>
    /// チャンネルの勢い値データを表す
    /// </summary>
    public interface IForceValueData
    {
        /// <summary>
        /// 指定チャンネルの勢い値を取得する
        /// </summary>
        /// <param name="channelInfo">勢い値を取得したいチャンネル</param>
        /// <returns>勢い値</returns>
        int? GetForceValue(ChannelInfo channelInfo);
    }
}
