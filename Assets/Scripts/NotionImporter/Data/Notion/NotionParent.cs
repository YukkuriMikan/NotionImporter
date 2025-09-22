using System;

namespace NotionImporter {

	[Serializable]
	public class NotionParent {

		public string type;
		public string page_id;
		public string database_id;

		#region 非シリアライズ要素
		public string Id {
			get {
				return string.IsNullOrWhiteSpace(page_id) ? database_id : page_id;
			}
		}
		#endregion

	}

}