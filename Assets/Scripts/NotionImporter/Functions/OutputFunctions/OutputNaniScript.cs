using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codeplex.Data;
using NotionImporter.Functions.SubFunction.NaniScripts;
using UnityEditor;
using UnityEngine;
using UniTask = Cysharp.Threading.Tasks.UniTask;

#if false

namespace NotionImporter.Functions.Output {

	public class OutputNaniScript : IOutputFunction {

		public Type DefinitionType {
			get {
				return typeof(NaniScriptImportDefinition);
			}
		}

		public void DrawFunction(NotionImporterSettings settings) {
			throw new NotImplementedException();
		}

		public async UniTask OutputFile(string fileName, NotionImporterSettings settings, ImportDefinitionBase importDefinition,
			NotionObject[] pages) {
			var def = importDefinition as NaniScriptImportDefinition;
			var textLineList = new List<(int, string)>();
			var naniScriptFullPath = def.outputPath + "/" + fileName + ".nani";

			using (var sw = File.CreateText(naniScriptFullPath)) {
				for (int i = 0; i < pages.Length; i++) {
					var resultPageJson = await NotionApi.GetNotionAsync(settings.apiKey, $"pages/{pages[i].id}");
					var props = DynamicJson.Parse(resultPageJson).properties;
					var sortKey = 0;
					var results = new Dictionary<MappingType, string>();
					var isTogaki = false;

					for (int j = 0; j < def.mappingProperties.Length; j++) {
						foreach (var prop in props) {
							if (prop.Value.id == def.mappingProperties[j]) {
								var propType = (MappingType)j;
								var val = await NotionUtils.GetStringProperty(settings, prop.Value);

								switch (propType) {
									case MappingType.SortKey:
										sortKey = int.Parse(val);

										break;

									case MappingType.IsTogaki:
										isTogaki = Boolean.Parse(val);

										break;

									case MappingType.CharacterName:
									case MappingType.CommandName:
									case MappingType.Contents:
									case MappingType.Comments:
										results.TryAdd(propType, val);

										break;

									default:
										throw new Exception("想定しないマッピングタイプが指定されました");
								}
							}
						}
					}

					if (!string.IsNullOrWhiteSpace(results[MappingType.Comments])) {
						results[MappingType.Contents] += Environment.NewLine + ";" + results[MappingType.Comments];
					}

					if (isTogaki) {
						textLineList.Add((sortKey, ";" + results[MappingType.Contents]));
					} else if (!string.IsNullOrWhiteSpace(results[MappingType.CharacterName])) {
						textLineList.Add((sortKey, results[MappingType.CharacterName] + ": " + results[MappingType.Contents]));
					} else if (!string.IsNullOrWhiteSpace(results[MappingType.CommandName])) {
						textLineList.Add((sortKey, results[MappingType.CommandName] + " " + results[MappingType.Contents]));
					}
				}

				var orderedTextLineList = textLineList.OrderBy(line => line.Item1);

				foreach (var line in orderedTextLineList) {
					sw.WriteLine(line.Item2);
				}
			}

			AssetDatabase.Refresh(); // 追加したテキストファイルの読み込み
			Debug.Log($"NotionImporter: {fileName} Imported.");

			#region Naninovelの設定にインポートしたスクリプトを追加
			var naniScriptResourceGuids = AssetDatabase.FindAssets("t:EditorResources");
			var importedScriptGuid = AssetDatabase.AssetPathToGUID(naniScriptFullPath);

			foreach (var guid in naniScriptResourceGuids) { // 一つしか存在しないはずだけど、一応
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var naniScriptResource = AssetDatabase.LoadAssetAtPath<EditorResources>(path);
				var scriptRecordDic = naniScriptResource.GetAllRecords("Scripts");

				foreach (var pair in scriptRecordDic) {
					if (pair.Value == importedScriptGuid) {
						return; // レコードに既にスクリプトがあるので終了
					}
				}

				naniScriptResource.AddRecord("Scripts", "Scripts", fileName, importedScriptGuid);
			}
			#endregion
		}

		public ImportDefinitionBase Deserialize(string json) => JsonUtility.FromJson<NaniScriptImportDefinition>(json);

	}

}

#endif