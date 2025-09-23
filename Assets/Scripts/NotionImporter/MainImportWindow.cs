using Cysharp.Threading.Tasks; // UniTask拡張（Forget等）
using NotionImporter.Functions; // インポート機能のIF/実装
using System; // Type, Environment.NewLine
using System.IO; // File/Directory/Path
using System.Linq; // LINQ拡張
using UnityEditor; // EditorWindow, MenuItem 等
using UnityEngine; // GUI, Debug, Texture2D

namespace NotionImporter {
	/// <summary>Notionインポータのメインウィンドウ</summary>
	public class MainImportWindow : EditorWindow {

		#region Fields
                private IMainFunction[] m_functions = { // 機能の配列。機能を追加する場合はここに追加
                        new CreateImportDefinition(),
                };

                private string m_currentStatusString; // 現在のステータス文言

                private NotionConnector m_connector; // Notion接続管理
		#endregion

		#region Properties
		/// <summary>現在のステータス</summary>
		public string CurrentStatusString {
			get => m_currentStatusString;
			set {
				m_currentStatusString = $"ステータス: {value}"; // UI表示用のプレフィックスを付与し、ログにも出力する

				Debug.Log($"NotionImporter: {m_currentStatusString}");
			}
		}
		#endregion

		#region MenuItems
		/// <summary>ウィンドウの生成</summary>
		[MenuItem(NotionImporterParameters.PROGRAM_ID + "/CreateDefinition", false, -1)]
		public static void CreateWindow() {
			var window = GetWindow<MainImportWindow>();

			var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(NotionImporterParameters.IconPath);

			if (!Directory.Exists(NotionImporterParameters.DefinitionFilePath)) { // 定義フォルダの存在確認。無ければ作成してプロジェクトビューを更新
				AssetDatabase.CreateFolder(NotionImporterParameters.BasePath, NotionImporterParameters.DefinitionDirectoryName);
				AssetDatabase.Refresh();
			}

			window.titleContent = new GUIContent(NotionImporterParameters.WindowTitle, icon);
			window.Show();
		}

		/// <summary>インポート用メニューを再生成</summary>
		[MenuItem(NotionImporterParameters.PROGRAM_ID + "/RefreshMenu", false, 0)]
		public static void RefreshImportMenu() {
			ImportMenu.RefreshImportMenu().Forget(); // 非同期の再構築を投げっぱなしで実行
		}
		#endregion

		#region Unity Callbacks
		/// <summary>エディタウィンドウの描画</summary>
		private void OnGUI() {
			#region ツールバー描画
			DrawToolBar();
			#endregion

			if (m_connector == null) { // 遅延初期化（OnGUIは複数回呼ばれるため、nullチェックで1度だけ生成）
				m_connector = new NotionConnector(this);
			}

			m_connector.InitialConnect(); // APIキーが設定済みであれば初回接続を行う（必要時のみ実行される想定）

			DrawHeader();

			if (m_connector.IsConnected) {
				m_functions[0].DrawFunction(this, m_connector.ImporterSettings); // 機能タブの描画（現状は最初の機能のみ使用）
			}

			DrawFooter();
		}
		#endregion

		#region GUI Draw Methods
		/// <summary>ツールバー描画（定義ファイルの読込等）</summary>
		private void DrawToolBar() {
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
				if (GUILayout.Button("読込", EditorStyles.toolbarButton, GUILayout.Width(100))) {
					var filePath = EditorUtility.OpenFilePanelWithFilters( // 定義ファイルを選択
						"定義ファイルを開く",
						NotionImporterParameters.DefinitionFilePath,
						new[] { "インポート定義", "json" }
					);

					if (string.IsNullOrWhiteSpace(filePath)) {
						Debug.Log("読込をキャンセルしました");
						return;
					}

                                        var defTypeName = "NotionImporter." + Path.GetDirectoryName(Path.GetRelativePath(NotionImporterParameters.DefinitionFilePath, filePath)); // 選択ファイルの相対パスからディレクトリ名を型サフィックスとして扱い完全名を構築する
					var defType = Type.GetType(defTypeName);

					var targetSubFunction = m_functions[0].SubFunctions.FirstOrDefault(func => func.ExportFileType == defType); // 対応するサブ機能（Exporter/Importer）を型で特定

					var subFunctionIndex = m_functions[0].SubFunctions.IndexOf(targetSubFunction); // 該当サブ機能のインデックスを取得（-1 なら未対応）
					
					if (subFunctionIndex < 0) {
						EditorUtility.DisplayDialog("エラー", "ファイルに対応した実装が見つかりませんでした" + Environment.NewLine + defTypeName, "OK");
						return;
					}
					
					m_functions[0].SelectedSubFunctionIndex = subFunctionIndex; // 対象サブ機能を選択状態にして、ファイル内容を読込

					var json = File.ReadAllText(filePath);
					targetSubFunction.ReadFile(m_connector.ImporterSettings, json);
				}
			}
		}

		/// <summary>ヘッダー（APIキー入力と再接続）</summary>
		private void DrawHeader() {
			using (new GUILayout.HorizontalScope()) {
				m_connector.ImporterSettings.apiKey = EditorGUILayout.TextField("Notion APIキー", m_connector.ImporterSettings.apiKey); // APIキー入力欄（即時反映）

				if (GUILayout.Button("更新", GUILayout.Width(50))) {
					m_connector.ForceConnect(); // 明示的に再接続を要求（認証情報が変わった際に使用）
				}
			}
		}

		/// <summary>フッター（ステータス表示のみ）</summary>
		private void DrawFooter() {
			using (new EditorGUILayout.HorizontalScope()) {
				EditorGUILayout.LabelField(CurrentStatusString); // 現在のステータスを左寄せで表示
			}
		}
		#endregion

		#region Nested types
		/// <summary>GUIのStyle定義</summary>
		public static class Styles {
                        public static readonly GUIStyle TabButtonStyle = "LargeButton"; // タブ風ボタンのスタイル

                        public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed; // タブボタンサイズ（Fixed固定）で GUI.ToolbarButtonSize.FitToContents も指定可能
		}
		#endregion
	}
}