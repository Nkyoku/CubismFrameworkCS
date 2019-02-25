using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CubismFramework
{
    // ユーザーデータを格納する構造体
    public struct CubismUserDataNode
    {
        public CubismId Id;
        public string Target;
        public string Value;
    }
    
    public class CubismUserData
    {
        /// <summary>
        /// ユーザーデータが無いものとして作成する。
        /// </summary>
        public CubismUserData()
        {
            Nodes = new CubismUserDataNode[0];
        }

        /// <summary>
        /// ストリームからユーザーデータを読み込む。
        /// </summary>
        /// <param name="stream">読み込むストリーム</param>
        public CubismUserData(Stream stream)
        {
            var json = CubismUserDataJson.Create(stream);
            var node_list = new List<CubismUserDataNode>();
            foreach (var item in json.UserData)
            {
                var node = new CubismUserDataNode();
                node.Id = new CubismId(item.Id);
                node.Target = item.Target;
                node.Value = item.Value;
                node_list.Add(node);
            }
            Nodes = node_list.ToArray();
        }
        
        /// <summary>
        /// ユーザーデータのノード
        /// </summary>
        public CubismUserDataNode[] Nodes;
    }
}
