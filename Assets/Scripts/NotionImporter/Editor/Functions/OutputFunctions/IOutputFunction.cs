using System;
using Cysharp.Threading.Tasks;

namespace NotionImporter.Functions.Output {

	public interface IOutputFunction {

		/// <summary> 対応するインポート定義の型 </summary>
		public Type DefinitionType { get; }

		public void DrawFunction(NotionImporterSettings settings);

		/// <summary> ファイルの出力 </summary>
		/// <param name="importDefinition">インポート定義</param>
		/// <param name="props">出力に使用するNotionのページ</param>
		public UniTask OutputFile(string fileName, NotionImporterSettings settings, ImportDefinitionBase importDefinition, NotionObject[] pages);

		public ImportDefinitionBase Deserialize(string json);

	}

}
