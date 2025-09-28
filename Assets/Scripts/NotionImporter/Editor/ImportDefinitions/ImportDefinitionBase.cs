using System;

namespace NotionImporter {

	/// <summary>インポートの定義</summary>
	[Serializable]
	public abstract class ImportDefinitionBase {

		public string definitionName; // インポート定義名

		/// <summary>定義の型名</summary>
		public abstract string definitionType { get; }

		/// <summary>対象データベース</summary>
		public NotionObject targetDb;

		public string outputPath; // 出力先パス

	}

}
