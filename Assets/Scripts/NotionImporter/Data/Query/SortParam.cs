using System;

/// <summary>検索結果のソート条件を保持します。</summary>
[Serializable]
public class SortParam {

        public string direction = "ascending"; // ソート方向
        public string timestamp = "last_edited_time"; // ソート対象のタイムスタンプ

}