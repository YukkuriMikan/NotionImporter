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
		/// <summary>機能の配列。機能を追加する場合はここに追加</summary>
		private IMainFunction[] m_functions = {
			new CreateImportDefinition(),
		};

		/// <summary>現在のステータス文言</summary>
		private string m_currentStatusString;

		/// <summary>Notion接続管理</summary>
		private NotionConnector m_connector;
		#endregion

		#region Properties
		/// <summary>現在のステータス</summary>
		public string CurrentStatusString {
			get => m_currentStatusString;
			set {
				// UI表示用のプレフィックスを付与し、ログにも出力する
				m_currentStatusString = $"ステータス: {value}";

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

			// 定義フォルダの存在確認。無ければ作成してプロジェクトビューを更新
			if (!Directory.Exists(NotionImporterParameters.DefinitionFilePath)) {
				AssetDatabase.CreateFolder(NotionImporterParameters.BasePath, NotionImporterParameters.DefinitionDirectoryName);
				AssetDatabase.Refresh();
			}

			window.titleContent = new GUIContent(NotionImporterParameters.WindowTitle, icon);
			window.Show();
		}

		/// <summary>インポート用メニューを再生成</summary>
		[MenuItem(NotionImporterParameters.PROGRAM_ID + "/RefreshMenu", false, 0)]
		public static void RefreshImportMenu() {
			// 非同期の再構築を投げっぱなしで実行
			ImportMenu.RefreshImportMenu().Forget();
		}
		#endregion

		#region Unity Callbacks
		/// <summary>エディタウィンドウの描画</summary>
		private void OnGUI() {
			#region ツールバー描画
			DrawToolBar();
			#endregion

			// 遅延初期化（OnGUIは複数回呼ばれるため、nullチェックで1度だけ生成）
			if (m_connector == null) {
				m_connector = new NotionConnector(this);
			}

			// APIキーが設定済みであれば初回接続を行う（必要時のみ実行される想定）
			m_connector.InitialConnect();

			DrawHeader();

			if (m_connector.IsConnected) {
				// 機能タブの描画（現状は最初の機能のみ使用）
				m_functions[0].DrawFunction(this, m_connector.ImporterSettings);
			}

			DrawFooter();
		}
		#endregion

		#region GUI Draw Methods
		/// <summary>ツールバー描画（定義ファイルの読込等）</summary>
		private void DrawToolBar() {
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
				if (GUILayout.Button("読込", EditorStyles.toolbarButton, GUILayout.Width(100))) {
					// 定義ファイルを選択
					var filePath = EditorUtility.OpenFilePanelWithFilters(
						"定義ファイルを開く",
						NotionImporterParameters.DefinitionFilePath,
						new[] { "インポート定義", "json" }
					);

					if (string.IsNullOrWhiteSpace(filePath)) {
						Debug.Log("読込をキャンセルしました");
						return;
					}

					// 選択ファイルの相対パスから「ディレクトリ名 = 型名のサフィックス」を推定
					// NotionImporter.{DirectoryName} を完全型名として解決する
					var defTypeName = "NotionImporter." + Path.GetDirectoryName(Path.GetRelativePath(NotionImporterParameters.DefinitionFilePath, filePath));
					var defType = Type.GetType(defTypeName);

					// 対応するサブ機能（Exporter/Importer）を型で特定
					var targetSubFunction = m_functions[0].SubFunctions.FirstOrDefault(func => func.ExportFileType == defType);

					// 該当サブ機能のインデックスを取得（-1 なら未対応）
					var subFunctionIndex = m_functions[0].SubFunctions.IndexOf(targetSubFunction);
					
					if (subFunctionIndex < 0) {
						EditorUtility.DisplayDialog("エラー", "ファイルに対応した実装が見つかりませんでした" + Environment.NewLine + defTypeName, "OK");
						return;
					}
					
					// 対象サブ機能を選択状態にして、ファイル内容を読込
					m_functions[0].SelectedSubFunctionIndex = subFunctionIndex;

					var json = File.ReadAllText(filePath);
					targetSubFunction.ReadFile(m_connector.ImporterSettings, json);
				}
			}
		}

		/// <summary>ヘッダー（APIキー入力と再接続）</summary>
		private void DrawHeader() {
			using (new GUILayout.HorizontalScope()) {
				// APIキー入力欄（即時反映）
				m_connector.ImporterSettings.apiKey = EditorGUILayout.TextField("Notion APIキー", m_connector.ImporterSettings.apiKey);

				if (GUILayout.Button("更新", GUILayout.Width(50))) {
					// 明示的に再接続を要求（認証情報が変わった際に使用）
					m_connector.ForceConnect();
				}
			}
		}

		/// <summary>フッター（ステータス表示のみ）</summary>
		private void DrawFooter() {
			using (new EditorGUILayout.HorizontalScope()) {
				// 現在のステータスを左寄せで表示
				EditorGUILayout.LabelField(CurrentStatusString);
			}
		}
		#endregion

		#region Nested types
		/// <summary>GUIのStyle定義</summary>
		public static class Styles {
			/// <summary>タブ風ボタンのスタイル</summary>
			public static readonly GUIStyle TabButtonStyle = "LargeButton";

			/// <summary>タブボタンサイズ（Fixed固定）</summary>
			// GUI.ToolbarButtonSize.FitToContentsも設定可能
			public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed;
		}
		#endregion
	}
}