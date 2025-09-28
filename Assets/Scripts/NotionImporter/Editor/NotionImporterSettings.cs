using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codeplex.Data;
using UnityEditor;
using UnityEngine;

namespace NotionImporter {
	/// <summary>Notionインポータの関連設定を保持します。</summary>
	[Serializable]
	public class NotionImporterSettings {
		#region Serialized Fields and Properties
		public string apiKey; // NotionのAPIキー

		public bool connectionSucceed; // 接続が成功したかどうか

		private string m_definitionName; // インポート定義名の内部保持用データ

		/// <summary>インポート定義名</summary>
		public string DefinitionName {
			get => m_definitionName?.Replace('/', '／'); // スラッシュは全角に置換
			set => m_definitionName = value?.Replace('/', '／');
		}

		public NotionObject[] objects; // インポート対象のNotionデータベース
		#endregion

		#region 非シリアライズ部
		/// <summary>インポートファイル出力フォルダ</summary>
		public string OutputPath { get; set; } // 保存対象外の出力先パス

		private string m_dbSearchResultRawJson; // DBサーチ結果のJSON(保存しない)

		private int m_currentObjectId = 0; // 現在選択されているDBのインデックス

		/// <summary>現在選択されているDBのインデックスを公開します。</summary>
		public int CurrentObjectId {
			get => m_currentObjectId;
			set => m_currentObjectId = value;
		}

		/// <summary>現在選択されているデータベースを取得します。</summary>
		public NotionObject CurrentObject {
			get {
				return objects.FirstOrDefault(obj => obj.id.GetHashCode() == CurrentObjectId); // objects 内から CurrentObjectId に一致するものを取得（id.GetHashCode() をキーにしているため衝突に注意）
			}
		}

		/// <summary>現在選択中のデータベースプロパティを取得します。</summary>
		public NotionProperty[] CurrentProperty {
			get {
				var obj = CurrentObject;

				if(CurrentObject.objectType == NotionObjectType.Container) {                                                                    // 注意: CurrentObject が null の場合は NullReference の可能性あり（仕様準拠）
					obj = objects.FirstOrDefault(obj => obj.parent.page_id == CurrentObject.id && obj.objectType == NotionObjectType.Database); // コンテナ直下のデータベースを探索して、そのプロパティを参照
				}

				return obj.properties;
			}
		}

		/// <summary>取り込み時のキーとするカラム名を保持します。</summary>
		public string KeyId { get; set; } // 取り込みキー列名

                /// <summary>キーフィルタリングの使用有無を示します。</summary>
                public bool UseKeyFiltering { get; set; } = false;

                /// <summary>コレクション出力時に使用するソートキーです。</summary>
                public string SortKey { get; set; } // 現在選択されているソート対象フィールド

                /// <summary>コレクション出力時のソート順設定です。</summary>
                public SortOrder SortOrder { get; set; } = SortOrder.Ascending; // ソート順（未設定時は昇順）

