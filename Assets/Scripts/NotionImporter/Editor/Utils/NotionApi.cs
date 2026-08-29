using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace NotionImporter {
	/// <summary>Notion APIとの通信を担当します。</summary>
	public static class NotionApi {

		private const int MAX_RETRY_COUNT = 100; // 接続時の最大リトライ回数

		private const float TIME_OUT = 10f; // 接続のタイムアウト秒数

		// Notion APIの上限は接続ごとに平均3リクエスト/秒。
		// タイマー精度による超過を避けるため、わずかに余裕を持たせて約2.86リクエスト/秒に抑える。
		private const int REQUEST_INTERVAL_MILLISECONDS = 350;

		private const string NOTION_API_URL = "https://api.notion.com/v1/{0}"; // Notion APIのベースURL

		private const long TOO_MANY_REQUESTS = 429; // レート制限時のHTTPステータスコード

		private const int DEFAULT_RETRY_AFTER_MILLISECONDS = 1000; // Retry-Afterヘッダが無い場合の待機時間

		private const int RETRY_AFTER_MARGIN_MILLISECONDS = 100; // 境界直前の再送を避けるための余裕時間

		private static Dictionary<string, string> m_apiCache = new(); // APIレスポンスのキャッシュ

		private static readonly object m_rateLimitLock = new(); // 同時呼び出し時も送信間隔を守るためのロック

		private static DateTime m_nextRequestUtc = DateTime.MinValue; // 次回リクエストを送信できる時刻

		/// <summary>次のリクエスト枠を確保できるか判定する</summary>
		/// <param name="waitMilliseconds">確保できない場合の待機ミリ秒数</param>
		/// <returns>送信枠を確保できた場合はTrue</returns>
		private static bool TryAcquireRequestSlot(out int waitMilliseconds) {
			lock (m_rateLimitLock) {
				var now = DateTime.UtcNow;

				if(now >= m_nextRequestUtc) {
					m_nextRequestUtc = now.AddMilliseconds(REQUEST_INTERVAL_MILLISECONDS);
					waitMilliseconds = 0;

					return true;
				}

				waitMilliseconds = Math.Max(1, (int)Math.Ceiling((m_nextRequestUtc - now).TotalMilliseconds));

				return false;
			}
		}

		/// <summary>Notionの推奨頻度を超えない送信枠まで同期的に待機する</summary>
		private static void WaitForRequestSlot() {
			while (!TryAcquireRequestSlot(out var waitMilliseconds)) {
				Thread.Sleep(waitMilliseconds);
			}
		}

		/// <summary>Notionの推奨頻度を超えない送信枠まで非同期に待機する</summary>
		private static async UniTask WaitForRequestSlotAsync() {
			while (!TryAcquireRequestSlot(out var waitMilliseconds)) {
				await Task.Delay(waitMilliseconds);
			}
		}

		/// <summary>429応答で指定された時刻まで、後続を含む全リクエストを停止する</summary>
		/// <param name="waitMilliseconds">待機ミリ秒数</param>
		private static void ApplyRateLimitBackoff(int waitMilliseconds) {
			lock (m_rateLimitLock) {
				var retryAt = DateTime.UtcNow.AddMilliseconds(waitMilliseconds);

				if(retryAt > m_nextRequestUtc) {
					m_nextRequestUtc = retryAt;
				}
			}
		}

		/// <summary>レート制限(429)かどうかを判定する</summary>
		/// <param name="req">対象のリクエスト</param>
		/// <returns>429ならTrue</returns>
		private static bool IsTooManyRequests(UnityWebRequest req) {
			return req != null && req.responseCode == TOO_MANY_REQUESTS; // ステータスコードで判定
		}

		/// <summary>Retry-Afterヘッダから待機すべきミリ秒数を取得する</summary>
		/// <param name="req">対象のリクエスト</param>
		/// <returns>待機ミリ秒数(Retry-After+余裕時間)</returns>
		private static int GetRetryAfterMilliseconds(UnityWebRequest req) {
			var header = req?.GetResponseHeader("Retry-After"); // Notionが返す待機時間(秒)

			if(!string.IsNullOrEmpty(header) && int.TryParse(header, out var retryAfterSeconds) && retryAfterSeconds >= 0) {
				var waitMilliseconds = (long)retryAfterSeconds * 1000 + RETRY_AFTER_MARGIN_MILLISECONDS;

				return (int)Math.Min(waitMilliseconds, int.MaxValue);
			}

			return DEFAULT_RETRY_AFTER_MILLISECONDS + RETRY_AFTER_MARGIN_MILLISECONDS;
		}

		/// <summary>NotionAPI用にリクエストヘッダ等を構成する</summary>
		/// <param name="req"></param>
		/// <param name="json"></param>
		private static void SetNotionRequestParams(string apiKey, UnityWebRequest req, string json) {
			if(json != null) { // POSTデータがある場合はアップロードハンドラを設定
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
			if(m_apiCache.ContainsKey(apiKey + method + postData)) { // キャッシュが存在すれば再利用
				return m_apiCache[apiKey + method + postData];
			}

			for(var retryCount = 0; retryCount <= MAX_RETRY_COUNT; retryCount++) {
				var req = UnityWebRequest.PostWwwForm(string.Format(NOTION_API_URL, method), "POST");

				SetNotionRequestParams(apiKey, req, postData);

				try {
					WaitForRequestSlot();

					var task = req.SendWebRequest(); // リクエストを送信してタイムアウトを監視
					var timeOutWatcher = new Stopwatch();

					timeOutWatcher.Start();

					while (!task.isDone) {
						if(timeOutWatcher.Elapsed.Seconds > TIME_OUT) {
							throw new TimeoutException();
						}
					}

					if(IsTooManyRequests(req)) { // レート制限時はRetry-Afterに従って再送
						var waitMs = GetRetryAfterMilliseconds(req);

						ApplyRateLimitBackoff(waitMs);
						Debug.LogWarning($"Post: {method} はレート制限(429)されました。{waitMs / 1000f}秒待機してリトライします。");

						continue;
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

			Debug.LogError($"リトライ回数が設定値{MAX_RETRY_COUNT}を超えました");

			return null;
		}

		/// <summary>NotionAPIにPOSTする</summary>
		/// <param name="apiKey">NotionのAPIキー</param>
		/// <param name="method">呼び出しAPI種別</param>
		/// <param name="postData">POSTデータ</param>
		/// <returns>POST結果</returns>
		public async static UniTask<string> PostNotionAsync(string apiKey, string method, string postData = null) {
			if(m_apiCache.ContainsKey(apiKey + method + postData)) { // キャッシュを優先的に返す
				return m_apiCache[apiKey + method + postData];
			}

			var retryCount = 0;
			UnityWebRequest result = null;

			while (result == null) {
				if(retryCount > MAX_RETRY_COUNT) {
					throw new HttpRequestException($"リトライ回数が設定値{MAX_RETRY_COUNT}を超えました");
				}

				using var req = UnityWebRequest.PostWwwForm(string.Format(NOTION_API_URL, method), "POST");

				try {
					SetNotionRequestParams(apiKey, req, postData);
					await WaitForRequestSlotAsync();
					result = await req.SendWebRequest();

					Debug.Log($"Post: {method} succeed.");

					var resultStr = req.downloadHandler.text;

					m_apiCache.TryAdd(apiKey + method + postData, resultStr); // レスポンスをキャッシュに保存

					return resultStr;
				} catch (Exception ex) {
					retryCount++;
					result = null;

					if(IsTooManyRequests(req)) { // レート制限時はRetry-Afterに従ってリトライ
						var waitMs = GetRetryAfterMilliseconds(req);

						ApplyRateLimitBackoff(waitMs);
						Debug.LogWarning($"Post: {method} はレート制限(429)されました。{waitMs / 1000f}秒待機してリトライします。");

						continue;
					}

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
			if(m_apiCache.ContainsKey(apiKey + method)) { // キャッシュ済みの結果を優先的に返す
				return m_apiCache[apiKey + method];
			}

			for(var retryCount = 0; retryCount <= MAX_RETRY_COUNT; retryCount++) {
				var req = UnityWebRequest.Get(string.Format(NOTION_API_URL, method));

				SetNotionRequestParams(apiKey, req, null);

				try {
					WaitForRequestSlot();

					var task = req.SendWebRequest(); // リクエスト送信とタイムアウト監視
					var timeOutWatcher = new Stopwatch();

					timeOutWatcher.Start();

					while (!task.isDone) {
						if(timeOutWatcher.Elapsed.Seconds > TIME_OUT) {
							throw new TimeoutException();
						}
					}

					if(IsTooManyRequests(req)) { // レート制限時はRetry-Afterに従って再送
						var waitMs = GetRetryAfterMilliseconds(req);

						ApplyRateLimitBackoff(waitMs);
						Debug.LogWarning($"Get: {method} はレート制限(429)されました。{waitMs / 1000f}秒待機してリトライします。");

						continue;
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

			Debug.LogError($"リトライ回数が設定値{MAX_RETRY_COUNT}を超えました");

			return null;
		}

		/// <summary>NotionAPIにGETする</summary>
		/// <param name="apiKey">NotionのAPIキー</param>
		/// <param name="method">呼び出しAPI種別</param>
		/// <returns>GET結果</returns>
		public async static UniTask<string> GetNotionAsync(string apiKey, string method) {
			if(m_apiCache.ContainsKey(apiKey + method)) { // キャッシュ済みレスポンスを利用
				return m_apiCache[apiKey + method];
			}

			var retryCount = 0;
			UnityWebRequest result = null;

			while (result == null) {
				if(retryCount > MAX_RETRY_COUNT) {
					throw new HttpRequestException($"リトライ回数が設定値{MAX_RETRY_COUNT}を超えました");
				}

				using var req = UnityWebRequest.Get(string.Format(NOTION_API_URL, method));

				try {
					SetNotionRequestParams(apiKey, req, null);
					await WaitForRequestSlotAsync();
					result = await req.SendWebRequest();

					Debug.Log($"Get: {method} succeed.");

					var resultStr = result.downloadHandler.text;

					m_apiCache.TryAdd(apiKey + method, resultStr); // レスポンスをキャッシュ

					return resultStr;
				} catch (Exception ex) {
					retryCount++;
					result = null;

					if(IsTooManyRequests(req)) { // レート制限時はRetry-Afterに従ってリトライ
						var waitMs = GetRetryAfterMilliseconds(req);

						ApplyRateLimitBackoff(waitMs);
						Debug.LogWarning($"Get: {method} はレート制限(429)されました。{waitMs / 1000f}秒待機してリトライします。");

						Debug.LogError(ex);

						continue;
					}

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
