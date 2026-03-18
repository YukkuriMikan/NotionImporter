using System;
using NotionImporter.Functions.SubFunction.ScriptableObjects;

namespace NotionImporter {

	/// <summary>ScriptableObject出力時のソート方向を表します。</summary>
	public enum SortOrder {

		Ascending,  // 昇順で並べ替える
		Descending, // 降順で並べ替える

	}

	/// <summary>配列/リスト出力時の単一ソート条件です。</summary>
	[Serializable]
	public class SortCondition {

		public string sortKey; // ソート対象のフィールド名

		public SortOrder sortOrder = SortOrder.Ascending; // ソート順

	}

	/// <summary>ScriptableObject向けのインポート定義を保持します。</summary>
	[Serializable]
	public class ScriptableObjectImportDefinition : ImportDefinitionBase {

		/// <summary>ScriptableObject用の定義タイプを返します。</summary>
		public override string definitionType
			=> "ScriptableObject"; // 定義タイプの固定文字列を返す

		public string targetScriptableObject; // 対象となるスクリプタブルオブジェクトの型名

		public string keyProperty; // グループ化に使用するプロパティID

		public bool useKeyFiltering; // フィルタリングを行うかどうか

		public MappingMode mappingMode; // マッピングモード（配列などの種別）

		public TypeItem targetFieldType; // 配列モード時のターゲット型

		public string targetFieldName; // 配列モード時のターゲットフィールド名

		public MappingData[] mappingData; // マッピング定義の一覧

		public string sortKey; // 互換性維持用の旧ソートキー

		public SortOrder sortOrder = SortOrder.Ascending; // 互換性維持用の旧ソート順

		public SortCondition[] sortConditions; // 複数ソート条件（新仕様）

	}

}
