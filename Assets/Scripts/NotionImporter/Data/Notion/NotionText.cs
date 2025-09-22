using System;

namespace NotionImporter {

	[Serializable]
	public class NotionText {

		public string plain_text;
		public string href;

		public static implicit operator string(NotionText str) => str.plain_text ?? "";

	}

}