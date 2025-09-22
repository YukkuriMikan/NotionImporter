using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	public class ListMapping : ArrayMapping {

		public override void DrawPaneHeader() {
			GUILayout.Label("リストマッピング設定", "ProfilerHeaderLabel");

		}

		public override void DrawTargetType() {
			EditorGUILayout.LabelField(
				$"{MethodTarget.fieldName}:{MethodTarget.fieldInfo.FieldType.Name}",
				(GUIStyle)"AM HeaderStyle");

		}
	}
}