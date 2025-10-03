using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NotionImporter.Functions;
using NotionImporter.Functions.Output;
using UnityEditor;
using UnityEngine;

namespace NotionImporter {

	[InitializeOnLoad]
	/// <summary>NotionImporterのメニュー項目を管理します。</summary>
	public class ImportMenu {

		private const string ASSEMBLY_NAME = "NotionImporter"; // 出力処理を検索する対象アセンブリ名

		private static IOutputFunction[] m_outputFunctions = Array.Empty<IOutputFunction>(); // 利用可能な出力処理一覧


		static ImportMenu() {
			m_outputFunctions = LoadOutputFunctions();
			RefreshImportMenu().Forget();
		}

		/// <summary>リフレクションで出力処理を収集します。</summary>
		private static IOutputFunction[] LoadOutputFunctions() {
			var targetAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == ASSEMBLY_NAME); // 対象アセンブリを検索

			if(targetAssembly == null) {
				Debug.LogWarning($"出力処理アセンブリ「{ASSEMBLY_NAME}」が見つかりませんでした。"); // 取得失敗を通知
				return Array.Empty<IOutputFunction>();
			}

			return targetAssembly.GetTypes()
				.Where(type => typeof(IOutputFunction).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
				.Select(type => {
					try {
						return Activator.CreateInstance(type) as IOutputFunction; // 生成成功時のみ採用
					} catch(Exception ex) {
						Debug.LogError($"出力処理の生成に失敗しました: {type.FullName}\n{ex}");
						return null;
					}
				})
				.Where(instance => instance != null)
				.ToArray();
		}

		/// <summary> ツールバーのインポートメニューを更新する </summary>
		public async static UniTask RefreshImportMenu() {
			await Task.Delay(TimeSpan.FromSeconds(1f)); // Unity起動直後の初期化待ち時間を確保

			if(Directory.Exists(NotionImporterParameters.DefinitionFilePath)) {
				var importDefinitionDirectories = Directory.GetDirectories(NotionImporterParameters.DefinitionFilePath);
				var isFirst = true;
				var priority = 1;

				NotionImporterUtils.RebuildAllMenus();

				foreach (var dir in importDefinitionDirectories) {
					var targetFileFullPaths = Directory.GetFiles(dir).Where(file => Path.GetExtension(file) == ".json");

					foreach (var fileFullPath in targetFileFullPaths) {
						if(isFirst) {
							isFirst = false;
							NotionImporterUtils.AddSeparator(NotionImporterParameters.PROGRAM_ID + "/", priority++);
						}

						var itemName = NotionImporterParameters.PROGRAM_ID + "/" +
							Path.GetFileNameWithoutExtension(fileFullPath);

						if(!NotionImporterUtils.ExistsMenuItem(itemName)) {
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
			m_outputFunctions = LoadOutputFunctions(); // ツールバー経由でも出力処理を確実に再ロードする

			var importType = Type.GetType($"NotionImporter.{typeString}, {ASSEMBLY_NAME}");
			var importDefJson = File.ReadAllText(fileFullPath);
			var subFunc = m_outputFunctions.FirstOrDefault(func => func.DefinitionType == importType);

			NotionApi.ClearCache();

			if(subFunc == null) {
				Debug.LogError($"「{Path.GetFileNameWithoutExtension(fileFullPath)}」をインポートする実装が見つかりませんでした。");
			}

			var importSettings = NotionImporterSettings.LoadSetting();

			if(importSettings == null) {
				Debug.LogError("インポート設定が見つかりませんでした");
			}

			Debug.Log($"NotionImporter: インポート定義「{Path.GetFileNameWithoutExtension(fileFullPath)}」読込完了");

			var importDef = subFunc.Deserialize(importDefJson);

			if(importDef.targetDb.objectType == NotionObjectType.Container) {
				var searchQuery = JsonUtility.ToJson(new SearchQuery()); // コンテナの子を取得するために全データベースを取得
				var dbSearchResultRawJson = await NotionApi.PostNotionAsync(importSettings.apiKey, "search", searchQuery);

				if(string.IsNullOrWhiteSpace(dbSearchResultRawJson)) {
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
			} else if(importDef.targetDb.objectType == NotionObjectType.Database) {
				await InvokeOutputProcess(importSettings,
					importDef,
					subFunc,
					string.IsNullOrWhiteSpace(importSettings.DefinitionName)
						? importDef.targetDb.MainTitle
						: importSettings.DefinitionName);
			}
		}

		/// <summary>出力関数を呼び出してファイル生成を行います。</summary>
		private async static UniTask InvokeOutputProcess(NotionImporterSettings importSettings, ImportDefinitionBase importDef,
			IOutputFunction subFunc, string fileName) {
			var resultListJson = await NotionApi.PostNotionAsync(importSettings.apiKey, $"databases/{importDef.targetDb.id}/query", ""); // 指定データベースのレコードを全て取得

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

			await subFunc.OutputFile(
				fileName,
				importSettings, importDef, pages.ToArray()); // インポート定義に適合するアウトプットの実装を実行し、取得したページを出力処理する
		}


	}

}
