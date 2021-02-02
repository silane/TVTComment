using System.Runtime.Serialization;

namespace TVTComment.Model.NiconicoUtils
{
    [DataContract]
    public class NewNiconicoTrendJson
    {
        [DataMember]
        public Datum[] Data { get; set; }
    }


    [DataContract]
    public class Datum
    {
        [DataMember]
        public string SocialGroupId { get; set; }
        [DataMember]
        public int CommentCount { get; set; }
    }

}
