using UnityEngine;

namespace NotionImporter {

	public interface ISetScriptableObjectFieldFunction {

		public string FunctionName { get; }

		public bool SetField(ScriptableObject so, string fieldName, string value);

	}

}