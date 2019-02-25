using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace CubismFramework
{
    [DataContract]
    public class CubismMotionJson
    {
        [DataMember]
        public int Version;

        [DataMember]
        public MetaItem Meta;

        [DataMember]
        public CurveItem[] Curves;

        [DataMember]
        public UserDataItem[] UserData;

        [DataContract]
        public class MetaItem
        {
            [DataMember]
            public double Duration;

            [DataMember]
            public double Fps;

            [DataMember]
            public bool Loop;

            [DataMember]
            public bool AreBeziersRestricted;

            [DataMember]
            public int CurveCount;

            [DataMember]
            public int TotalSegmentCount;

            [DataMember]
            public int TotalPointCount;

            [DataMember]
            public int UserDataCount;

            [DataMember]
            public int TotalUserDataSize;

            [DataMember]
            public double FadeInTime;

            [DataMember]
            public double FadeOutTime;

            [OnDeserializing]
            internal void OnDeserializing(StreamingContext context)
            {
                Duration = -1.0;
                Fps = 15.0;
                FadeInTime = double.NaN;
                FadeOutTime = double.NaN;
            }
        }

        [DataContract]
        public class CurveItem
        {
            [DataMember]
            public string Target;

            [DataMember]
            public string Id;

            [DataMember]
            public double[] Segments;

            [DataMember]
            public double FadeInTime;

            [DataMember]
            public double FadeOutTime;

            [OnDeserializing]
            internal void OnDeserializing(StreamingContext context)
            {
                FadeInTime = double.NaN;
                FadeOutTime = double.NaN;
            }
        }

        [DataContract]
        public class UserDataItem
        {
            [DataMember]
            public double Time;

            [DataMember]
            public string Value;
        }
        
        /// <summary>
        /// ストリームからデシリアライズする。
        /// </summary>
        /// <param name="stream">変換するストリーム</param>
        /// <returns>変換されたオブジェクト</returns>
        static public CubismMotionJson Create(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(CubismMotionJson));
            return (CubismMotionJson)serializer.ReadObject(stream);
        }
    }
}
