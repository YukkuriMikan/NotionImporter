using System;

namespace NotionImporter {

	[Serializable]
	public class NaniScriptImportDefinition : ImportDefinitionBase {

		public override string definitionType {
			get {
				return "NaniScript";
			}
		}

		public string[] mappingProperties;

	}

}