using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

        /// <summary>ScriptableObjectのマッピング設定を管理します。</summary>
        public class MappingFunction {

                private NotionImporterSettings m_settings; // 現在のインポート設定

                private MappingMode m_mappingMode = MappingMode.Normal; // 現在のマッピングモード

                /// <summary>選択されているマッピングモード</summary>
                public MappingMode MappingMode {
                        get {
                                return m_mappingMode;
                        }
                        set {
                                m_mappingMode = value;
                        }
                }

                private MappingMethodBase[] m_mappingMethods = { // マッピング処理の実装一覧
                        new NormalMapping(),
                        new ArrayMapping(),
                        new ListMapping(),
                };

                /// <summary>現在のマッピング方式</summary>
                public MappingMethodBase CurrentMappingMethod {
                        get {
                                return m_mappingMethods[(int)m_mappingMode];
                        }
                }

                public TypeItem m_targetType; // マッピング対象の型情報

                public NotionObject m_currentObject; // 現在処理中のNotionObject

                /// <summary>マッピング設定用のペインを描画します。</summary>
                public void DrawMappingPane(NotionImporterSettings settings, TypeItem targetTypeItem) {
                        m_settings = settings; // 設定や対象型が変わった場合は再初期化

                        if (m_targetType?.typeFullName != targetTypeItem?.typeFullName || m_currentObject != m_settings.CurrentObject) {
				m_targetType = targetTypeItem;
				m_mappingMode = MappingMode.Normal;

				CurrentMappingMethod.Initialize(m_settings, m_targetType);

				m_currentObject = m_settings.CurrentObject;
			}

			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) {
				using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) { // マッピング設定タイトル
					CurrentMappingMethod.DrawPaneHeader();

					if (GUILayout.Button("全てON", GUILayout.Width(60))) {
						foreach (var itm in CurrentMappingMethod.MethodMappingItems) {
							itm.doMaching = true;
						}
					}

					if (GUILayout.Button("全てOFF", GUILayout.Width(60))) {
						foreach (var itm in CurrentMappingMethod.MethodMappingItems) {
							itm.doMaching = false;
						}
					}
				}

				GUILayout.Space(10);

				using (new EditorGUILayout.HorizontalScope()) { // マッピングフィールド描画
					DrawMappingItems();
				}
			}
		}

                /// <summary> デシリアライズ先の型のフィールドリストを描画 </summary>
                private void DrawMappingItems() {
                        using (new EditorGUILayout.VerticalScope()) { // マッピング対象フィールドの一覧を描画
                                CurrentMappingMethod.DrawKeyRow();

				using (new EditorGUILayout.HorizontalScope()) { // ヘッダ
					CurrentMappingMethod.DrawTargetType();

					EditorGUILayout.LabelField($"　", GUILayout.Width(20));
					EditorGUILayout.LabelField("Notionデータベース", (GUIStyle)"AM HeaderStyle");
				}

				EditorGUILayout.Space(5);

				foreach (var itm in CurrentMappingMethod.MethodMappingItems) {
					using (new EditorGUILayout.HorizontalScope()) {
						if (itm.targetProperties.Length == 0) { } // インポート可能なプロパティがゼロの場合、マッチング不可

						CurrentMappingMethod.DrawMappingRow(this, itm);
					}
				}
			}
		}

	}

}