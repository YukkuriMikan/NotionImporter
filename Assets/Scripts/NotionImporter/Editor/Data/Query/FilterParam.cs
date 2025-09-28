using System;

namespace NotionImporter {

	/// <summary>Notion検索のフィルタ条件を保持します。</summary>
	[Serializable]
	public class FilterParam {

		public string value = "database";  // フィルタする値
		public string property = "object"; // フィルタ対象のプロパティ名

	}

}
