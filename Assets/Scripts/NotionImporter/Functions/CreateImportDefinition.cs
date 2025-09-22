using System.IO;
using System.Linq;
using NotionImporter.Functions.SubFunction;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NotionImporter.Functions {

	public class CreateImportDefinition : IMainFunction {

		/// <summary> 親ウィンドウ </summary>
		private MainImportWindow m_parent;

		/// <summary> サブ機能の配列、追加機能がある時は初期化子に追加する </summary>
		private ISubFunction[] m_subFunctions = {
			new CreateScriptableObjectDefinition(),
		};

		public ISubFunction[] SubFunctions {
			get {
				return m_subFunctions;
			}
		}

		/// <summary> 機能名 </summary>
		public string FunctionName {
			get {
				return "インポート定義作成";
			}
		}

		/// <summary> インポート設定 </summary>
		private NotionImporterSettings m_settings;

		/// <summary> 選択しているサブ機能のインデックス </summary>
		private int m_selectedSubFunctionIndex;

		public int SelectedSubFunctionIndex {
			get {
				return m_selectedSubFunctionIndex;
			}
			set {
				m_selectedSubFunctionIndex = value;
			}
		}

		private TreeViewState m_treeViewState;

		private NotionTree m_notionTree;
		public NotionTree NotionTree {
			get {
				return m_notionTree;
			}
		}

		/// <summary> Notionインポータの機能を描画する </summary>
		public void DrawFunction(MainImportWindow parent, NotionImporterSettings settings) {
			m_parent = parent;
			m_settings = settings;

			foreach (var func in m_subFunctions) {
				func.ParentFunction = this;
			}

			//接続が成功した場合のみデータベースリストを描画
			if (m_settings.connectionSucceed) {
				DrawDefinitionNameSetting();
				DrawOutputFolderSetting();

				//サブ機能をドロップダウンリストで表示
				m_selectedSubFunctionIndex = EditorGUILayout.Popup("インポート種別", m_selectedSubFunctionIndex,
					m_subFunctions.Select(func => func.FunctionName).ToArray());

				using (new EditorGUILayout.HorizontalScope()) {
					DrawNotionTree();

					//サブ機能の描画を実行
					m_subFunctions[m_selectedSubFunctionIndex].DrawFunction(m_settings);
				}

				DrawFooter();
			} else {
				EditorGUILayout.LabelField("Notionに接続出来ませんでした");
			}
		}

		private void DrawNotionTree() {
			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) {
				using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) {
					GUILayout.Label("Notionオブジェクト", "ProfilerHeaderLabel");
				}

				if (m_notionTree == null) {
					if (m_treeViewState == null) {
						m_treeViewState = new TreeViewState();
					}

					m_notionTree = new NotionTree(m_treeViewState);

					m_notionTree.Initialize(m_settings.objects);

					m_notionTree.ExpandAll();
					m_notionTree.SetSelection(new[] { m_settings.objects.FirstOrDefault().id.GetHashCode() });

				} else {
					m_notionTree.Initialize(m_settings.objects);
				}

				var selectItem = m_notionTree.GetSelection();

				m_settings.CurrentObjectId = selectItem.FirstOrDefault();

				if (m_settings.CurrentObject.objectType == NotionObjectType.Container) {
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

		private void DrawDefinitionNameSetting() {
			using (new EditorGUILayout.HorizontalScope()) {
				m_settings.DefinitionName = EditorGUILayout.TextField("定義名", m_settings.DefinitionName);
			}
		}

		private void DrawOutputFolderSetting() {
			using (new EditorGUILayout.HorizontalScope()) {
				using (new EditorGUI.DisabledScope(true)) {
					m_settings.OutputPath = EditorGUILayout.TextField("インポート先フォルダ", m_settings.OutputPath);
				}

				if (GUILayout.Button("フォルダ選択", GUILayout.Width(80))) {
					m_settings.OutputPath = EditorUtility.OpenFolderPanel("スクリプタブルオブジェクトの保存先", m_settings.OutputPath, "");

					if (!string.IsNullOrWhiteSpace(m_settings.OutputPath)) {
						m_settings.OutputPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), m_settings.OutputPath);
					}
				}
			}
		}

		private void DrawFooter() {
			if (GUILayout.Button("インポート定義作成")) {


				m_subFunctions[m_selectedSubFunctionIndex].CreateFile();

			}
		}

	}

}