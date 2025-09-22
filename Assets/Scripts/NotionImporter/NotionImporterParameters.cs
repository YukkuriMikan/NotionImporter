using System.IO;
using System.Linq;
using UnityEditor;

namespace NotionImporter {

	public static class NotionImporterParameters {

		public const string PROGRAM_ID = "NotionImporter";

		/// <summary> Notionインポータウィンドウのタイトル文字 </summary>
		public static string WindowTitle {
			get {
				return PROGRAM_ID;
			}
		}

		/// <summary> 基準パス、NotionImporterフォルダを使用 </summary>
		public static string BasePath {
			get {
				return AssetDatabase.FindAssets(PROGRAM_ID)
					.Select(str => AssetDatabase.GUIDToAssetPath(str))
					.FirstOrDefault(path => Directory.Exists(path));
			}
		}

		public static string IconPath {
			get {
				return BasePath + $"\\{PROGRAM_ID}.png";
				//アイコンのパス
			}
		}
		public static string SettingFilePath {
			get {
				return BasePath + "\\ImporterSettings.json";
				//インポータの設定ファイル
			}
		}
		public static readonly string DefinitionDirectoryName = "DatabaseDefinitions";        //データベース定義のフォルダ名
		public static string DefinitionFilePath {
			get {
				return BasePath + $"\\{DefinitionDirectoryName}";
				//データベース定義のパス
			}
		}

	}

}
