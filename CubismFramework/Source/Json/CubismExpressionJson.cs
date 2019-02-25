using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace CubismFramework
{
    [DataContract]
    public class CubismExpressionJson
    {
        [DataMember]
        public string Type;

        [DataMember]
        public double FadeInTime;

        [DataMember]
        public double FadeOutTime;

        [DataMember]
        public ParameterItem[] Parameters;
        
        [DataContract]
        public class ParameterItem
        {
            [DataMember]
            public string Id;

            [DataMember]
            public double Value;

            [DataMember]
            public string Blend;
        }

        [OnDeserializing]
        internal void OnDeserializing(StreamingContext context)
        {
            FadeInTime = 1.0;
            FadeOutTime = 1.0;
        }

        /// <summary>
        /// ストリームからデシリアライズする。
        /// </summary>
        /// <param name="stream">変換するストリーム</param>
        /// <returns>変換されたオブジェクト</returns>
        static public CubismExpressionJson Create(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(CubismExpressionJson));
            return (CubismExpressionJson)serializer.ReadObject(stream);
        }
    }
}
