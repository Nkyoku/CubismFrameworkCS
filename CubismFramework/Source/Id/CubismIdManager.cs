using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CubismFramework
{
    public class CubismIdManager<IdType> where IdType : CubismId
    {
        /// <summary>
        /// IDマネージャーを作成する。
        /// </summary>
        /// <param name="count">IDの数</param>
        internal CubismIdManager(int count)
        {
            IdList = new IdType[count];
            UnindexedIdList = new List<IdType>();
        }
        
        /// <summary>
        /// 文字列配列からIDマネージャーを作成する。
        /// IDオブジェクトのインデックスは文字列配列のインデックスに等しい。
        /// もし重複した文字列が含まれていた場合でもIDオブジェクトは登録されるがアクセスできなくなる。
        /// </summary>
        /// <param name="name_list">ID名の文字列配列</param>
        /// <param name="factory_method">IDオブジェクトを生成するメソッド</param>
        internal CubismIdManager(string[] name_list, FactoryMethod factory_method)
        {
            int count = name_list.Length;
            IdList = new IdType[count];
            for(int index = 0; index < count; index++)
            {
                IdList[index] = factory_method(name_list[index], index);
            }
            UnindexedIdList = new List<IdType>();
        }
        
        /// <summary>
        /// IDオブジェクトを登録する。
        /// </summary>
        /// <param name="id">登録するID</param>
        internal void RegisterId(IdType id)
        {
            if (id == null)
            {
                throw new ArgumentNullException();
            }
            int index = id.Index;
            if ((0 <= index) && (index < IdList.Length))
            {
                if (IdList[index] == null)
                {
                    IdList[index] = id;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            else if (SupportUnindexedId == true)
            {
                UnindexedIdList.Add(id);
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// インデックスからIDオブジェクトを取得する。
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>登録されているIDあるいはnull</returns>
        public IdType GetId(int index)
        {
            if ((0 <= index) && (index < IdList.Length))
            {
                return IdList[index];
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// ID名からIDオブジェクトを取得する。
        /// </summary>
        /// <param name="name">ID名</param>
        /// <returns>登録されているIDあるいはnull</returns>
        public IdType GetId(string name)
        {
            // インデックスのあるIDから検索する
            IdType id = IdList.FirstOrDefault(item => item.CompareTo(name));
            if (id != null)
            {
                return id;
            }
            if (SupportUnindexedId == true)
            {
                // 見つからなかったのでインデックスのないIDも検索する
                id = UnindexedIdList.FirstOrDefault(x => x.CompareTo(name));
                if (id != null)
                {
                    return id;
                }
            }
            return null;
        }

        /// <summary>
        /// ID名からIDオブジェクトが登録されているかどうか確認する。
        /// </summary>
        /// <param name="name">ID名</param>
        /// <returns>存在するならtrue</returns>
        public bool IsResistered(string name)
        {
            bool result = Array.Exists(IdList, item => item.CompareTo(name));
            if ((result == false) && (SupportUnindexedId == true))
            {
                result = UnindexedIdList.Exists(item => item.CompareTo(name));
            }
            return result;
        }
        
        /// <summary>
        /// インデックスのあるIDオブジェクトの数
        /// </summary>
        public int Count
        {
            get { return IdList.Length; }
        }

        /// <summary>
        /// 登録されているIDオブジェクトのリスト
        /// </summary>
        private IdType[] IdList;

        /// <summary>
        /// インデックスのないIDオブジェクトのリスト
        /// </summary>
        private List<IdType> UnindexedIdList;

        /// <summary>
        /// インデックスのないIDオブジェクトの取り扱いをサポートする。
        /// </summary>
        internal bool SupportUnindexedId = false;

        /// <summary>
        /// IdTypeを生成するメソッド
        /// </summary>
        /// <param name="name">ID名</param>
        /// <param name="index">インデックス</param>
        /// <returns></returns>
        internal delegate IdType FactoryMethod(string name, int index);
    }
}
