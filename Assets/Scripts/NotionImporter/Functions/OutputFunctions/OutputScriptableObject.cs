using System;
using System.Linq;
using System.Reflection;
using Codeplex.Data;
using Kokorowa;
using NotionImporter.Functions.SubFunction.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace NotionImporter.Functions.Output {

	/// <summary> スクリプタブルオブジェクトを出力 </summary>
	public class OutputScriptableObject : IOutputFunction {

		public Type DefinitionType {
			get {
				return typeof(ScriptableObjectImportDefinition);
			}
		}

		private ISetScriptableObjectFieldFunction[] soFunctions = {
			new DefaultSOFieldFunction(),
		};

		private int m_setFieldFunctionIndex = 0;

		private NotionImporterSettings m_settings;

		public void DrawFunction(NotionImporterSettings settings) {
			m_setFieldFunctionIndex = EditorGUILayout.Popup("出力方式", m_setFieldFunctionIndex,
				soFunctions.Select(func => func.FunctionName).ToArray());
		}

		/// <summary> ファイル出力 </summary>
		/// <param name="importDefinition">インポート定義</param>
		/// <param name="props">インポート対象プロパティ</param>
		public async UniTask OutputFile(string fileName, NotionImporterSettings settings, ImportDefinitionBase importDefinition,
			NotionObject[] pages) {
			m_settings = settings;

			var def = importDefinition as ScriptableObjectImportDefinition;
			var soType = Type.GetType(def.targetScriptableObject);
			ScriptableObject so = default;

			switch(def.mappingMode) {
				case MappingMode.Array:
					var targetTypeList = new (string, object)[pages.Length]; //キー文字列、値

					async UniTask GetArrayData(int index) {
						targetTypeList[index] = ("", Activator.CreateInstance(def.targetFieldType.targetType.GetElementType()));

						var resultPageJson = await NotionApi.GetNotionAsync(m_settings.apiKey, $"pages/{pages[index].id}");
						var resultPage = DynamicJson.Parse(resultPageJson);
						var props = resultPage.properties;

						Debug.Log($"NotionImporter:{index}回目 ページID「{pages[index].id}」取得完了");

						foreach (var dat in def.mappingData) {
							var targetPropId = dat.targetPropertyId;

							foreach (dynamic prop in (object[])props) {
								var id = prop.Value.id.ToString();

								if(!string.IsNullOrWhiteSpace(def.keyProperty)) { // キー列設定があるか？
									if(prop.Value.id == def.keyProperty) { // キー列と一致してたらセット
										var propValue = await NotionUtils.GetStringProperty(m_settings, prop.Value);

										targetTypeList[index] = SetObjectField(targetTypeList[index], "Item1", propValue);
									}
								}

								if(prop.Value.id == targetPropId) {
									var propValue = await NotionUtils.GetStringProperty(m_settings, prop.Value);

									SetObjectField(targetTypeList[index].Item2, dat.targetFieldName, propValue.ToString()); // breakするとキーが設定出来ないケースがあるため全部回す
								}
							}
						}
					}

					await UniTask.WhenAll(Enumerable.Range(0, pages.Length).Select(GetArrayData));

					if(string.IsNullOrWhiteSpace(def.keyProperty)) {
						OutputArrayScriptableObject(def, fileName, targetTypeList.Select(targetTypeItm => targetTypeItm.Item2).ToArray(),
							soType);
					} else {
						var targets = targetTypeList.GroupBy(itm => itm.Item1);

						if(def.useKeyFiltering) {
							PopupFilteringWindow.Open(
								targets.Select(itm => itm.Key).ToArray(),
								filter => {
									foreach (var groupedItm in targets) {
										if(groupedItm.Key == filter) {
											OutputArrayScriptableObject(def, groupedItm.Key, groupedItm.Select(itm => itm.Item2).ToArray(),
												soType);
										}
									}
								});

							break;
						}

						foreach (var groupedItm in targets) { // キー列設定無し
							if(string.IsNullOrWhiteSpace(groupedItm.Key)) continue;

							if(def.definitionName.Contains("$K")) {
								var defName = def.definitionName;
								var assetName = defName.Replace("$K", groupedItm.Key);

								OutputArrayScriptableObject(def,
									assetName,
									groupedItm.Select(itm => itm.Item2).ToArray(), soType);
							} else {
								OutputArrayScriptableObject(def, groupedItm.Key, groupedItm.Select(itm => itm.Item2).ToArray(), soType);
							}
						}
					}

					break;

				default:
					foreach (var page in pages) { // ページ=Notionの表の行
						var resultPageJson = await NotionApi.GetNotionAsync(m_settings.apiKey, $"pages/{page.id}");
						var props = DynamicJson.Parse(resultPageJson).properties;
						so = ScriptableObject.CreateInstance(soType);

						Debug.Log($"NotionImporter: ページ「{page.id}」取得完了");

						foreach (var prop in props) {
							if(prop.Value.type == "title" && ((object[])prop.Value.title).Length != 0) {
								fileName = prop.Value.title[0].plain_text;

								break;
							}
						}

						if(string.IsNullOrWhiteSpace(fileName)) {
							Debug.Log("ファイル名が設定されていないレコード");

							return;
						}

						var existFile = AssetDatabase.LoadAssetAtPath(def.outputPath + $"\\{fileName}.asset", // 既存のファイルを探す
							Type.GetType(def.targetScriptableObject));

						if(existFile != null) {
							so = (ScriptableObject)existFile;
						} else {
							AssetDatabase.CreateAsset(so, def.outputPath + $"\\{fileName}.asset");
						}

						foreach (var dat in def.mappingData) {
							var targetPropId = dat.targetPropertyId;

                                                        foreach (var prop in props) {
                                                                if(prop.Value.id == targetPropId) {
                                                                        var propValue = await NotionUtils.GetStringProperty(m_settings, prop.Value); // 非同期結果を待って文字列を取得

                                                                        SetObjectField(so, dat.targetFieldName, propValue);

									break;
								}
							}
						}
					}

					EditorUtility.SetDirty(so);
					AssetDatabase.SaveAssets();
					Debug.Log($"NotionImporter: {fileName} Imported.");

					break;
			}
		}

		public ImportDefinitionBase Deserialize(string json) => JsonUtility.FromJson<ScriptableObjectImportDefinition>(json);

		public void OutputArrayScriptableObject(ScriptableObjectImportDefinition def, string assetName, object[] soValues, Type soType) {
			var existFile = AssetDatabase.LoadAssetAtPath(def.outputPath + $"\\{assetName}.asset", soType);
			ScriptableObject so = default;

			if(existFile != null) {
				so = (ScriptableObject)existFile;
			} else {
				so = ScriptableObject.CreateInstance(soType);
				AssetDatabase.CreateAsset(so, def.outputPath + $"\\{assetName}.asset");
			}

			var arrayType = def.targetFieldType.targetType; // キー列設定あり
			var instance = Activator.CreateInstance(arrayType, soValues.Length);
			var array = instance as Array;

			for (int i = 0; i < soValues.Length; i++) {
				array.SetValue(soValues[i], i);
			}

			so.SetField(def.targetFieldName, array);

			EditorUtility.SetDirty(so);
			AssetDatabase.SaveAssets();
			Debug.Log($"NotionImporter: {assetName} Imported.");
		}

		private object SetObjectField(object targetObject, string fieldName, string value) {
			var field = targetObject.GetType()
				.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			var fieldType = field.FieldType;

			try {
				switch(fieldType) {
					case Type t when //Parseでいける型全部まとめ
						t == typeof(byte) ||
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
						targetObject.SetField(fieldName, parsedVal);

						break;

					case Type t when t.BaseType == typeof(Enum):
						if(t.CustomAttributes.Any(attr => attr.AttributeType == typeof(FlagsAttribute))) { // Flags付きのフィールドか確認
							var values = value.Split('\t');
							int flagsEnumVal = 0;

							foreach (var val in values) {

								if(string.IsNullOrEmpty(val))
									continue;

								if(Enum.TryParse(t, val, out var enumVal)) {
									flagsEnumVal |= (int)enumVal;
								} else {
									Debug.LogError($"NotionImporter: 「{value}」は「{fieldName}」にインポート出来ない値です");
								}
							}

							targetObject.SetField(fieldName, flagsEnumVal);
						} else {
							if(Enum.TryParse(t, value, out var enumVal)) {
								targetObject.SetField(fieldName, enumVal);
							} else {
								Debug.LogError($"NotionImporter: 「{value}」は「{fieldName}」にインポート出来ない値です");
							}
						}

						break;

					case Type t when t == typeof(string[]):
						var stringArray = value.Split('\t', StringSplitOptions.RemoveEmptyEntries);

						targetObject.SetField(fieldName, stringArray.ToArray());

						break;

					default:
						targetObject.SetField(fieldName, value);

						break;
				}
			} catch (Exception ex) {
				Debug.LogError(ex);

				return targetObject;
			}

			return targetObject;
		}

	}

}
