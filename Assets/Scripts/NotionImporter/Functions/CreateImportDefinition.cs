using System.IO;
using System.Linq;
using NotionImporter.Functions.SubFunction;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NotionImporter.Functions {

	/// <summary>インポート定義を作成するメイン機能です。</summary>
	public class CreateImportDefinition : IMainFunction {

		private MainImportWindow m_parent; // 親ウィンドウ

		private ISubFunction[] m_subFunctions = { // サブ機能の配列、追加機能がある時は初期化子に追加する
			new CreateScriptableObjectDefinition(),
		};

		/// <summary>利用可能なサブ機能の一覧を取得します。</summary>
		public ISubFunction[] SubFunctions {
			get {
				return m_subFunctions;
			}
		}

		/// <summary>機能名を取得します。</summary>
		public string FunctionName {
			get {
				return "インポート定義作成";
			}
		}

		private NotionImporterSettings m_settings; // インポート設定

		private int m_selectedSubFunctionIndex; // 選択しているサブ機能のインデックス

		/// <summary>選択中のサブ機能インデックスを取得または設定します。</summary>
		public int SelectedSubFunctionIndex {
			get {
				return m_selectedSubFunctionIndex;
			}
			set {
				m_selectedSubFunctionIndex = value;
			}
		}

		private TreeViewState m_treeViewState; // ツリービューの状態

		private NotionTree m_notionTree; // 表示中のNotionツリー

		/// <summary>表示に使用するNotionツリーを取得します。</summary>
		public NotionTree NotionTree {
			get {
				return m_notionTree;
			}
		}

		/// <summary>Notionインポータの機能を描画する</summary>
		public void DrawFunction(MainImportWindow parent, NotionImporterSettings settings) {
			m_parent = parent; // 親ウィンドウと設定情報を保持
			m_settings = settings;

			foreach (var func in m_subFunctions) {
				func.ParentFunction = this;
			}

			if(m_settings.connectionSucceed) { // 接続が成功した場合のみデータベースリストを描画
				DrawDefinitionNameSetting();
				DrawOutputFolderSetting();

				m_selectedSubFunctionIndex = EditorGUILayout.Popup("インポート種別", m_selectedSubFunctionIndex, // サブ機能をドロップダウンリストで表示
					m_subFunctions.Select(func => func.FunctionName).ToArray());

				using (new EditorGUILayout.HorizontalScope()) {
					DrawNotionTree();

					m_subFunctions[m_selectedSubFunctionIndex].DrawFunction(m_settings); // サブ機能の描画を実行
				}

				DrawFooter();
			} else {
				EditorGUILayout.LabelField("Notionに接続出来ませんでした");
			}
		}

		/// <summary>Notionのデータベース構造をツリー表示します。</summary>
		private void DrawNotionTree() {
			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) { // Notionオブジェクトを一覧表示する枠を描画
				using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) {
					GUILayout.Label("Notionオブジェクト", "ProfilerHeaderLabel");
				}

				if(m_notionTree == null) {
					if(m_treeViewState == null) {
						m_treeViewState = new TreeViewState();
					}

					m_notionTree = new NotionTree(m_treeViewState);

					m_notionTree.Initialize(m_settings.objects);

					m_notionTree.ExpandAll();
					m_notionTree.SetSelection(new[] {
						m_settings.objects.FirstOrDefault().id.GetHashCode()
					});

				} else {
					m_notionTree.Initialize(m_settings.objects);
				}

				var selectItem = m_notionTree.GetSelection();

				m_settings.CurrentObjectId = selectItem.FirstOrDefault();

				if(m_settings.CurrentObject.objectType == NotionObjectType.Container) {
					var children = m_settings.objects
						.Where(obj => obj.parent?.page_id == m_settings.CurrentObject.id && obj.objectType == NotionObjectType.Database)
						.Select(child => child.id.GetHashCode()).ToList();

					children.Add(m_settings.CurrentObjectId);
					m_notionTree.SetSelection(children);
				}

				var rect = EditorGUILayout.GetControlRect(false, m_notionTree.totalHeight);
				m_notionTree.OnGUI(rect);
			}
		}

		/// <summary>定義名の入力欄を描画します。</summary>
		private void DrawDefinitionNameSetting() {
			using (new EditorGUILayout.HorizontalScope()) { // 定義名を入力するテキストフィールドを配置
				m_settings.DefinitionName = EditorGUILayout.TextField("定義名", m_settings.DefinitionName);
			}
		}

		/// <summary>出力先フォルダ設定を描画します。</summary>
		private void DrawOutputFolderSetting() {
			using (new EditorGUILayout.HorizontalScope()) { // 選択済みフォルダを表示し、必要に応じて変更できるようにする
				using (new EditorGUI.DisabledScope(true)) {
					m_settings.OutputPath = EditorGUILayout.TextField("インポート先フォルダ", m_settings.OutputPath);
				}

				if(GUILayout.Button("フォルダ選択", GUILayout.Width(80))) {
					m_settings.OutputPath = EditorUtility.OpenFolderPanel("スクリプタブルオブジェクトの保存先", m_settings.OutputPath, "");

					if(!string.IsNullOrWhiteSpace(m_settings.OutputPath)) {
						m_settings.OutputPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), m_settings.OutputPath);
					}
				}
			}
		}

		/// <summary>インポート定義作成ボタンを描画します。</summary>
		private void DrawFooter() {
			if(GUILayout.Button("インポート定義作成")) { // 定義作成処理のトリガーボタン


				m_subFunctions[m_selectedSubFunctionIndex].CreateFile();

			}
		}

	}

}