                /// <summary>Notionから最新のデータベース情報を取得します。</summary>
                public void RefreshDatabaseInfo() {
			try {
				var searchQuery = JsonUtility.ToJson(new SearchQuery()); // Notion APIを呼び出して内部状態を更新

				m_dbSearchResultRawJson = NotionApi.PostNotion(apiKey, "search", searchQuery); // 1) データベース一覧を検索（検索APIはページ等も返す）

				if(string.IsNullOrWhiteSpace(m_dbSearchResultRawJson)) {
					connectionSucceed = false; // 通信/認証エラーなど
					return;
				}

				var dbSearchResult = JsonUtility.FromJson<SearchResult>(m_dbSearchResultRawJson);
				var targetNotionObjects = dbSearchResult.results.ToList();

				foreach (var obj in targetNotionObjects) { // 2) データベースのプロパティを抽出・整形
					obj.objectType = NotionObjectType.Database;

					GetProperties(obj, m_dbSearchResultRawJson); // 検索APIのレスポンスから、該当DBのプロパティのみを引き当てる
				}

				var parentList = targetNotionObjects.ToList(); // 3) データベースの親コンテナ（ページ）を再帰的に収集

				while (true) { // 親チェーンを辿り、最上位コンテナまで収集
					foreach (var parent in parentList.ToArray()) {
						parentList.Remove(parent);

						if(string.IsNullOrWhiteSpace(parent.parent.page_id)) { // 親IDが無い場合はルート要素と見なし parent を null にする
							parent.parent = null;
							continue;
						}

						var resultPageJson = NotionApi.GetNotion(apiKey, $"pages/{parent.parent.page_id}"); // 親ページ情報を取得（見つからない場合はルート扱い）

						if(!NotionApi.IsSearchError(resultPageJson)) {
							var page = JsonUtility.FromJson<NotionObject>(resultPageJson);

							if(!targetNotionObjects.Any(obj => page.id == obj.id)) { // 未収集の親コンテナだけ追加
								page.objectType = NotionObjectType.Container;
								targetNotionObjects.Add(page);

								GetContainerTitle(page, resultPageJson); // 表示用タイトルを抽出（ページの title プロパティから）

								parentList.Add(page); // さらに上位の親を辿るために探索キューへ
							}
						} else {
							parent.parent = null; // 親を取得できない場合はルート扱いにして探索打ち切り
						}
					}

					if(parentList.Count == 0) break; // 追加の親オブジェクトが一つも見つからない場合は終了
				}

				objects = targetNotionObjects.ToArray(); // 4) 検索・収集結果を設定
				connectionSucceed = true;                // 一連の処理が成功
			} catch (Exception ex) {
				connectionSucceed = false; // 失敗した場合は接続失敗扱いにする
				throw new InvalidOperationException("Notionのデータベース情報の更新に失敗しました。", ex);
			}
		}

		/// <summary>ページ（コンテナ）のタイトルを抽出し設定します。</summary>
		private void GetContainerTitle(NotionObject obj, string json) {
			if(obj.properties == null || obj.properties.Length == 0) { // 既にプロパティが設定済みならスキップ（冪等性確保）
				var dynamicResults = DynamicJson.Parse(json);

				obj.title = new NotionText[1] {
					new() {
						plain_text = dynamicResults.properties.title.title[0].plain_text
					}
				}; // Notionのページタイトル構造に合わせて plain_text を取得
			}
		}

		/// <summary>データベースのプロパティを取得します。</summary>
		/// <param name="obj">取得対象のデータベース</param>
		private void GetProperties(NotionObject obj, string json) {
			if(obj.properties == null || obj.properties.Length == 0) { // 既にプロパティが埋まっている場合は再解析しない
				var dynamicResults = DynamicJson.Parse(json);

				foreach (var result in dynamicResults.results) { // search 結果から対象DB（id一致）を探し、プロパティ一覧を構築
					if(result.id == obj.id) {
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
		/// <summary>設定を読み込みます。</summary>
		public static NotionImporterSettings LoadSetting() {
			try {
				var importerSettings = new NotionImporterSettings(); // 設定ファイルからデータを取得

				if(File.Exists(NotionImporterParameters.SettingFilePath)) {
					var json = File.ReadAllText(NotionImporterParameters.SettingFilePath);

					EditorJsonUtility.FromJsonOverwrite(json, importerSettings); // 既存インスタンスに対して上書き（参照の差し替えを避ける）
					return importerSettings;
				}

				return null; // 設定ファイルが存在しない場合は null
			} catch (Exception ex) {
				throw new IOException("設定ファイルの読み込みに失敗しました。", ex);
			}
		}

		/// <summary>設定を保存します。</summary>
		/// <param name="setting">保存する設定クラス、省略でメンバ変数の設定を保存</param>
		public static void SaveSetting(NotionImporterSettings setting) {
			if(setting == null) {
				throw new ArgumentNullException(nameof(setting)); // null引数を明示的に拒否
			}

			try {
				var json = JsonUtility.ToJson(setting); // インポータ設定を書き出し

				var settingDirectory = Path.GetDirectoryName(NotionImporterParameters.SettingFilePath); // 設定保存用ディレクトリを取得

				if(!string.IsNullOrEmpty(settingDirectory) && !Directory.Exists(settingDirectory)) {
					Directory.CreateDirectory(settingDirectory); // 事前にフォルダを生成して出力先を保証
				}

				File.WriteAllText(NotionImporterParameters.SettingFilePath, json); // 設定ファイルへ書き出し

				AssetDatabase.Refresh(); // Unity エディタにアセット変更を通知
			} catch (Exception ex) {
				throw new IOException("設定ファイルの保存に失敗しました。", ex);
			}
		}
		#endregion
	}
}
