using System;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

        /// <summary>リスト用の型アイテムクラス</summary>
        [Serializable]
        public class TypeItem {

                public string typeName; // 型名
                public string typeFullName; // フルクラス名
                public string assemblyName; // 所属アセンブリ名

                /// <summary>リフレクションで型を取得する際の文字列(型のフルネーム, アセンブリ名というフォーマット)</summary>
                public string typeString {
                        get {
                                return $"{typeFullName}, {assemblyName}";
                        }
                }

                /// <summary>実際の型情報</summary>
                public Type targetType {
                        get {
                                return Type.GetType(typeString);
			}
		}

	}

}