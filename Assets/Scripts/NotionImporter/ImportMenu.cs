using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NotionImporter.Functions.Output;
using UnityEditor;
using UnityEngine;

namespace NotionImporter {

	[InitializeOnLoad]
	public class ImportMenu {

		private const string ASSEMBLY_NAME = "Assembly-CSharp-Editor";

		private static IOutputFunction[] m_outputFunctions = {
			new OutputScriptableObject(),
		};

		static ImportMenu() {
			RefreshImportMenu().Forget();
		}

		/// <summary> ツールバーのインポートメニューを更新する </summary>
		public async static UniTask RefreshImportMenu() {
			await Task.Delay(TimeSpan.FromSeconds(1f));

			if (Directory.Exists(NotionImporterParameters.DefinitionFilePath)) {
				var importDefinitionDirectories = Directory.GetDirectories(NotionImporterParameters.DefinitionFilePath);
				var isFirst = true;
				var priority = 1;

				NotionImporterUtils.RebuildAllMenus();

				foreach (var dir in importDefinitionDirectories) {
					var targetFileFullPaths = Directory.GetFiles(dir).Where(file => Path.GetExtension(file) == ".json");

					foreach (var fileFullPath in targetFileFullPaths) {
						if (isFirst) {
							isFirst = false;
							NotionImporterUtils.AddSeparator(NotionImporterParameters.PROGRAM_ID + "/", priority++);
						}

						var itemName = NotionImporterParameters.PROGRAM_ID + "/" +
										Path.GetFileNameWithoutExtension(fileFullPath);

						if (!NotionImporterUtils.ExistsMenuItem(itemName)) {
							NotionImporterUtils.AddMenuItem(itemName, "", false, priority++,
								() => Import(Path.GetFileName(dir), fileFullPath).Forget(), null);
						}
					}
				}
			}

			NotionImporterUtils.Update();
		}

		/// <summary> インポート処理 </summary>
		/// <param name="typeString">インポート処理型の文字列</param>
		/// <param name="fileFullPath">定義ファイルのフルパス</param>
		private async static UniTask Import(string typeString, string fileFullPath) {
			var importType = Type.GetType($"NotionImporter.{typeString}, {ASSEMBLY_NAME}");
			var importDefJson = File.ReadAllText(fileFullPath);
			var subFunc = m_outputFunctions.FirstOrDefault(func => func.DefinitionType == importType);

			NotionApi.ClearCache();

			if (subFunc == null) {
				Debug.LogError($"「{Path.GetFileNameWithoutExtension(fileFullPath)}」をインポートする実装が見つかりませんでした。");
			}

			var importSettings = NotionImporterSettings.LoadSetting();

			if (importSettings == null) {
				Debug.LogError("インポート設定が見つかりませんでした");
			}

			Debug.Log($"NotionImporter: インポート定義「{Path.GetFileNameWithoutExtension(fileFullPath)}」読込完了");

			var importDef = subFunc.Deserialize(importDefJson);

			if (importDef.targetDb.objectType == NotionObjectType.Container) {
				//コンテナの子を取得するために全データベースを取得
				var searchQuery = JsonUtility.ToJson(new SearchQuery());
				var dbSearchResultRawJson = await NotionApi.PostNotionAsync(importSettings.apiKey, "search", searchQuery);

				if (string.IsNullOrWhiteSpace(dbSearchResultRawJson)) {
					EditorUtility.DisplayDialog("接続エラー", "データベースを取得出来ませんでした", "OK");

					return;
				}

				Debug.Log("NotionImporter: 全データベース取得完了");

				var dbSearchResult = JsonUtility.FromJson<SearchResult>(dbSearchResultRawJson);

				var targetNotionObjects = dbSearchResult.results.ToList()
					.Where(obj => obj.parent.Id == importDef.targetDb.id);

				foreach (var db in targetNotionObjects) {
					await InvokeOutputProcess(importSettings,
						importDef,
						subFunc,
						db.MainTitle);
				}
			} else if (importDef.targetDb.objectType == NotionObjectType.Database) {
				await InvokeOutputProcess(importSettings,
					importDef,
					subFunc,
					string.IsNullOrWhiteSpace(importSettings.DefinitionName)
						? importDef.targetDb.MainTitle
						: importSettings.DefinitionName);
			}
		}

		private async static UniTask InvokeOutputProcess(NotionImporterSettings importSettings, ImportDefinitionBase importDef,
			IOutputFunction subFunc, string fileName) {
			var resultListJson =
				await NotionApi.PostNotionAsync(importSettings.apiKey, $"databases/{importDef.targetDb.id}/query", "");

			var resultList = JsonUtility.FromJson<SearchResult>(resultListJson);
			var pages = new List<NotionObject>();

			pages.AddRange(resultList.results);

			Debug.Log($"NotionImporter: データベース「{importDef.targetDb.id}」取得完了");

			while (resultList.has_more) {
				var nextId = resultList.next_cursor;

				resultListJson =
					await NotionApi.PostNotionAsync(importSettings.apiKey,
						$"databases/{importDef.targetDb.id}/query",
						$"{{\"start_cursor\": \"{nextId}\"}}");

				resultList = JsonUtility.FromJson<SearchResult>(resultListJson);

				pages.AddRange(resultList.results);

				Debug.Log($"NotionImporter: データベース「{importDef.targetDb.id}」の取得完了");
			}

			//インポート定義に適合するアウトプットの実装を実行
			await subFunc.OutputFile(
				fileName,
				importSettings, importDef, pages.ToArray());
		}

	}

}