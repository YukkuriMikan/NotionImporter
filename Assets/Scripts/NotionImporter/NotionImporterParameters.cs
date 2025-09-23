using System.IO;
using System.Linq;
using UnityEditor;

namespace NotionImporter {

	/// <summary>NotionImporterで使用する共通パラメータを提供します。</summary>
	public static class NotionImporterParameters {

		public const string PROGRAM_ID = "NotionImporter"; // プログラム識別子

		/// <summary>Notionインポータウィンドウのタイトル文字を取得します。</summary>
		public static string WindowTitle => PROGRAM_ID; // ウィンドウタイトルとしてプログラムIDを返す

		/// <summary>基準パスとしてNotionImporterフォルダの場所を取得します。</summary>
		public static string BasePath {
			get {
				return AssetDatabase.FindAssets(PROGRAM_ID) // プロジェクト内から対象フォルダパスを検索
					.Select(str => AssetDatabase.GUIDToAssetPath(str))
					.FirstOrDefault(path => Directory.Exists(path));
			}
		}

		/// <summary>ウィンドウアイコンのパスを取得します。</summary>
		public static string IconPath
			=> BasePath + $"\\{PROGRAM_ID}.png"; // 基準パスにアイコンファイル名を結合

		/// <summary>インポート設定ファイルのパスを取得します。</summary>
		public static string SettingFilePath
			=> BasePath + "\\ImporterSettings.json"; // 基準パスに設定ファイル名を結合

		public static readonly string DefinitionDirectoryName = "DatabaseDefinitions"; // データベース定義を格納するディレクトリ名

		/// <summary>データベース定義フォルダのパスを取得します。</summary>
		public static string DefinitionFilePath
			=> BasePath + $"\\{DefinitionDirectoryName}"; // 基準パスに定義ディレクトリ名を結合

	}

}
