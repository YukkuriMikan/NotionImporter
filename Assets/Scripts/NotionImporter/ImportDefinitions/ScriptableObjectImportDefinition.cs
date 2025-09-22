using System;
using NotionImporter.Functions.SubFunction.ScriptableObjects;

namespace NotionImporter {

	[Serializable]
	public class ScriptableObjectImportDefinition : ImportDefinitionBase {

		public override string definitionType {
			get {
				return "ScriptableObject";
			}
		}

		/// <summary> 対象のスクリプタブルオブジェクトの型文字列 </summary>
		public string        targetScriptableObject;

		/// <summary> グループ化するキーのプロパティID </summary>
		public string keyProperty;

		/// <summary> インポート時にフィルタリングを行うか？ </summary>
		public bool useKeyFiltering;

		/// <summary> マッピングモード、基本的には配列かどうか </summary>
		public MappingMode   mappingMode;

		/// <summary> 配列モード時のターゲットとなる配列型 </summary>
		public TypeItem      targetFieldType;

		/// <summary> 配列モード時のターゲットとなる配列のフィールド名 </summary>
		public string        targetFieldName;

		/// <summary> マッピング対象 </summary>
		public MappingData[] mappingData;

	}

}