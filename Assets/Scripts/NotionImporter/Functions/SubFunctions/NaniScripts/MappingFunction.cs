using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.NaniScripts {

        /// <summary>NaniScript用のマッピング設定を管理します。</summary>
        public class MappingFunction {

                private Dictionary<MappingType, int> m_mappingType = new(); // マッピング種別ごとの選択インデックス

                /// <summary>マッピング種別ごとの選択状態</summary>
                public Dictionary<MappingType, int> MappintType {
                        get {
                                return m_mappingType;
                        }
                }

                /// <summary>マッピング設定用のペインを描画します。</summary>
                public void DrawMappingPane(NotionImporterSettings settings) {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.textArea)) { // 各マッピング種別に対応するNotionプロパティを選択
                                using (new EditorGUILayout.HorizontalScope("AC BoldHeader")) { // マッピング設定タイトル
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