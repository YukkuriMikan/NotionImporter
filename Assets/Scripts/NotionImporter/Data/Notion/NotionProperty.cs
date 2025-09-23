using System;

namespace NotionImporter {

	/// <summary>Notionのプロパティ情報を保持します。</summary>
	[Serializable]
	public class NotionProperty {

		public string id;           // プロパティのID
		public string name;         // プロパティ名
		public DbPropertyType type; // プロパティの型

	}

}
