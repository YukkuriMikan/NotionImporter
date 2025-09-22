using System;

namespace NotionImporter {

	/// <summary> 検索クエリ </summary>
	[Serializable]
	public class SearchQuery {

		/// <summary> 検索文字列 </summary>
		public string    query;

		/// <summary> フィルタリングオプション </summary>
		public FilterParam filter;

		/// <summary> ソートオプション </summary>
		public SortParam sort;

		/// <summary> コンストラクタ </summary>
		public SearchQuery() { }

		/// <summary> コンストラクタ </summary>
		/// <param name="searchQuery">検索文字列</param>
		public SearchQuery(string searchQuery) {
			this.query = searchQuery;
		}
	}
}