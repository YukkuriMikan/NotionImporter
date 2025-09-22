using System;

namespace NotionImporter.Functions.SubFunction.NaniScripts {

	public enum MappingType {

		SortKey,
		CharacterName,
		CommandName,
		IsTogaki,
		Contents,
		Comments,

	}

	public static class MappingTypeEx {

		public static string GetName(this MappingType type) => type switch {
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