using System;

namespace NotionImporter.Functions.SubFunction.NaniScripts {

        /// <summary>NaniScriptのマッピング種別を表します。</summary>
        public enum MappingType {

                SortKey, // 並び替えに利用するキー
                CharacterName, // キャラクター名
                CommandName, // コマンド名
                IsTogaki, // ト書きかどうかのフラグ
                Contents, // セリフ内容
                Comments, // コメント欄

        }

        /// <summary>MappingTypeに関する拡張機能です。</summary>
        public static class MappingTypeEx {

                /// <summary>種別に応じた表示名を取得します。</summary>
                public static string GetName(this MappingType type) => type switch { // 列挙値から日本語名を返す
                        MappingType.SortKey => "ソートキー",
                        MappingType.CharacterName => "キャラクター名",
			MappingType.CommandName => "コマンド名",
			MappingType.IsTogaki => "ト書きフラグ",
			MappingType.Contents => "内容",
			MappingType.Comments => "コメント",
			_ => throw new ArgumentException($"想定していないタイプ{type.ToString()}が指定されました"),
		};

	}

}