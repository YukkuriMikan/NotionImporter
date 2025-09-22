using System;
using System.Reflection;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

	/// <summary> 型マッピング用クラス </summary>
	public class MappingItem {

		public bool doMaching = true;
		public bool isArray = false;
		public bool isList = false;
		public string fieldName;
		public Type fieldType;
		public FieldInfo fieldInfo;
		public FieldInfo[] innerFieldInfo;

		public NotionProperty[] targetProperties;

		public int propertyIndex;

	}

}