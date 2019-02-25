using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.IO;

namespace CubismFramework
{
    [DataContract]
    public class CubismModelSettingJson
    {
        [DataMember]
        public int Version;

        [DataMember]
        public GroupItem[] Groups;

        [DataMember]
        public FileReferencesItem FileReferences;
        
        [DataMember]
        public HitAreaItem[] HitAreas;

        [DataMember]
        public Dictionary<string, double> Layout;

        [DataContract]
        public class FileReferencesItem
        {
            [DataMember]
            public string Moc;

            [DataMember]
            public Dictionary<string, MotionItem[]> Motions;

            [DataMember]
            public ExpressionItem[] Expressions;

            [DataMember]
            public string[] Textures;

            [DataMember]
            public string Physics;

            [DataMember]
            public string Pose;
            
            [DataMember]
            public string UserData;
            
            [DataContract]
            [DebuggerDisplay("File={File}, FadeInTime={FadeInTime}, FadeOutTime={FadeOutTime}")]
            public class MotionItem
            {
                [DataMember]
                public string File;

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
            [DebuggerDisplay("Name={Name}, File={File}")]
            public class ExpressionItem
            {
                [DataMember]
                public string Name;

                [DataMember]
                public string File;
            }
        }

        [DataContract]
        [DebuggerDisplay("Target={Target}, Name={Name}")]
        public class GroupItem
        {
            [DataMember]
            public string Target;

            [DataMember]
            public string Name;

            [DataMember]
            public string[] Ids;
        }

        [DataContract]
        [DebuggerDisplay("Name={Name}, Id={Id}")]
        public class HitAreaItem
        {
            [DataMember]
            public string Name;

            [DataMember]
            public string Id;
        }

        /// <summary>
        /// ストリームからデシリアライズする。
        /// </summary>
        /// <param name="stream">変換するストリーム</param>
        /// <returns>変換されたオブジェクト</returns>
        static public CubismModelSettingJson Create(Stream stream)
        {
            var serializer_settings = new DataContractJsonSerializerSettings();
            serializer_settings.UseSimpleDictionaryFormat = true;
            var serializer = new DataContractJsonSerializer(typeof(CubismModelSettingJson), serializer_settings);
            return (CubismModelSettingJson)serializer.ReadObject(stream);
        }
    }
}
