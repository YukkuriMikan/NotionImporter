using System;

namespace NotionImporter {

	/// <summary>Notionから受け取るオブジェクト情報を保持します。</summary>
	[Serializable]
	public class NotionObject {

		public NotionObjectType objectType; // オブジェクトの種別
		public string @object;              // レスポンスのオブジェクト文字列
		public string id;                   // オブジェクトのID
		public string description;          // オブジェクトの説明
		public string url;                  // オブジェクトのURL
		public bool archived;               // アーカイブ済みかどうか
		public NotionText[] title;          // タイトル文字列の配列
		public NotionProperty[] properties; // プロパティ情報の配列
		public NotionParent parent;         // 親オブジェクト情報

		#region 非シリアライズ要素
		/// <summary>タイトル配列から表示用の文字列を取得します。</summary>
		public string MainTitle {
			get {
				return title == null ? "" : title[0]; // タイトルが存在しない場合は空文字を返す
			}
		}
		#endregion

	}

}
