using System;

namespace NotionImporter {

	[Serializable]
	public class MappingData {

		public string         targetFieldName;
		public string         targetPropertyName;
		public string         targetPropertyId;
		public DbPropertyType targetPropertyType;

	}

}