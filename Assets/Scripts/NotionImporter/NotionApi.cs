using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace NotionImporter {

        /// <summary>Notion APIとの通信を担当します。</summary>
        public static class NotionApi {

                private const int MAX_RETRY_COUNT = 100; // 接続時の最大リトライ回数

                private const float TIME_OUT = 10f; // 接続のタイムアウト秒数

                private const string NOTION_API_URL = "https://api.notion.com/v1/{0}"; // Notion APIのベースURL

                private static Dictionary<string, string> m_apiCache = new(); // APIレスポンスのキャッシュ

                /// <summary>NotionAPI用にリクエストヘッダ等を構成する</summary>
                /// <param name="req"></param>
                /// <param name="json"></param>
                private static void SetNotionRequestParams(string apiKey, UnityWebRequest req, string json) {
                        if (json != null) { // POSTデータがある場合はアップロードハンドラを設定
                                var postBytes = Encoding.UTF8.GetBytes(json);

                                req.uploadHandler = new UploadHandlerRaw(postBytes);
                        }

                        req.downloadHandler = new DownloadHandlerBuffer(); // レスポンス受信用ハンドラを設定

                        req.SetRequestHeader("Authorization", $"Bearer {apiKey}"); // 認証情報とバージョン、コンテンツタイプを指定
                        req.SetRequestHeader("Notion-Version", "2022-06-28");
                        req.SetRequestHeader("Content-Type", "application/json");
                }

                /// <summary>NotionAPIにPOSTする</summary>
		/// <param name="apiKey">NotionのAPIキー</param>
		/// <param name="method">呼び出しAPI種別</param>
		/// <param name="postData">POSTデータ</param>
		/// <returns>POST結果</returns>
                public static string PostNotion(string apiKey, string method, string postData = null) {
                        if (m_apiCache.ContainsKey(apiKey + method + postData)) { // キャッシュが存在すれば再利用
                                return m_apiCache[apiKey + method + postData];
                        }

			using var req = UnityWebRequest.PostWwwForm(string.Format(NOTION_API_URL, method), "POST");

			SetNotionRequestParams(apiKey, req, postData);

                        try {
                                var task = req.SendWebRequest(); // リクエストを送信してタイムアウトを監視
                                var timeOutWatcher = new Stopwatch();

				timeOutWatcher.Start();

				while (!task.isDone) {
					if (timeOutWatcher.Elapsed.Seconds > TIME_OUT) {
						throw new TimeoutException();
					}
				}

				Debug.Log($"Post: {method} succeed.");

                                var resultStr = req.downloadHandler.text;

                                m_apiCache.TryAdd(apiKey + method + postData, resultStr); // レスポンスをキャッシュに保存

				return resultStr;
			} catch (Exception ex) {
				Debug.LogError("NotionAPIのリクエストがタイムアウトしました。" + Environment.NewLine + ex.Message);

				return null;
			} finally {
				req.Dispose();
			}
		}

                /// <summary>NotionAPIにPOSTする</summary>
		/// <param name="apiKey">NotionのAPIキー</param>
		/// <param name="method">呼び出しAPI種別</param>
		/// <param name="postData">POSTデータ</param>
		/// <returns>POST結果</returns>
                public async static UniTask<string> PostNotionAsync(string apiKey, string method, string postData = null) {
                        if (m_apiCache.ContainsKey(apiKey + method + postData)) { // キャッシュを優先的に返す
                                return m_apiCache[apiKey + method + postData];
                        }

			var retryCount = 0;
			UnityWebRequest result = null;

			while (result == null) {
				if (retryCount > MAX_RETRY_COUNT) {
					throw new HttpRequestException($"リトライ回数が設定値{MAX_RETRY_COUNT}を超えました");
				}

				try {
					using var req = UnityWebRequest.PostWwwForm(string.Format(NOTION_API_URL, method), "POST");

					SetNotionRequestParams(apiKey, req, postData);
					result = await req.SendWebRequest();

					Debug.Log($"Post: {method} succeed.");

                                        var resultStr = req.downloadHandler.text;

                                        m_apiCache.TryAdd(apiKey + method + postData, resultStr); // レスポンスをキャッシュに保存

					return resultStr;
				} catch (Exception ex) {
					retryCount++;
					Debug.LogError(ex);
				}
			}

			return null;
		}

                /// <summary>NotionAPIにGETする</summary>
		/// <param name="apiKey">NotionのAPIキー</param>
		/// <param name="method">呼び出しAPI種別</param>
		/// <returns>GET結果</returns>
                public static string GetNotion(string apiKey, string method) {
                        if (m_apiCache.ContainsKey(apiKey + method)) { // キャッシュ済みの結果を優先的に返す
                                return m_apiCache[apiKey + method];
                        }

			using var req = UnityWebRequest.Get(string.Format(NOTION_API_URL, method));

			SetNotionRequestParams(apiKey, req, null);

                        try {
                                var task = req.SendWebRequest(); // リクエスト送信とタイムアウト監視
                                var timeOutWatcher = new Stopwatch();

				timeOutWatcher.Start();

				while (!task.isDone) {
					if (timeOutWatcher.Elapsed.Seconds > TIME_OUT) {
						throw new TimeoutException();
					}
				}

                                var resultStr = req.downloadHandler.text;

                                m_apiCache.TryAdd(apiKey + method, resultStr); // レスポンスをキャッシュに保存

				return resultStr;
			} catch (Exception ex) {
				Debug.LogError("NotionAPIのリクエストがタイムアウトしました。" + Environment.NewLine + ex.Message);

				return null;
			} finally {
				req.Dispose();
			}
		}

                /// <summary>NotionAPIにGETする</summary>
		/// <param name="apiKey">NotionのAPIキー</param>
		/// <param name="method">呼び出しAPI種別</param>
		/// <returns>GET結果</returns>
                public async static UniTask<string> GetNotionAsync(string apiKey, string method) {
                        if (m_apiCache.ContainsKey(apiKey + method)) { // キャッシュ済みレスポンスを利用
                                return m_apiCache[apiKey + method];
                        }

			var retryCount = 0;
			UnityWebRequest result = null;

			while (result == null) {
				if (retryCount > MAX_RETRY_COUNT) {
					throw new HttpRequestException($"リトライ回数が設定値{MAX_RETRY_COUNT}を超えました");
				}

				try {
					using var req = UnityWebRequest.Get(string.Format(NOTION_API_URL, method));

					SetNotionRequestParams(apiKey, req, null);
					result = await req.SendWebRequest();

					Debug.Log($"Get: {method} succeed.");

                                        var resultStr = result.downloadHandler.text;

                                        m_apiCache.TryAdd(apiKey + method, resultStr); // レスポンスをキャッシュ

					return result.downloadHandler.text;
				} catch (Exception ex) {
					retryCount++;

					await Task.Delay(1000);

					Debug.LogError(ex);
				}
			}

			return null;
		}

                /// <summary>search結果がエラーかどうかを判定する</summary>
                /// <param name="json">対象のJSON</param>
                /// <returns>検索結果の有無(True=エラー、結果無し、False=結果あり)</returns>
                public static bool IsSearchError(string json) {
                        /* エラー時のJSON
                         * {"object":"error","status":404,"code":"object_not_found","message":"Could not find page with ID: 08c1542c-46fa-4296-886f-288c3c68a5b1. Make sure the relevant pages and databases are shared with your integration."}
                         */

                        return json.Contains("object_not_found") || json.Contains("gateway error"); // エラーメッセージが含まれている場合は失敗扱い
                }

                /// <summary>APIレスポンスのキャッシュを削除します。</summary>
                public static void ClearCache() {
                        m_apiCache.Clear(); // キャッシュを初期化
                }

	}

}