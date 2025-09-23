using System;
using System.Reflection;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	/// <summary>型マッピングに使用する情報を保持します。</summary>
	public class MappingItem {

		public bool doMaching = true;      // マッピング対象かどうか
		public bool isArray = false;       // 配列を対象とするか
		public bool isList = false;        // リストを対象とするか
		public string fieldName;           // 対象フィールド名
		public Type fieldType;             // フィールドの型
		public FieldInfo fieldInfo;        // フィールド情報
		public FieldInfo[] innerFieldInfo; // ネストされたフィールド情報

		public NotionProperty[] targetProperties; // 対象のNotionプロパティ一覧

		public int propertyIndex; // 選択されているプロパティインデックス

	}

}
