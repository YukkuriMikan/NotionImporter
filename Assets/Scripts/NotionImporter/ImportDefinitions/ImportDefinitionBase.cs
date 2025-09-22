using System;

namespace NotionImporter {

	/// <summary> インポートの定義 </summary>
	[Serializable]
	public abstract class ImportDefinitionBase {

		public string definitionName;

		public abstract string definitionType { get; }

		/// <summary> 対象データベース </summary>
		public NotionObject targetDb;

		public string outputPath;

	}

}
