using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NotionImporter.Functions.SubFunction.NaniScripts;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction {

	public class CreateNaniScriptDefinition : ISubFunction {

		public IMainFunction ParentFunction { get; set; }

		/// <summary> インポート設定 </summary>
		private NotionImporterSettings m_settings;

		public string FunctionName {
			get {
				return "NaniScript";
			}
		}

		public Type ExportFileType {
			get {
				return typeof(NaniScriptImportDefinition);
			}
		}

		private MappingFunction m_mappingFunction = new();

		public void DrawFunction(NotionImporterSettings settings) {
			m_settings = settings;

			m_mappingFunction.DrawMappingPane(m_settings);
		}

		public void CreateFile() {
			if (string.IsNullOrWhiteSpace(m_settings.OutputPath)) {
				EditorUtility.DisplayDialog("エラー", "エクスポート先のフォルダを指定して下さい", "OK");

				return;
			}

			if (string.IsNullOrWhiteSpace(m_settings.DefinitionName)) {
				EditorUtility.DisplayDialog("エラー", "ファイル名を指定して下さい", "OK");

				return;
			}

			if (!Directory.Exists(NotionImporterParameters.DefinitionFilePath + $"\\{nameof(NaniScriptImportDefinition)}")) {
				Directory.CreateDirectory(NotionImporterParameters.DefinitionFilePath + $"\\{nameof(NaniScriptImportDefinition)}");
			}

			//型名のフォルダに定義ファイルを保存
			var filePath =
				NotionImporterParameters.DefinitionFilePath +
				$"\\{nameof(NaniScriptImportDefinition)}" +
				$"\\{m_settings.DefinitionName}.json";

			var soSetting = new NaniScriptImportDefinition {
				outputPath = m_settings.OutputPath,
				definitionName = m_settings.DefinitionName,
				targetDb = m_settings.CurrentObject,
				mappingProperties = m_mappingFunction.MappintType
					.OrderBy(pair => pair.Key)
					.Select(pair => m_settings.CurrentProperty[pair.Value].id).ToArray(),
			};

			var jsonText = JsonUtility.ToJson(soSetting);

			File.WriteAllText(filePath, jsonText);
			AssetDatabase.Refresh();

			ImportMenu.RefreshImportMenu().Forget();
			Debug.Log("インポート定義を作成しました");
		}

		public void ReadFile(NotionImporterSettings settings, string json) {
			m_settings = settings;
			var def = JsonUtility.FromJson<NaniScriptImportDefinition>(json);

			var db = m_settings.objects.FirstOrDefault(obj => obj.id == def.targetDb.id);

			if (db == null) {
				EditorUtility.DisplayDialog("エラー", "定義ファイルに指定されているデータベースが存在しませんでした", "OK");

				return;
			}

			m_settings.CurrentObjectId = db.id.GetHashCode();
			ParentFunction.NotionTree.SetSelection(new List<int> { db.id.GetHashCode() });

			m_settings.DefinitionName = def.definitionName;
			m_settings.OutputPath = def.outputPath;

			m_mappingFunction.MappintType.Clear();

			foreach (MappingType type in Enum.GetValues(typeof(MappingType))) {
				m_mappingFunction.MappintType.TryAdd(type, 0);

				m_mappingFunction.MappintType[type] =
					m_settings.CurrentProperty
						.Select((prop, index) => (prop, index))
						.FirstOrDefault(prop => prop.prop.id == def.mappingProperties[(int)type])
						.index;
			}
		}

	}

}