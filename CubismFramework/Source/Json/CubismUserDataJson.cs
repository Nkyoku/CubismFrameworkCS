using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace CubismFramework
{
    [DataContract]
    public class CubismUserDataJson
    {
        [DataMember]
        public int Version;

        [DataMember]
        public MetaItem Meta;

        [DataMember]
        public UserDataItem[] UserData;
        
        [DataContract]
        public class MetaItem
        {
            [DataMember]
            public int UserDataCount;

            [DataMember]
            public int TotalUserDataSize;
        }

        [DataContract]
        public class UserDataItem
        {
            [DataMember]
            public string Target;

            [DataMember]
            public string Id;

            [DataMember]
            public string Value;
        }

        /// <summary>
        /// ストリームからデシリアライズする。
        /// </summary>
        /// <param name="stream">変換するストリーム</param>
        /// <returns>変換されたオブジェクト</returns>
        static public CubismUserDataJson Create(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(CubismUserDataJson));
            return (CubismUserDataJson)serializer.ReadObject(stream);
        }
    }
}
