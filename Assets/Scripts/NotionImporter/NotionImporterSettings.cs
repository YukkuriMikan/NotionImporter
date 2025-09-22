using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codeplex.Data;
using UnityEditor;
using UnityEngine;

namespace NotionImporter {
	/// <summary> Notionインポータの関連設定 </summary>
	[Serializable]
	public class NotionImporterSettings {
		#region Serialized Fields and Properties
		/// <summary> NotionのAPIキー </summary>
		public string apiKey;

		/// <summary> 接続は成功したか？ </summary>
		public bool connectionSucceed;

		/// <summary> インポート定義名の元データ（内部保持用）</summary>
		private string m_definitionName;

		/// <summary> インポート定義名 </summary>
		public string DefinitionName {
			get => m_definitionName?.Replace('/', '／'); // スラッシュは全角に置換
			set => m_definitionName = value?.Replace('/', '／');
		}

		/// <summary> インポート対象のNotionデータベース </summary>
		public NotionObject[] objects;
		#endregion

		#region 非シリアライズ部
		/// <summary> インポートファイル出力フォルダ </summary>
		public string OutputPath { get; set; } // 保存対象外の出力先パス

		/// <summary> DBサーチ結果のJSON(保存しない) </summary>
		private string m_dbSearchResultRawJson;

		/// <summary> 現在選択されているDBのインデックス </summary>
		private int m_currentObjectId = 0;

		/// <summary> 現在選択されているDBのインデックス(公開用) </summary>
		public int CurrentObjectId {
			get => m_currentObjectId;
			set => m_currentObjectId = value;
		}

		/// <summary> 現在選択されているデータベース </summary>
		public NotionObject CurrentObject {
			get {
				// objects 内から CurrentObjectId に一致するものを取得
				// 備考: id.GetHashCode() をキーにしているため衝突に注意
				return objects.FirstOrDefault(obj => obj.id.GetHashCode() == CurrentObjectId);
			}
		}

		/// <summary> 現在選択されているDBのプロパティ、コンテナの場合は直下DBのプロパティ </summary>
		public NotionProperty[] CurrentProperty {
			get {
				var obj = CurrentObject;

				// 注意: CurrentObject が null の場合は NullReference の可能性あり（仕様準拠）
				if (CurrentObject.objectType == NotionObjectType.Container) {
					// コンテナ直下のデータベースを探索して、そのプロパティを参照
					obj = objects.FirstOrDefault(obj => obj.parent.page_id == CurrentObject.id && obj.objectType == NotionObjectType.Database);
				}

				return obj.properties;
			}
		}

		/// <summary> 取り込み時のキーとするカラム(キーがない場合はNullか空文字) </summary>
		public string KeyId { get; set; } // 取り込みキー列名

		/// <summary> キーフィルタリングの使用フラグ </summary>
		public bool UseKeyFiltering { get; set; } = false;

