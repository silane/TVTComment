using System.IO;
using System.Runtime.Serialization.Json;

namespace TVTComment.Model.NiconicoUtils
{
    public static class TrendJsonUtils
    {
        public static T ToObject<T>(Stream json)
        {
            using (json)
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(json);
            }
        }
    }
}
