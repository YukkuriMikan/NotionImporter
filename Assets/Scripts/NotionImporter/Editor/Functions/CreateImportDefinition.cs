using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NotionImporter.Functions.SubFunction;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NotionImporter.Functions {

	/// <summary>インポート定義を作成するメイン機能です。</summary>
	public class CreateImportDefinition : IMainFunction {

		private MainImportWindow m_parent; // 親ウィンドウ

		private ISubFunction[] m_subFunctions; // サブ機能のキャッシュ、リフレクションで自動収集する

		/// <summary>利用可能なサブ機能の一覧を取得します。</summary>
		public ISubFunction[] SubFunctions => m_subFunctions ??= LoadSubFunctions();

		/// <summary>機能名を取得します。</summary>
		public string FunctionName => "Notionインポート定義作成";

		private NotionImporterSettings m_settings; // インポート設定

		private int m_selectedSubFunctionIndex; // 選択しているサブ機能のインデックス

		/// <summary>選択中のサブ機能インデックスを取得または設定します。</summary>
		public int SelectedSubFunctionIndex {
			get => m_selectedSubFunctionIndex;
			set => m_selectedSubFunctionIndex = Mathf.Clamp(value, 0, Mathf.Max(0, SubFunctions.Length - 1)); // 無効なインデックスを防ぐ
		}

		private TreeViewState m_treeViewState; // ツリービューの状態

		private NotionTree m_notionTree; // 表示中のNotionツリー

		/// <summary>表示に使用するNotionツリーを取得します。</summary>
		public NotionTree NotionTree => m_notionTree;

		private NotionObject[] m_cachedNotionObjects; // 最新のNotionオブジェクト参照を保持

		/// <summary>Notionインポータの機能を描画する</summary>
		public void DrawFunction(MainImportWindow parent, NotionImporterSettings settings) {
			m_parent = parent; // 親ウィンドウと設定情報を保持
			m_settings = settings;

			var subFunctions = SubFunctions; // 利用可能なサブ機能一覧を取得
			if(subFunctions.Length == 0) {
				EditorGUILayout.HelpBox("利用可能なサブ機能が見つかりません。", MessageType.Warning); // 何も無ければ早期終了
				return;
			}

			foreach (var func in subFunctions) {
				func.ParentFunction = this;
			}

			if(m_settings.connectionSucceed) { // 接続が成功した場合のみデータベースリストを描画
				DrawDefinitionNameSetting();
				DrawOutputFolderSetting();

				m_selectedSubFunctionIndex = Mathf.Clamp(m_selectedSubFunctionIndex, 0, subFunctions.Length - 1); // 配列外参照の防止

				m_selectedSubFunctionIndex = EditorGUILayout.Popup("インポート種別", m_selectedSubFunctionIndex, // サブ機能をドロップダウンリストで表示
					subFunctions.Select(func => func.FunctionName).ToArray());

				using (new EditorGUILayout.HorizontalScope()) {
					DrawNotionTree();

					subFunctions[m_selectedSubFunctionIndex].DrawFunction(m_settings); // サブ機能の描画を実行
				}

				DrawFooter();
			} else {
				EditorGUILayout.LabelField("Notionに接続出来ませんでした");
			}
		}

		/// <summary>Notionのデータベース構造をツリー表示します。</summary>
		private void DrawNotionTree() {
			if(!ReferenceEquals(m_cachedNotionObjects, m_settings.objects)) {
				m_cachedNotionObjects = m_settings.objects; // 新しい取得結果に更新
				m_notionTree = null;
				m_treeViewState = null; // ⭐ データ更新時はツリーを作り直して最新状態を表示
			}

			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) { // Notionオブジェクトを一覧表示する枠を描画
                                using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) {
                                        GUILayout.Label("Notionオブジェクト", "ProfilerHeaderLabel");
                                }

                                if(m_settings.objects == null || m_settings.objects.Length == 0) {
                                        EditorGUILayout.HelpBox("Notionで参照可能なオブジェクトが見つかりません。APIキーや権限を確認してください。", MessageType.Info); // 表示対象が無ければ案内だけ出して早期リターン
                                        return;
                                }

                                if(m_notionTree == null) {
                                        if(m_treeViewState == null) {
                                                m_treeViewState = new TreeViewState();
                                        }

                                        m_notionTree = new NotionTree(m_treeViewState);

                                        m_notionTree.Initialize(m_settings.objects);

                                        m_notionTree.ExpandAll();

                                        var firstObject = m_settings.objects.FirstOrDefault();
                                        if(firstObject != null) {
                                                m_notionTree.SetSelection(new[] {
                                                        firstObject.id.GetHashCode()
                                                });
                                        }

                                } else {
                                        m_notionTree.Initialize(m_settings.objects);
                                }

                                var selectItem = m_notionTree.GetSelection();

                                if(selectItem == null || selectItem.Count == 0) {
                                        return; // 選択状態が無ければ後続処理をスキップして安全性を確保
                                }

                                m_settings.CurrentObjectId = selectItem.FirstOrDefault();

                                var currentObject = m_settings.CurrentObject;
                                if(currentObject == null) {
                                        return; // 選択オブジェクトが特定出来ない場合は描画を終了
                                }

                                if(currentObject.objectType == NotionObjectType.Container) {
                                        var children = m_settings.objects
                                                .Where(obj => obj.parent?.page_id == currentObject.id && obj.objectType == NotionObjectType.Database)
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
				SubFunctions[m_selectedSubFunctionIndex].CreateFile(); // リフレクションで取得した機能を実行
			}
		}

		/// <summary>ISubFunctionを実装したクラスをリフレクションで収集します。</summary>
		private static ISubFunction[] LoadSubFunctions() {
			var instances = new List<ISubFunction>(); // 生成したサブ機能を蓄積

      foreach (var type in TypeCache.GetTypesDerivedFrom<ISubFunction>()) {
				if(type.IsAbstract) {
					continue; // 抽象クラスはインスタンス化できない
				}

				if(type.GetConstructor(Type.EmptyTypes) == null) {
					continue; // 引数なしコンストラクタが無い型は生成不可なのでスキップ
				}

				try {
					var instance = (ISubFunction)Activator.CreateInstance(type);
					if(instance != null) {
						instances.Add(instance);
					}
				} catch(Exception ex) {
					Debug.LogWarning($"ISubFunctionの生成に失敗しました: {type.FullName}\n{ex}"); // 生成エラーを通知
				}
			}

			if(instances.Count == 0) {
				Debug.LogWarning("ISubFunctionを実装したクラスが見つかりませんでした。"); // 未検出時に注意喚起
			}

			return instances
				.OrderBy(func => func.FunctionName, StringComparer.Ordinal) // 表示順を安定化
				.ToArray();
		}

		}

}

