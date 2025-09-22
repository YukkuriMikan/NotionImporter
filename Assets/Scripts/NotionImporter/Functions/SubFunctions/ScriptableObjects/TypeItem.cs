using System;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	/// <summary> リスト用の型アイテムクラス </summary>
	[Serializable]
	public class TypeItem {

		public string typeName;
		public string typeFullName;
		public string assemblyName;

		/// <summary> リフレクションで型を取得する際の文字列(型のフルネーム, アセンブリ名というフォーマット) </summary>
		public string typeString {
			get {
				return $"{typeFullName}, {assemblyName}";
			}
		}

		public Type targetType {
			get {
				return Type.GetType(typeString);
			}
		}

	}

}