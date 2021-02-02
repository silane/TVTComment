using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.NichanUtils
{
    class FixedNichanThreadSelector : INichanThreadSelector
    {
        public IEnumerable<string> Uris { get; }

        public FixedNichanThreadSelector(IEnumerable<string> uris)
        {
            Uris = uris;
        }

#pragma warning disable CS1998 // この非同期メソッドには 'await' 演算子がないため、同期的に実行されます。'await' 演算子を使用して非ブロッキング API 呼び出しを待機するか、'await Task.Run(...)' を使用してバックグラウンドのスレッドに対して CPU 主体の処理を実行することを検討してください。
        public async Task<IEnumerable<string>> Get(
#pragma warning restore CS1998 // この非同期メソッドには 'await' 演算子がないため、同期的に実行されます。'await' 演算子を使用して非ブロッキング API 呼び出しを待機するか、'await Task.Run(...)' を使用してバックグラウンドのスレッドに対して CPU 主体の処理を実行することを検討してください。
            ChannelInfo channel, DateTimeOffset time, CancellationToken cancellationToken
        )
        {
            return Uris;
        }
    }
}
