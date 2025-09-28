using Cysharp.Threading.Tasks;  // UniTask拡張（Forget等）
using NotionImporter.Functions; // インポート機能のIF/実装
using System;                   // Type, Environment.NewLine
using System.IO;                // File/Directory/Path
using System.Linq;              // LINQ拡張
using UnityEditor;              // EditorWindow, MenuItem 等
using UnityEngine;              // GUI, Debug, Texture2D

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

			if(!Directory.Exists(NotionImporterParameters.DefinitionFilePath)) { // 定義フォルダの存在確認。無ければ作成してプロジェクトビューを更新
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

			if(m_connector == null) { // 遅延初期化（OnGUIは複数回呼ばれるため、nullチェックで1度だけ生成）
				ExecuteWithErrorHandling("Notionコネクタの生成", () => m_connector = new NotionConnector(this));
			}

			if(m_connector != null) {                                                   // コネクタ生成に成功した場合のみ後続処理を実行
				ExecuteWithErrorHandling("初期接続処理", () => m_connector.InitialConnect()); // APIキーが設定済みであれば初回接続を行う（必要時のみ実行される想定）

				DrawHeader();

				if(m_connector.IsConnected) {
					DrawFunctionArea(); // 機能タブの描画（現状は最初の機能のみ使用）
				}
			}

			DrawFooter();
		}
		#endregion

		#region GUI Draw Methods
		/// <summary>ツールバー描画（定義ファイルの読込等）</summary>
		private void DrawToolBar() {
			DrawBlockWithErrorHandling(
				"ツールバー",
				() => {
					using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
						if(GUILayout.Button("読込", EditorStyles.toolbarButton, GUILayout.Width(100))) {
							ExecuteWithErrorHandling("定義ファイルの読込処理", () => {
								var filePath = EditorUtility.OpenFilePanelWithFilters( // 定義ファイルを選択
									"定義ファイルを開く",
									NotionImporterParameters.DefinitionFilePath,
									new[] {
										"インポート定義",
										"json"
									}
									);

								if(string.IsNullOrWhiteSpace(filePath)) {
									Debug.Log("読込をキャンセルしました");
									return;
								}

								if(m_connector?.ImporterSettings == null) { // コネクタ未初期化時は明示的にエラーとして扱う
									throw new InvalidOperationException("Notion接続が初期化される前に読込処理が実行されました。");
								}

								var defTypeName = "NotionImporter." + Path.GetDirectoryName(Path.GetRelativePath(NotionImporterParameters.DefinitionFilePath, filePath)); // 選択ファイルの相対パスからディレクトリ名を型サフィックスとして扱い完全名を構築する
								var defType = Type.GetType(defTypeName);

								var targetSubFunction = m_functions[0].SubFunctions.FirstOrDefault(func => func.ExportFileType == defType); // 対応するサブ機能（Exporter/Importer）を型で特定

								var subFunctionIndex = m_functions[0].SubFunctions.IndexOf(targetSubFunction); // 該当サブ機能のインデックスを取得（-1 なら未対応）

								if(subFunctionIndex < 0) {
									EditorUtility.DisplayDialog("エラー", "ファイルに対応した実装が見つかりませんでした" + Environment.NewLine + defTypeName, "OK");
									return;
								}

								m_functions[0].SelectedSubFunctionIndex = subFunctionIndex; // 対象サブ機能を選択状態にして、ファイル内容を読込

								var json = File.ReadAllText(filePath);
								targetSubFunction.ReadFile(m_connector.ImporterSettings, json);
							});
						}
					}
				},
				message => {
					using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
						EditorGUILayout.LabelField(message, EditorStyles.boldLabel); // 異常時はツールバー領域にエラー内容を表示
					}
				});
		}

		/// <summary>ヘッダー（APIキー入力と再接続）</summary>
		private void DrawHeader() {
			if(m_connector?.ImporterSettings == null) { // 設定がまだ読み込めていない場合は描画をスキップ
				return;
			}

			DrawBlockWithErrorHandling(
				"ヘッダー",
				() => {
					using (new GUILayout.HorizontalScope()) {
						m_connector.ImporterSettings.apiKey = EditorGUILayout.TextField("Notion APIキー", m_connector.ImporterSettings.apiKey); // APIキー入力欄（即時反映）

						if(GUILayout.Button("更新", GUILayout.Width(50))) {
							ExecuteWithErrorHandling("Notionへの再接続", () => m_connector.ForceConnect()); // 明示的に再接続を要求（認証情報が変わった際に使用）
						}
					}
				},
				message => {
					using (new GUILayout.HorizontalScope()) {
						EditorGUILayout.LabelField(message, EditorStyles.boldLabel); // 描画不能時は領域内にエラーを表示
					}
				});
		}

		/// <summary>フッター（ステータス表示のみ）</summary>
		private void DrawFooter() {
			DrawBlockWithErrorHandling(
				"フッター",
				() => {
					using (new EditorGUILayout.HorizontalScope()) {
						var statusText = string.IsNullOrWhiteSpace(m_currentStatusString) ? "ステータス: -" : m_currentStatusString; // 未設定時はハイフン表示
						EditorGUILayout.LabelField(statusText);                                                                 // 現在のステータスを左寄せで表示
					}
				},
				message => {
					using (new EditorGUILayout.HorizontalScope()) {
						EditorGUILayout.LabelField(message, EditorStyles.boldLabel); // 描画不能時はステータス領域にエラーを表示
					}
				});
		}

		/// <summary>機能タブを描画します。</summary>
		private void DrawFunctionArea() {
			DrawBlockWithErrorHandling(
				"インポート機能",
				() => {
					m_functions[0].DrawFunction(this, m_connector.ImporterSettings); // 機能描画を実行
				},
				message => {
					EditorGUILayout.HelpBox(message, MessageType.Error); // タブ描画失敗時はヘルプボックスで通知
				});
		}
                #endregion

                #region Error Handling Helpers
		/// <summary>指定した処理を例外補足付きで実行します。</summary>
		private void ExecuteWithErrorHandling(string context, Action action) {
			try {
				action?.Invoke();
			} catch (Exception ex) {
				HandleError(context, ex); // 発生した例外をログ・ステータスへ反映
			}
		}

		/// <summary>GUIブロック描画時に例外を捕捉しつつエラー表示を行います。</summary>
		private void DrawBlockWithErrorHandling(string blockName, Action drawAction, Action<string> drawErrorAction = null) {
			try {
				drawAction?.Invoke();
			} catch (Exception ex) {
				var errorMessage = HandleError($"{blockName}の描画", ex); // UI描画失敗時の共通メッセージを生成

				if(drawErrorAction != null) {
					drawErrorAction(errorMessage);
				} else {
					EditorGUILayout.HelpBox(errorMessage, MessageType.Error); // デフォルトはヘルプボックスで通知
				}
			}
		}

		/// <summary>例外発生時のログ出力とステータス更新をまとめます。</summary>
		private string HandleError(string context, Exception ex) {
			var message = $"{context}でエラーが発生しました: {ex.Message}"; // 画面表示・ログ用の文言を整形

			Debug.LogError($"NotionImporter: {message}{Environment.NewLine}{ex}");
			CurrentStatusString = message; // フッターにもエラー内容を反映

			return message;
		}

		/// <summary>外部クラス（NotionConnector等）からのエラー通知を一元化します。</summary>
		internal void ReportError(string context, Exception ex) {
			HandleError(context, ex);
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
