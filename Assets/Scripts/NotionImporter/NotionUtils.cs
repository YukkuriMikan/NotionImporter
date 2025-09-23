using System;
using System.Collections.Generic;
using System.Linq;
using Codeplex.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NotionImporter {

        /// <summary>Notionデータ操作向けのユーティリティを提供します。</summary>
        public static class NotionUtils {

		/// <summary> プロパティから文字列として値を取り出す </summary>
		/// <param name="propVal">対象のプロパティ(DynamicJSON前提)</param>
		/// <returns>文字列の値</returns>
                public async static UniTask<string> GetStringProperty(NotionImporterSettings settings, dynamic propVal) {
                        var type = Enum.Parse<DbPropertyType>(propVal["type"].ToString()); // プロパティ種別ごとに文字列化の方法を切り替える

			switch (type) {
				case DbPropertyType.rollup:
					if (((object[])propVal.rollup.array).Length == 0) { // データ無しの場合
						return null;
					}

					return propVal.rollup.array[0].rich_text[0].plain_text;

				case DbPropertyType.relation:
					if (propVal.relation.IsArray) {
						var titleList = new string[((object[])propVal.relation).Length];

						try {
							async UniTask GetNotionString(dynamic rel, int index) {
								var resultJson = await NotionApi.GetNotionAsync(settings.apiKey, $"pages/{rel.id.ToString()}"); // IDからリレーション先のページを取得
								var result = DynamicJson.Parse(resultJson);

								foreach (dynamic prop in (object[])result.properties) {
									if (prop.Value.type == "title") {
										if (prop.Value.title.IsArray) {
											var titleStr = "";

											foreach (dynamic ti in (object[])prop.Value.title) {
												titleStr += ti.plain_text;
											}

											titleList[index] = titleStr;
										} else {
											titleList[index] = prop.Value.title.plain_text;
										}
									}
								}
							}

							var tasks = ((object[])propVal.relation).Select(GetNotionString).ToArray();

							await UniTask.WhenAll(tasks);
						} catch (Exception ex) {
							Debug.LogError(ex);
						}

						return string.Join('\t', titleList);
					} else {
						var titleList = new List<string>();
						var resultJson = await NotionApi.GetNotionAsync(settings.apiKey, $"pages/{propVal.relation.id.ToString()}"); // IDからリレーション先のページを取得
						var result = DynamicJson.Parse(resultJson);

						foreach (dynamic prop in (object[])result.properties) {
							if (prop.Value.type == "title") {
								if (prop.Value.title.IsArray) {
									var titleStr = "";

									foreach (dynamic ti in (object[])prop.Value.title) {
										titleStr += ti.plain_text;
									}

									titleList.Add(titleStr);
								} else {
									titleList.Add(prop.Value.title.plain_text);
								}
							}
						}
					}

					break;

				case DbPropertyType.created_time:
					return propVal.created_time;

				case DbPropertyType.number:
					if (propVal.number == null) {
						return "0"; // Notionだと数字がNullは有り得る、その場合はデフォルト値の0を返す
					}

					return propVal.number.ToString();

				case DbPropertyType.email:
					return propVal.email;

				case DbPropertyType.select:
					return propVal.select?.name;

				case DbPropertyType.url:
					return propVal.url;

				case DbPropertyType.phone_number:
					return propVal.phone_number;

				case DbPropertyType.last_edited_time:
					return propVal.last_edited_time;

				case DbPropertyType.rich_text:
					if (propVal.rich_text.IsArray) {
						var returnStr = "";

						if (((object[])propVal.rich_text).Length > 0) {
							foreach (var val in propVal.rich_text) {
								returnStr += val.plain_text;
							}
						}

						return returnStr;
					}

					return propVal.rich_text?.plain_text;

				case DbPropertyType.formula:
					return propVal.formula?.@string;

				case DbPropertyType.multi_select:
					var returnVal = new List<string>();

					foreach (var select in propVal.multi_select) {
						returnVal.Add(select?.name);
					}

					return string.Join(',', returnVal);

				case DbPropertyType.created_by:
					return propVal.created_by.name;

				case DbPropertyType.status:
					return propVal.status.name;

				case DbPropertyType.people:
					return propVal.people.name;

				case DbPropertyType.files:
					if (((object[])propVal.files).Length == 0) {
						return null;
					} else {
						return propVal.files[0].file.url;
					}

				case DbPropertyType.date:
					return propVal.date?.start;

				case DbPropertyType.checkbox:
					return propVal.checkbox?.ToString();

				case DbPropertyType.title:
					if (propVal.title.IsArray) {
						var returnStr = "";

						if (((object[])propVal.title).Length > 0) {
							foreach (var val in propVal.title) {
								returnStr += val.plain_text;
							}

							return returnStr;
						} else {
							return null;
						}
					}

					return propVal.title.plain_text;

				case DbPropertyType.unique_id:
					return propVal.unique_id.number.ToString();
			}

			return null;
		}

	}

}