using System;

namespace NotionImporter {

	[Serializable]
	public class SearchResult {

		public string @object;

		public NotionObject[] results;

		public bool has_more;

		public string next_cursor;

		public string type;

		public NotionObject page;


	}

}