using System;
using System.Collections.Generic;
using System.Text;

namespace TVTComment.Model.NiconicoUtils
{
    class UnOfficialNiconicoLogSettings
    {
        public int UnOfficialApiTimeOut { get; set; } = 30;
        public int UnOfficialApiGetInterval { get; set; } = 1;
        public string UnOfficialApiUri { get; set; } = "https://jikkyo.tsukumijima.net/api/kakolog/jk{jkId}?starttime={start}&endtime={end}&format=xml";
    }
}
