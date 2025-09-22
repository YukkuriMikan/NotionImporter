using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	public class MappingFunction {

		private NotionImporterSettings m_settings;

		private MappingMode m_mappingMode = MappingMode.Normal;

		public MappingMode MappingMode {
			get {
				return m_mappingMode;
			}
			set {
				m_mappingMode = value;
			}
		}

		private MappingMethodBase[] m_mappingMethods = {
			new NormalMapping(),
			new ArrayMapping(),
			new ListMapping(),
		};

		public MappingMethodBase CurrentMappingMethod {
			get {
				return m_mappingMethods[(int)m_mappingMode];
			}
		}

		public TypeItem m_targetType; //マッピング対象の型

		public NotionObject m_currentObject; //現在処理中のNotionObject

		public void DrawMappingPane(NotionImporterSettings settings, TypeItem targetTypeItem) {
			m_settings = settings;

			if (m_targetType?.typeFullName != targetTypeItem?.typeFullName || m_currentObject != m_settings.CurrentObject) {
				m_targetType = targetTypeItem;
				m_mappingMode = MappingMode.Normal;

				CurrentMappingMethod.Initialize(m_settings, m_targetType);

				m_currentObject = m_settings.CurrentObject;
			}

			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) {
				//マッピング設定タイトル
				using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) {
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

				//マッピングフィールド描画
				using (new EditorGUILayout.HorizontalScope()) {
					DrawMappingItems();
				}
			}
		}

		/// <summary> デシリアライズ先の型のフィールドリストを描画 </summary>
		private void DrawMappingItems() {
			using (new EditorGUILayout.VerticalScope()) {
				CurrentMappingMethod.DrawKeyRow();

				//ヘッダ
				using (new EditorGUILayout.HorizontalScope()) {
					CurrentMappingMethod.DrawTargetType();

					EditorGUILayout.LabelField($"　", GUILayout.Width(20));
					EditorGUILayout.LabelField("Notionデータベース", (GUIStyle)"AM HeaderStyle");
				}

				EditorGUILayout.Space(5);

				foreach (var itm in CurrentMappingMethod.MethodMappingItems) {
					using (new EditorGUILayout.HorizontalScope()) {
						//インポート可能なプロパティがゼロの場合、マッチング不可
						if (itm.targetProperties.Length == 0) { }

						CurrentMappingMethod.DrawMappingRow(this, itm);
					}
				}
			}
		}

	}

}