		/// <summary> DB情報を更新 </summary>
		public void RefreshDatabaseInfo() {
			var searchQuery = JsonUtility.ToJson(new SearchQuery());

			// 1) データベース一覧を検索（検索APIはページ等も返す）
			m_dbSearchResultRawJson = NotionApi.PostNotion(apiKey, "search", searchQuery);

			if (string.IsNullOrWhiteSpace(m_dbSearchResultRawJson)) {
				connectionSucceed = false; // 通信/認証エラーなど
				return;
			}

			var dbSearchResult = JsonUtility.FromJson<SearchResult>(m_dbSearchResultRawJson);
			var targetNotionObjects = dbSearchResult.results.ToList();

			// 2) データベースのプロパティを抽出・整形
			foreach (var obj in targetNotionObjects) {
				obj.objectType = NotionObjectType.Database;

				// 検索APIのレスポンスから、該当DBのプロパティのみを引き当てる
				GetProperties(obj, m_dbSearchResultRawJson);
			}

			// 3) データベースの親コンテナ（ページ）を再帰的に収集
			var parentList = targetNotionObjects.ToList();

			// 親チェーンを辿り、最上位コンテナまで収集
			while (true) {
				foreach (var parent in parentList.ToArray()) {
					parentList.Remove(parent);

					// 親IDが無い場合はルート要素と見なし parent を null にする
					if (string.IsNullOrWhiteSpace(parent.parent.page_id)) {
						parent.parent = null;
						continue;
					}

					// 親ページ情報を取得（見つからない場合はルート扱い）
					var resultPageJson = NotionApi.GetNotion(apiKey, $"pages/{parent.parent.page_id}");

					if (!NotionApi.IsSearchError(resultPageJson)) {
						var page = JsonUtility.FromJson<NotionObject>(resultPageJson);

						// 未収集の親コンテナだけ追加
						if (!targetNotionObjects.Any(obj => page.id == obj.id)) {
							page.objectType = NotionObjectType.Container;
							targetNotionObjects.Add(page);

							// 表示用タイトルを抽出（ページの title プロパティから）
							GetContainerTitle(page, resultPageJson);

							// さらに上位の親を辿るために探索キューへ
							parentList.Add(page);
						}
					} else {
						// 親を取得できない場合はルート扱いにして探索打ち切り
						parent.parent = null;
					}
				}

				// 追加の親オブジェクトが一つも見つからない場合は終了
				if (parentList.Count == 0) break;
			}

			// 4) 検索・収集結果を設定
			objects = targetNotionObjects.ToArray();
			connectionSucceed = true; // 一連の処理が成功
		}

		/// <summary> ページ（コンテナ）のタイトルを抽出し設定 </summary>
		private void GetContainerTitle(NotionObject obj, string json) {
			// 既にプロパティが設定済みならスキップ（冪等性確保）
			if (obj.properties == null || obj.properties.Length == 0) {
				var dynamicResults = DynamicJson.Parse(json);

				// Notionのページタイトル構造に合わせて plain_text を取得
				obj.title = new NotionText[1] { new() { plain_text = dynamicResults.properties.title.title[0].plain_text } };
			}
		}

		/// <summary> データベースのプロパティを取得 </summary>
		/// <param name="obj">取得対象のデータベース</param>
		private void GetProperties(NotionObject obj, string json) {
			// 既にプロパティが埋まっている場合は再解析しない
			if (obj.properties == null || obj.properties.Length == 0) {
				var dynamicResults = DynamicJson.Parse(json);

				// search 結果から対象DB（id一致）を探し、プロパティ一覧を構築
				foreach (var result in dynamicResults.results) {
					if (result.id == obj.id) {
						var dbPropertieList = new List<NotionProperty>();

						foreach (var prop in result.properties) {
							var dbProp = new NotionProperty {
								id = prop.Value.id,
								name = prop.Value.name,
								type = Enum.Parse<DbPropertyType>(prop.Value.type), // 文字列 -> Enum 変換
							};

							dbPropertieList.Add(dbProp);
						}

						obj.properties = dbPropertieList.ToArray();
						break;
					}
				}
			}
		}
		#endregion

		#region Persistence
		/// <summary> 設定のロード </summary>
		public static NotionImporterSettings LoadSetting() {
			var importerSettings = new NotionImporterSettings();

			if (File.Exists(NotionImporterParameters.SettingFilePath)) {
				var json = File.ReadAllText(NotionImporterParameters.SettingFilePath);

				// 既存インスタンスに対して上書き（参照の差し替えを避ける）
				EditorJsonUtility.FromJsonOverwrite(json, importerSettings);
				return importerSettings;
			}

			// 設定ファイルが存在しない場合は null
			return null;
		}

		/// <summary> 設定の保存 </summary>
		/// <param name="setting">保存する設定クラス、省略でメンバ変数の設定を保存</param>
		public static void SaveSetting(NotionImporterSettings setting) {
			var json = JsonUtility.ToJson(setting);

			// 設定ファイルへ書き出し
			File.WriteAllText(NotionImporterParameters.SettingFilePath, json);

			// Unity エディタにアセット変更を通知
			AssetDatabase.Refresh();
		}
		#endregion
	}
}