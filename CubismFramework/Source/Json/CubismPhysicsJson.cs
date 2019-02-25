using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace CubismFramework
{
    [DataContract]
    public class CubismPhysicsJson
    {
        [DataMember]
        public int Version;

        [DataMember]
        public MetaItem Meta;
        
        [DataMember]
        public PhysicsSettingItem[] PhysicsSettings;
        
        [DataContract]
        public class MetaItem
        {
            [DataMember]
            public int PhysicsSettingCount;

            [DataMember]
            public int TotalInputCount;

            [DataMember]
            public int TotalOutputCount;

            [DataMember]
            public int VertexCount;

            [DataMember]
            public EffectiveForcesItem EffectiveForces;

            [DataMember]
            public PhysicsDictionaryItem[] PhysicsDictionary;

            [DataContract]
            public class EffectiveForcesItem
            {
                [DataMember]
                public Vector2D Gravity;

                [DataMember]
                public Vector2D Wind;
            }

            [DataContract]
            public class PhysicsDictionaryItem
            {
                [DataMember]
                public string Id;

                [DataMember]
                public string Name;
            }
        }

        [DataContract]
        public class PhysicsSettingItem
        {
            [DataMember]
            public string Id;

            [DataMember]
            public InputItem Input;

            [DataMember]
            public OutputItem Output;

            [DataMember]
            public VertixItem Vertices;

            [DataMember]
            public NormalizationItem Normalization;

            [DataContract]
            public class InputItem
            {
                [DataMember]
                public SourceDestinationItem Source;

                [DataMember]
                public double Weight;

                [DataMember]
                public string Type;

                [DataMember]
                public bool Reflect;
            }

            [DataContract]
            public class OutputItem
            {
                [DataMember]
                public SourceDestinationItem Destination;

                [DataMember]
                public int VertexIndex;

                [DataMember]
                public double Scale;

                [DataMember]
                public double Weight;

                [DataMember]
                public string Type;

                [DataMember]
                public bool Reflect;
            }

            [DataContract]
            public class VertixItem
            {
                [DataMember]
                public Vector2D Position;

                [DataMember]
                public double Mobility;

                [DataMember]
                public double Delay;

                [DataMember]
                public double Acceleration;

                [DataMember]
                public double Radius;
            }

            [DataContract]
            public class NormalizationItem
            {
                [DataMember]
                public MinMaxItem Position;

                [DataMember]
                public MinMaxItem Angle;
            }
            
            [DataContract]
            public class SourceDestinationItem
            {
                [DataMember]
                public string Target;

                [DataMember]
                public string Id;
            }

            [DataContract]
            public class MinMaxItem
            {
                [DataMember]
                public double Minimum;

                [DataMember]
                public double Default;

                [DataMember]
                public double Maximum;
            }
        }
        
        [DataContract]
        public class Vector2D
        {
            [DataMember]
            public double X;

            [DataMember]
            public double Y;
        }
        
        /// <summary>
        /// ストリームからデシリアライズする。
        /// </summary>
        /// <param name="stream">変換するストリーム</param>
        /// <returns>変換されたオブジェクト</returns>
        static public CubismPhysicsJson Create(Stream stream)
        {
            var serializer = new DataContractJsonSerializer(typeof(CubismPhysicsJson));
            return (CubismPhysicsJson)serializer.ReadObject(stream);
        }
    }
}
