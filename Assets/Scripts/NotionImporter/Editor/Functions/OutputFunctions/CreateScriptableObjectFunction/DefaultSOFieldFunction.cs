using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NotionImporter {

	public class DefaultSOFieldFunction : ISetScriptableObjectFieldFunction {

		public string FunctionName {
			get {
				return "デフォルト出力";
			}
		}

		public bool SetField(ScriptableObject so, string fieldName, string value) {
			var field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var fieldType = field.FieldType;

			try {
				switch(fieldType) {
					case Type t when t == typeof(byte) || // Parseで処理可能なプリミティブ型をまとめて処理
						t == typeof(Char) ||
						t == typeof(short) ||
						t == typeof(ushort) ||
						t == typeof(int) ||
						t == typeof(uint) ||
						t == typeof(long) ||
						t == typeof(ulong) ||
						t == typeof(float) ||
						t == typeof(double) ||
						t == typeof(decimal) ||
						t == typeof(bool):

						var parseMethod = fieldType.GetMethod("Parse", new[] {
							typeof(string)
						});
						var parsedVal = parseMethod.Invoke(null, new[] {
							value
						});
						so.SetField(fieldName, parsedVal);

						break;

					case Type t when t.BaseType == typeof(Enum):
						if(t.CustomAttributes.Any(attr => attr.AttributeType == typeof(FlagsAttribute))) { // Flags付きのフィールドか確認
							var values = value.Split(',');
							int flagsEnumVal = 0;

							foreach (var val in values) {
								if(Enum.TryParse(t, val, out var enumVal)) {
									flagsEnumVal |= (int)enumVal;
								} else {
									Debug.LogError($"NotionImporter: 「{value}」は「{fieldName}」にインポート出来ない値です");
								}

							}

							so.SetField(fieldName, flagsEnumVal);

						} else {
							if(Enum.TryParse(t, value, out var enumVal)) {
								so.SetField(fieldName, enumVal);
							} else {
								Debug.LogError($"NotionImporter: 「{value}」は「{fieldName}」にインポート出来ない値です");
							}
						}

						break;

					default:
						so.SetField(fieldName, value);

						break;

				}
			} catch (Exception) {
				return false;
			}


			return true;

		}

	}

}
