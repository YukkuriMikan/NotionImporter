using NotionImporter.Functions.SubFunction;

namespace NotionImporter.Functions {

	/// <summary> Notionインポータの機能インターフェイス </summary>
	public interface IMainFunction {

		public ISubFunction[] SubFunctions { get; }

		public int SelectedSubFunctionIndex { get; set; }

		public string FunctionName { get; }

		public NotionTree NotionTree { get; }

		/// <summary> Notionインポータの機能を描画する </summary>
		public void DrawFunction(MainImportWindow parent, NotionImporterSettings settings);

	}

}