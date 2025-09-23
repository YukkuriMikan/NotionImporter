using System;

namespace NotionImporter {

	/// <summary>Notion検索の条件を表します。</summary>
	[Serializable]
	public class SearchQuery {

		public string query; // 検索文字列

		public FilterParam filter; // フィルタリングオプション

		public SortParam sort; // ソートオプション

		/// <summary>既定値で検索条件を初期化します。</summary>
		public SearchQuery() { } // デフォルト状態で空の検索条件を保持

		/// <summary>指定した文字列で検索条件を初期化します。</summary>
		/// <param name="searchQuery">検索文字列</param>
		public SearchQuery(string searchQuery) {
			this.query = searchQuery; // 入力された検索文字列を保持
		}
	}
}
