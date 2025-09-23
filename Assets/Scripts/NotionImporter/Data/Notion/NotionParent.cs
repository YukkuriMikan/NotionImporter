using System;

namespace NotionImporter {

        /// <summary>Notionオブジェクトの親情報を保持します。</summary>
        [Serializable]
        public class NotionParent {

                public string type; // 親のタイプ
                public string page_id; // 親ページのID
                public string database_id; // 親データベースのID

		#region 非シリアライズ要素
                /// <summary>親のページまたはデータベースIDを取得します。</summary>
                public string Id {
                        get {
                                return string.IsNullOrWhiteSpace(page_id) ? database_id : page_id; // ページIDが無ければデータベースIDを返す
                        }
                }
		#endregion

	}

}