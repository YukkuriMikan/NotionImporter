using System;

namespace NotionImporter {

	[Serializable]
	public class I2LocalizeImportDefinition : ImportDefinitionBase {

		public override string definitionType {
			get {
				return "I2Localization";
			}
		}

		public string targetSourceGuid;

		public string[] languages;

		public string[] languageIds;

	}

}