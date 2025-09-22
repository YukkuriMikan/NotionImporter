using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.NaniScripts {

	public class MappingFunction {

		private Dictionary<MappingType, int> m_mappingType = new();
		public Dictionary<MappingType, int> MappintType {
			get {
				return m_mappingType;
			}
		}

		public void DrawMappingPane(NotionImporterSettings settings) {
			using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) {
				//マッピング設定タイトル
				using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) {
					GUILayout.Label("マッピング設定", "ProfilerHeaderLabel");
				}

				foreach (MappingType type in Enum.GetValues(typeof(MappingType))) {
					m_mappingType.TryAdd(type, 0);

					m_mappingType[type] = EditorGUILayout.Popup(
						type.GetName(),
						m_mappingType[type],
						settings.CurrentProperty.Select(prop => prop.name).ToArray());
				}
			}
		}

	}

}