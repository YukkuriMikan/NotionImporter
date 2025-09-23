using System;

namespace NotionImporter {

        /// <summary>Notionの検索結果を保持します。</summary>
        [Serializable]
        public class SearchResult {

                public string @object; // レスポンスの種類

                public NotionObject[] results; // 取得したオブジェクトの一覧

                public bool has_more; // 追加で取得できるかどうか

                public string next_cursor; // 次ページのカーソル

                public string type; // 結果のタイプ

                public NotionObject page; // ページ結果の詳細


        }

}