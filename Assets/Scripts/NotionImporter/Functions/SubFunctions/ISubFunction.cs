using System;

namespace NotionImporter.Functions.SubFunction {

	/// <summary> Notionインポータのサブ機能インターフェイス </summary>
	public interface ISubFunction {
		public IMainFunction ParentFunction { get; set; }

		/// <summary> 機能名 </summary>
		public string FunctionName { get; }

		/// <summary> 出力する定義ファイルの型 </summary>
		public Type ExportFileType { get; }

		/// <summary> Notionインポータのサブ機能を描画する </summary>
		public void DrawFunction(NotionImporterSettings settings);

		/// <summary> 当該機能のファイルを出力する </summary>
		public void CreateFile();

		public void ReadFile(NotionImporterSettings settings, string json);

	}

}