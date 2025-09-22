using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NotionImporter.Functions.SubFunction.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction {

	public class CreateScriptableObjectDefinition : ISubFunction {

		public IMainFunction ParentFunction { get; set; }

		/// <summary> インポート設定 </summary>
		private NotionImporterSettings m_settings;

		/// <summary> 機能名 </summary>
		public string FunctionName {
			get {
				return "ScriptableObject";
			}
		}

		/// <summary> 出力するファイルの型 </summary>
		public Type ExportFileType {
			get {
				return typeof(ScriptableObjectImportDefinition);
			}
		}

		private TypePaneFunction m_typePaneFunction = new();
		private MappingFunction m_mappingFunction = new();

		/// <summary> 画面の描画 </summary>
		/// <param name="settings">インポータの設定</param>
		public void DrawFunction(NotionImporterSettings settings) {
			m_settings = settings;

			//型一覧のペインを描画
			m_typePaneFunction.DrawTypePane(m_settings);
			m_mappingFunction.DrawMappingPane(m_settings, m_typePaneFunction.SelectedMappingTargetTypes);
		}

		/// <summary> インポート定義ファイルを生成する </summary>
		public void CreateFile() {
			if (string.IsNullOrWhiteSpace(m_settings.OutputPath)) {
				EditorUtility.DisplayDialog("エラー", "エクスポート先のフォルダを指定して下さい", "OK");

				return;
			}

			if (string.IsNullOrWhiteSpace(m_settings.DefinitionName)) {
				EditorUtility.DisplayDialog("エラー", "ファイル名を指定して下さい", "OK");

				return;
			}

			if (!Directory.Exists(NotionImporterParameters.DefinitionFilePath + $"\\{nameof(ScriptableObjectImportDefinition)}")) {
				Directory.CreateDirectory(NotionImporterParameters.DefinitionFilePath +
										$"\\{nameof(ScriptableObjectImportDefinition)}");
			}

			//型名のフォルダに定義ファイルを保存
			var filePath =
				NotionImporterParameters.DefinitionFilePath +
				$"\\{nameof(ScriptableObjectImportDefinition)}" +
				$"\\{m_settings.DefinitionName}.json";

			var soSetting = new ScriptableObjectImportDefinition {
				outputPath = m_settings.OutputPath,
				definitionName = m_settings.DefinitionName,
				targetDb = m_settings.CurrentObject,
				targetScriptableObject = m_typePaneFunction.SelectedMappingTargetTypes.typeString,
				mappingMode = m_mappingFunction.MappingMode,
				keyProperty = m_settings.KeyId,
				useKeyFiltering = m_settings.UseKeyFiltering,
				targetFieldType = m_mappingFunction.CurrentMappingMethod.MethodTargetArrayType,
				targetFieldName = m_mappingFunction.CurrentMappingMethod.MethodTarget?.fieldName,
				mappingData = m_mappingFunction.CurrentMappingMethod.GetMappingData(),
			};

			var jsonText = JsonUtility.ToJson(soSetting);

			File.WriteAllText(filePath, jsonText);
			AssetDatabase.Refresh();

			ImportMenu.RefreshImportMenu().Forget();
			Debug.Log("インポート定義を作成しました");
		}

		public void ReadFile(NotionImporterSettings settings, string json) {
			m_settings = settings;
			var def = JsonUtility.FromJson<ScriptableObjectImportDefinition>(json);

			var db = m_settings.objects.FirstOrDefault(obj => obj.id == def.targetDb.id);

			if (db == null) {
				EditorUtility.DisplayDialog("エラー", "定義ファイルに指定されているデータベースが存在しませんでした", "OK");

				return;
			}

			m_settings.CurrentObjectId = db.id.GetHashCode();
			ParentFunction.NotionTree.SetSelection(new List<int> { db.id.GetHashCode() });

			m_settings.DefinitionName = def.definitionName;
			m_settings.OutputPath = def.outputPath;

			m_mappingFunction.MappingMode = def.mappingMode;
			m_settings.KeyId = def.keyProperty;
			m_settings.UseKeyFiltering = def.useKeyFiltering;

			var targetType = Type.GetType(def.targetScriptableObject);
			var targetTypeItem = new TypeItem {
				typeName = targetType.Name,
				typeFullName = targetType.FullName,
				assemblyName = targetType.Assembly.GetName().Name,
			};
			var typeIndex = m_typePaneFunction.MappingTargetTypes.Select((itm, index) => (itm, index)).FirstOrDefault(pair => pair.itm.targetType == targetType).index;

			if (typeIndex < 0) {
				EditorUtility.DisplayDialog("エラー", "定義ファイルの処理対象の型が見つかりませんでした", "OK");

				return;
			}

			m_typePaneFunction.SelectedTypeIndex = typeIndex;

			m_mappingFunction.CurrentMappingMethod.Initialize(m_settings, targetTypeItem);

			var methodTarget = new MappingItem();

			methodTarget.doMaching = true;

			switch (def.mappingMode) {
				case MappingMode.Array:
					methodTarget.isArray = true;

					break;
				case MappingMode.List:
					methodTarget.isList = true;

					break;
			}

			methodTarget.fieldName = def.targetFieldName;

			m_mappingFunction.CurrentMappingMethod.MethodTarget = methodTarget;
			m_mappingFunction.m_targetType = targetTypeItem;
		}

	}

}