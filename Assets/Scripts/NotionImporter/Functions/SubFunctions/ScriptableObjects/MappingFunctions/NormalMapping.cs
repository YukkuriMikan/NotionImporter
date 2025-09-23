using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	/// <summary>通常のフィールドへマッピングする処理を提供します。</summary>
	public class NormalMapping : MappingMethodBase {
		/// <summary>メソッドが必要とするターゲットアイテム</summary>
		public override MappingItem MethodTarget { get; set; }

		public override void DrawPaneHeader() {
			GUILayout.Label("マッピング設定", "ProfilerHeaderLabel");
		}

		public override void DrawTargetType() {
			EditorGUILayout.LabelField("スクリプタブルオブジェクト", (GUIStyle)"AM HeaderStyle");
		}

		public override void DrawKeyRow() {
			m_settings.KeyId = null; // 通常マッピングではキー設定を初期化
			m_settings.UseKeyFiltering = false;
		}

		public override void DrawMappingRow(MappingFunction func, MappingItem itm) {
			var selectableNotionFieldsNothing = itm.targetProperties.Length == 0; // 対象フィールドの種類に応じたマッピング可否を判定（Notion側に一致するフィールドがあるか）

			if(itm.isArray) {
				itm.doMaching = false;

				if(GUILayout.Button($"{itm.fieldName}:{itm.fieldInfo?.FieldType.Name}にマッピングする")) {
					func.MappingMode = MappingMode.Array;

					func.CurrentMappingMethod.MethodTarget = itm;
					func.CurrentMappingMethod.Initialize(m_settings, func.CurrentMappingMethod.MethodTargetType);
				}
			} else if(itm.isList) {
				itm.doMaching = false;

				var buttonString =
					$"{itm.fieldName}:{itm.fieldInfo?.FieldType.Name}<{itm.fieldInfo.FieldType.GenericTypeArguments[0]}>にマッピングする";

				if(GUILayout.Button(buttonString)) {
					func.MappingMode = MappingMode.List;

					func.CurrentMappingMethod.MethodTarget = itm;
					func.CurrentMappingMethod.Initialize(m_settings, func.CurrentMappingMethod.MethodTargetType);
				}
			} else {
				itm.doMaching = EditorGUILayout.ToggleLeft($"{itm.fieldName}:{itm.fieldInfo?.FieldType.Name}",
					itm.doMaching);

				using (new EditorGUI.DisabledScope(!itm.doMaching)) {
					EditorGUILayout.LabelField($"←", GUILayout.Width(20));

					if(selectableNotionFieldsNothing) {
						EditorGUILayout.Popup(0,
							new[] {
								"Notion側に適合するフィールドがありません"
							});
					} else {
						itm.propertyIndex = EditorGUILayout.Popup(itm.propertyIndex,
							itm.targetProperties.Select(prop => $"{prop.name.Replace("/", "／")}:{prop.type}").ToArray());
					}
				}
			}
		}

	}

}
