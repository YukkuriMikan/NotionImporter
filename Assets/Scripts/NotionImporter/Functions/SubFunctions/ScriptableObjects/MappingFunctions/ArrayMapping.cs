using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	public class ArrayMapping : MappingMethodBase {

		private bool m_useKeyProperty;

		private int m_propertyIndex;

		public override TypeItem MethodTargetType {
			get {
				return new TypeItem {
					typeName = MethodTarget?.fieldInfo.FieldType.GetElementType().Name,
					typeFullName = MethodTarget?.fieldInfo.FieldType.GetElementType().FullName,
					assemblyName = MethodTarget?.fieldInfo.FieldType.GetElementType().Assembly.GetName().Name,
				};
			}
		}

		public override TypeItem MethodTargetArrayType {
			get {
				return new TypeItem {
					typeName = MethodTarget?.fieldInfo.FieldType.Name,
					typeFullName = MethodTarget?.fieldInfo.FieldType.FullName,
					assemblyName = MethodTarget?.fieldInfo.FieldType.Assembly.GetName().Name,
				};
			}
		}

		public override void DrawPaneHeader() {
			GUILayout.Label("配列マッピング設定", "ProfilerHeaderLabel");
		}

		public override void DrawTargetType() {
			EditorGUILayout.LabelField(
				$"{MethodTarget.fieldName}:{MethodTarget.fieldInfo.FieldType.Name}",
				(GUIStyle)"AM HeaderStyle");
		}

		public override void DrawKeyRow() {
			var props = m_settings.CurrentProperty.Select(prop => prop.name).ToArray();

			using (new EditorGUILayout.HorizontalScope()) {
				m_useKeyProperty = EditorGUILayout.ToggleLeft($"キー列でグループ化", m_useKeyProperty);

				using (new EditorGUI.DisabledScope(!m_useKeyProperty)) {
					m_propertyIndex = EditorGUILayout.Popup(m_propertyIndex, props);
				}
			}

			if (m_useKeyProperty) {
				m_settings.KeyId = m_settings.CurrentProperty.FirstOrDefault(prop => prop.name == props[m_propertyIndex])?.id;
			} else {
				m_settings.KeyId = null;
			}

			m_settings.UseKeyFiltering = EditorGUILayout.ToggleLeft("出力時にフィルタリングを行う", m_settings.UseKeyFiltering);

			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
			EditorGUILayout.Space();
		}

		public override void DrawMappingRow(MappingFunction func, MappingItem itm) {
			//ノーションのデータベースに変数にマッチするフィールドが存在するか？
			var selectableNotionFieldsNothing = itm.targetProperties.Length == 0;
			var isUnsupportedArray = (itm.fieldType != typeof(string[]) && (itm.isArray || itm.isList));
			var isDisableRow = selectableNotionFieldsNothing || isUnsupportedArray;

			using (new EditorGUI.DisabledScope(isDisableRow)) {
				itm.doMaching = EditorGUILayout.ToggleLeft($"{itm.fieldName}:{itm.fieldInfo?.FieldType.Name}",
					itm.doMaching);

				EditorGUILayout.LabelField($"←", GUILayout.Width(20));

				if (isUnsupportedArray) {
					itm.doMaching = !isDisableRow;
					EditorGUILayout.LabelField("配列のネストはサポートしていません");
				} else {
					if (selectableNotionFieldsNothing) {
						itm.doMaching = !isDisableRow;
						EditorGUILayout.LabelField("選択されたデータベースに適合するフィールドがありません");
					} else {
						itm.propertyIndex = EditorGUILayout.Popup(itm.propertyIndex,
							itm.targetProperties.Select(prop => $"{prop.name.Replace("/", "／")}:{prop.type}").ToArray());
					}
				}
			}
		}

	}

}