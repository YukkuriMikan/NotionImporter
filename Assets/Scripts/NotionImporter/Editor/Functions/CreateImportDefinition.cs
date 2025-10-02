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

                private bool m_isLastSelectionLoaded; // 前回選択済みインポート種別の適用有無

		/// <summary>利用可能なサブ機能の一覧を取得します。</summary>
		public ISubFunction[] SubFunctions => m_subFunctions ??= LoadSubFunctions();

		/// <summary>機能名を取得します。</summary>
		public string FunctionName => "Notionインポート定義作成";

		private NotionImporterSettings m_settings; // インポート設定

		private int m_selectedSubFunctionIndex; // 選択しているサブ機能のインデックス

		/// <summary>選択中のサブ機能インデックスを取得または設定します。</summary>
                public int SelectedSubFunctionIndex {
                        get => m_selectedSubFunctionIndex;
                        set {
                                var clamped = Mathf.Clamp(value, 0, Mathf.Max(0, SubFunctions.Length - 1)); // 範囲外アクセスの防止

                                if(m_selectedSubFunctionIndex == clamped) {
                                        return; // 値が変わらない場合は何もしない
                                }

                                m_selectedSubFunctionIndex = clamped;

                                PersistLastSelection(SubFunctions.Length > 0 ? SubFunctions[m_selectedSubFunctionIndex] : null); // 選択種別を永続化
                        }
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

                                if(!m_isLastSelectionLoaded) {
                                        LoadLastSelection(subFunctions); // 前回選択を初回描画時に反映
                                }

                                m_selectedSubFunctionIndex = Mathf.Clamp(m_selectedSubFunctionIndex, 0, subFunctions.Length - 1); // 配列外参照の防止

                                var newSelectedIndex = EditorGUILayout.Popup("インポート種別", m_selectedSubFunctionIndex, // サブ機能をドロップダウンリストで表示
                                        subFunctions.Select(func => func.FunctionName).ToArray());

                                if(newSelectedIndex != m_selectedSubFunctionIndex) {
                                        m_selectedSubFunctionIndex = newSelectedIndex;
                                        PersistLastSelection(subFunctions[m_selectedSubFunctionIndex]); // 選択変更時に保存
                                }

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

                /// <summary>前回選択したインポート種別を読み込みます。</summary>
                /// <param name="subFunctions">利用可能なサブ機能一覧</param>
		private void LoadLastSelection(ISubFunction[] subFunctions) {
			m_isLastSelectionLoaded = true; // 二重適用を防止

			if(m_settings == null) {
				return; // 設定未初期化時は既定値を使用
			}

			var lastTypeName = m_settings.LastImportTypeFullName; // 設定ファイルに保存した型名を取得

			if(string.IsNullOrEmpty(lastTypeName)) {
				return; // 保存データが無ければ既定値のまま使用
			}

			var storedIndex = Array.FindIndex(subFunctions, func => func?.GetType().FullName == lastTypeName); // 型名一致を探索

			if(storedIndex >= 0) {
				m_selectedSubFunctionIndex = storedIndex; // 一致した場合に前回選択を復元
			}
		}

		/// <summary>選択したインポート種別を永続化します。</summary>
		/// <param name="subFunction">現在選択しているサブ機能</param>
		private void PersistLastSelection(ISubFunction subFunction) {
			if(m_settings == null) {
				return; // 設定参照が無い場合は保存できない
			}

			var newTypeName = subFunction?.GetType().FullName ?? string.Empty; // 保存対象の型名を決定

			if(m_settings.LastImportTypeFullName == newTypeName) {
				return; // 既存値と同じ場合は保存処理を省略
			}

			m_settings.LastImportTypeFullName = newTypeName; // 設定オブジェクトに最新値を反映

			try {
				NotionImporterSettings.SaveSetting(m_settings); // 設定ファイルへ書き戻して永続化
			} catch(Exception ex) {
				Debug.LogWarning($"インポート種別の保存に失敗しました: {ex.Message}\n{ex}"); // 保存失敗時は警告で通知
			}
		}

		}

}

