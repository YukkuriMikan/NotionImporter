using System;

namespace NotionImporter {

        /// <summary>Notionのテキスト要素を表します。</summary>
        [Serializable]
        public class NotionText {

                public string plain_text; // 表示用のプレーンテキスト
                public string href; // リンクされているURL

                /// <summary>テキストをプレーンな文字列として取得します。</summary>
                public static implicit operator string(NotionText str) => str.plain_text ?? "";

        }

}