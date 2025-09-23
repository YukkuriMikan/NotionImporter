using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

        /// <summary>マッピング対象の型選択ペインを管理します。</summary>
        public class TypePaneFunction {

                private const string TARGET_ASSEMBLY = "Assembly-CSharp"; // 対象となる型のアセンブリ(とりあえずプロジェクトのアセンブリ)

                private Vector2 m_typeListScrollPosition; // 対象タイプリストのスクロール位置

                private TypeItem[] m_mappingTargetTypes; // マッピング対象の型情報配列

                /// <summary>マッピング対象となる型の一覧</summary>
                public TypeItem[] MappingTargetTypes {
                        get {
                                return m_mappingTargetTypes;
                        }
                }

                /// <summary>読込時に型キャッシュを確実に初期化する</summary>
                public void EnsureTypeList(NotionImporterSettings settings) {
                        m_settings = settings; // 最新設定を保持

                        if (m_mappingTargetTypes == null || m_settings.CurrentObject != m_currentDatabase) {
                                m_mappingTargetTypes = GetTypeItems(); // DB変更時などに型一覧を再生成
                                m_currentDatabase = m_settings.CurrentObject;
                        }
                }

                /// <summary>現在選択されている型情報</summary>
                public TypeItem SelectedMappingTargetTypes {
                        get {
                                return m_mappingTargetTypes[m_selectedTypeIndex];
                        }
                }

                /// <summary>現在選択されているDB(DB変更検出用)</summary>
                private NotionObject m_currentDatabase;

                /// <summary>選択タイプのインデックス</summary>
                private int m_selectedTypeIndex;

                public int SelectedTypeIndex {
                        get {
                                return m_selectedTypeIndex;
                        }
                        set {
                                m_selectedTypeIndex = value;
                        }
                }

                private NotionImporterSettings m_settings; // 現在の設定参照

                /// <summary>対象型リストのペイン描画</summary>
                public void DrawTypePane(NotionImporterSettings settings) {
                        m_settings = settings; // 設定を保持して型選択UIを描画

			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) {
				var doRefreshTypeCache = false;

				using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) {
					GUILayout.Label("マッピングクラスリスト", "ProfilerHeaderLabel");
					doRefreshTypeCache = GUILayout.Button("更新", GUILayout.Width(60));
				}

				GUILayout.Space(10);

				using (var scrollView = new EditorGUILayout.ScrollViewScope(m_typeListScrollPosition)) {
					m_typeListScrollPosition = scrollView.scrollPosition;

					if (m_mappingTargetTypes == null || doRefreshTypeCache || m_settings.CurrentObject != m_currentDatabase) {
						m_mappingTargetTypes = GetTypeItems();
						m_currentDatabase = m_settings.CurrentObject;
					}

					m_selectedTypeIndex =
						GUILayout.SelectionGrid(m_selectedTypeIndex,
							m_mappingTargetTypes.Select(itm => itm.typeName).ToArray(),
							1,
							"PreferencesKeysElement");
				}
			}
		}


                /// <summary>インポート対象の型アイテムを取得</summary>
                /// <returns>取得した型アイテム</returns>
                private TypeItem[] GetTypeItems() {
			return AppDomain.CurrentDomain.GetAssemblies()
				.Where(asm => asm.FullName.Contains(TARGET_ASSEMBLY)) // プロジェクトのアセンブリに絞り込む
				.OrderBy(asm => asm.GetName().Name)
				.SelectMany(asm => asm.GetTypes())
				.Where(t => !t.IsGenericType && !t.IsEnum && !t.IsNotPublic && !t.IsAbstract && !t.IsInterface)
				.Where(t => t.FullName.Contains(EditorSettings.projectGenerationRootNamespace)) // プロジェクトの名前空間に絞り込む
				.Where(t => { // ScriptableObjectの継承クラスに絞り込む
					Func<Type, bool> checkBaseClass = null;

					checkBaseClass = (t) => {
						if (t == typeof(ScriptableObject)) {
							return true;
						}

						if (t.BaseType == null) return false; // 完全な基底クラスまでScriptableObjectが見つからなかったのでFalseで終了

						if (t.Name == "Editor" || t.Name == "EditorWindow") return false; // Unityのエディタオブジェクトなので除外

						return checkBaseClass(t.BaseType);
					};

					return checkBaseClass(t);
				})
				.OrderBy(t => t.FullName)
				.Select(t =>
					new TypeItem {
						typeName = t.Name,
						typeFullName = t.FullName,
						assemblyName = t.Assembly.GetName().Name,
					})
				.ToArray();
		}

	}

}