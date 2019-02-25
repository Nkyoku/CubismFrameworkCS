using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace CubismFramework
{
    [DataContract]
    public class CubismPoseJson
    {
        [DataMember]
        public string Type;

        [DataMember]
        public double FadeInTime;
        
        [DataMember]
        public GroupItem[][] Groups;
        
        [DataContract]
        public class GroupItem
        {
            [DataMember]
            public string Id;

            [DataMember]
            public string[] Link;

            [OnDeserializing]
            internal void OnDeserializing(StreamingContext context)
            {
                Link = new string[0];
            }
        }

        [OnDeserializing]
        internal void OnDeserializing(StreamingContext context)
        {
            FadeInTime = double.NaN;
            Groups = new GroupItem[0][];
        }

        /// <summary>
        /// ストリームからデシリアライズする。
        /// </summary>
        /// <param name="stream">変換するストリーム</param>
        /// <returns>変換されたオブジェクト</returns>
        static public CubismPoseJson Create(Stream stream)
        {
            var serializer_settings = new DataContractJsonSerializerSettings();
            serializer_settings.UseSimpleDictionaryFormat = true;
            var serializer = new DataContractJsonSerializer(typeof(CubismPoseJson), serializer_settings);
            return (CubismPoseJson)serializer.ReadObject(stream);
        }
    }
}
