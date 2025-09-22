using System;

namespace NotionImporter {

	[Serializable]
	public class NotionObject {

		public NotionObjectType objectType;
		public string           @object;
		public string           id;
		public string           description;
		public string           url;
		public bool             archived;
		public NotionText[]     title;
		public NotionProperty[] properties;
		public NotionParent     parent;

		#region 非シリアライズ要素
		public string MainTitle {
			get {
				return title == null ? "" : title[0];
			}
		}
		#endregion

	}

}