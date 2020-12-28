using System.Runtime.Serialization;

namespace TVTComment.Model.NiconicoUtils
{
    [DataContract]
    public class NewNiconicoTrendJson
    {
        [DataMember]
        public Datum[] data { get; set; }
    }


    [DataContract]
    public class Datum
    {
        [DataMember]
        public string socialGroupId { get; set; }
        [DataMember]
        public int commentCount { get; set; }
    }

}
