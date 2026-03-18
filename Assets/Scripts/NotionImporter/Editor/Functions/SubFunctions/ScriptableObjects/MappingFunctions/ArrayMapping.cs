using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NotionImporter;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	/// <summary>配列フィールドへのマッピング処理を提供します。</summary>
	public class ArrayMapping : MappingMethodBase {

		private bool m_useKeyProperty; // キー列を利用するかどうか

		private int m_propertyIndex; // 選択中のプロパティインデックス

		public override TypeItem MethodTargetType
			=> new() {
				typeName = MethodTarget?.fieldInfo.FieldType.GetElementType().Name,
				typeFullName = MethodTarget?.fieldInfo.FieldType.GetElementType().FullName,
				assemblyName = MethodTarget?.fieldInfo.FieldType.GetElementType().Assembly.GetName().Name,
			};

		public override TypeItem MethodTargetArrayType
			=> new() {
				typeName = MethodTarget?.fieldInfo.FieldType.Name,
				typeFullName = MethodTarget?.fieldInfo.FieldType.FullName,
				assemblyName = MethodTarget?.fieldInfo.FieldType.Assembly.GetName().Name,
			};

		public override void DrawPaneHeader() {
			GUILayout.Label("配列マッピング設定", "ProfilerHeaderLabel");
		}

		public override void DrawTargetType() {
			EditorGUILayout.LabelField(
				$"{MethodTarget.fieldName}:{MethodTarget.fieldInfo.FieldType.Name}",
				(GUIStyle)"AM HeaderStyle");
		}

		public override void DrawKeyRow() {
			var props = m_settings.CurrentProperty.Select(prop => prop.name).ToArray(); // キー列の選択UIを構築

			if(!string.IsNullOrEmpty(m_settings.KeyId)) { // 保存済み設定があればUI状態へ反映
				var idIndex = Array.FindIndex(m_settings.CurrentProperty, prop => prop.id == m_settings.KeyId);

				if(idIndex >= 0) {
					m_useKeyProperty = true;
					m_propertyIndex = idIndex;
				}
			}

			using (new EditorGUILayout.HorizontalScope()) {
				m_useKeyProperty = EditorGUILayout.ToggleLeft($"キー列でグループ化", m_useKeyProperty);

				using (new EditorGUI.DisabledScope(!m_useKeyProperty)) {
					m_propertyIndex = EditorGUILayout.Popup(m_propertyIndex, props);
				}
                        }

                        if(m_useKeyProperty) {
                                m_settings.KeyId = m_settings.CurrentProperty
                                        .ElementAtOrDefault(m_propertyIndex)?.id;
                        } else {
                                m_settings.KeyId = null;
                        }

			m_settings.UseKeyFiltering = EditorGUILayout.ToggleLeft("出力時にフィルタリングを行う", m_settings.UseKeyFiltering);

			var mappingItems = MethodMappingItems ?? Array.Empty<MappingItem>(); // ソート候補となるフィールド群
			var sortKeyLabels = new string[mappingItems.Length + 1];
			var sortKeyValues = new string[mappingItems.Length + 1];

			sortKeyLabels[0] = "（ソートしない）";
			sortKeyValues[0] = string.Empty;

			for (var i = 0; i < mappingItems.Length; i++) {
				var item = mappingItems[i];

				sortKeyLabels[i + 1] = $"{item.fieldName}:{item.fieldInfo?.FieldType.Name}";
				sortKeyValues[i + 1] = item.fieldName;
			}

			var sortConditions = BuildEditableSortConditions();
			int removeAt = -1; // 描画中に削除が押された行番号を後で反映

			for (var i = 0; i < sortConditions.Count; i++) {
				var condition = sortConditions[i];
				var currentSortKey = string.IsNullOrEmpty(condition.sortKey) ? string.Empty : condition.sortKey;
				var sortKeyIndex = Array.IndexOf(sortKeyValues, currentSortKey);

				if(sortKeyIndex < 0) {
					sortKeyIndex = 0;
					condition.sortKey = null;
				}

				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.LabelField($"第{i + 1}キー", GUILayout.Width(80));
					sortKeyIndex = EditorGUILayout.Popup(sortKeyIndex, sortKeyLabels);
					condition.sortKey = sortKeyIndex <= 0 ? null : sortKeyValues[sortKeyIndex];

					using (new EditorGUI.DisabledScope(sortKeyIndex == 0)) {
						var sortOrderLabels = new[] {
							"昇順",
							"降順",
						};

						var orderIndex = EditorGUILayout.Popup((int)condition.sortOrder, sortOrderLabels, GUILayout.Width(70));
						orderIndex = Mathf.Clamp(orderIndex, 0, sortOrderLabels.Length - 1);
						condition.sortOrder = (SortOrder)orderIndex;
					}

					if(GUILayout.Button("削除", GUILayout.Width(44))) {
						removeAt = i;
					}
				}

				if(sortKeyIndex == 0) {
					condition.sortOrder = SortOrder.Ascending; // キー未指定行は昇順に初期化
				}
			}

			if(removeAt >= 0) {
				sortConditions.RemoveAt(removeAt);
			}

			using (new EditorGUI.DisabledScope(mappingItems.Length == 0)) {
				if(GUILayout.Button("ソートキーを追加")) {
					sortConditions.Add(new SortCondition()); // 第2・第3キー追加用
				}
			}

			ApplySortConditionToSettings(sortConditions);

			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
			EditorGUILayout.Space();
		}

		/// <summary>編集対象のソート条件配列を構築します。</summary>
		private List<SortCondition> BuildEditableSortConditions() {
			if(m_settings.SortConditions != null && m_settings.SortConditions.Length > 0) {
				return m_settings.SortConditions
					.Where(condition => condition != null)
					.Select(condition => new SortCondition {
						sortKey = condition.sortKey,
						sortOrder = condition.sortOrder,
					})
					.ToList();
			}

			if(!string.IsNullOrEmpty(m_settings.SortKey)) {
				return new List<SortCondition> {
					new() {
						sortKey = m_settings.SortKey,
						sortOrder = m_settings.SortOrder,
					}
				};
			}

			return new List<SortCondition>();
		}

		/// <summary>UI編集結果を設定オブジェクトへ反映します。</summary>
		private void ApplySortConditionToSettings(List<SortCondition> sortConditions) {
			var sanitized = (sortConditions ?? new List<SortCondition>())
				.Where(condition => condition != null)
				.Select(condition => new SortCondition {
					sortKey = condition.sortKey,
					sortOrder = condition.sortOrder,
				})
				.ToArray();

			m_settings.SortConditions = sanitized;

			var firstActive = sanitized.FirstOrDefault(condition => !string.IsNullOrEmpty(condition.sortKey)); // 旧項目にも先頭キーを書き戻して互換維持

			m_settings.SortKey = firstActive?.sortKey;
			m_settings.SortOrder = firstActive?.sortOrder ?? SortOrder.Ascending;
		}

		public override void DrawMappingRow(MappingFunction func, MappingItem itm) {
			var selectableNotionFieldsNothing = itm.targetProperties.Length == 0; // 配列向けのマッピング行を描画する際に、Notion側に一致フィールドがあるか確認
			var isUnsupportedArray = (itm.fieldType != typeof(string[]) && (itm.isArray || itm.isList));
			var isDisableRow = selectableNotionFieldsNothing || isUnsupportedArray;

			using (new EditorGUI.DisabledScope(isDisableRow)) {
				itm.doMaching = EditorGUILayout.ToggleLeft($"{itm.fieldName}:{itm.fieldInfo?.FieldType.Name}",
					itm.doMaching);

				EditorGUILayout.LabelField($"←", GUILayout.Width(20));

				if(isUnsupportedArray) {
					itm.doMaching = !isDisableRow;
					EditorGUILayout.LabelField("配列のネストはサポートしていません");
				} else {
					if(selectableNotionFieldsNothing) {
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
