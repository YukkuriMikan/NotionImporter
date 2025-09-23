using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NotionImporter.Functions.SubFunction.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction {

        /// <summary>ScriptableObject定義を生成するサブ機能です。</summary>
        public class CreateScriptableObjectDefinition : ISubFunction {

                public IMainFunction ParentFunction { get; set; }

                private NotionImporterSettings m_settings; // インポート設定

                /// <summary>機能名</summary>
                public string FunctionName {
                        get {
                                return "ScriptableObject";
                        }
                }

                /// <summary>出力するファイルの型</summary>
                public Type ExportFileType {
                        get {
                                return typeof(ScriptableObjectImportDefinition);
                        }
                }

                private TypePaneFunction m_typePaneFunction = new(); // 型選択ペイン
                private MappingFunction m_mappingFunction = new(); // マッピング設定管理

                /// <summary>画面の描画</summary>
                /// <param name="settings">インポータの設定</param>
                public void DrawFunction(NotionImporterSettings settings) {
                        m_settings = settings; // 現在の設定を保持しつつUIを描画

			m_typePaneFunction.DrawTypePane(m_settings); // 型一覧のペインを描画
			m_mappingFunction.DrawMappingPane(m_settings, m_typePaneFunction.SelectedMappingTargetTypes);
		}

                /// <summary>インポート定義ファイルを生成する</summary>
                public void CreateFile() {
                        if (string.IsNullOrWhiteSpace(m_settings.OutputPath)) { // 出力条件をチェック
                                EditorUtility.DisplayDialog("エラー", "エクスポート先のフォルダを指定して下さい", "OK");

				return;
			}

			if (string.IsNullOrWhiteSpace(m_settings.DefinitionName)) {
				EditorUtility.DisplayDialog("エラー", "ファイル名を指定して下さい", "OK");

				return;
			}

			if (!Directory.Exists(NotionImporterParameters.DefinitionFilePath + $"\\{nameof(ScriptableObjectImportDefinition)}")) {
				Directory.CreateDirectory(NotionImporterParameters.DefinitionFilePath +
										$"\\{nameof(ScriptableObjectImportDefinition)}");
			}

                        var filePath = NotionImporterParameters.DefinitionFilePath + // 型名のフォルダに定義ファイルを保存
                                $"\\{nameof(ScriptableObjectImportDefinition)}" +
                                $"\\{m_settings.DefinitionName}.json";

                        var soSetting = new ScriptableObjectImportDefinition {
                                outputPath = m_settings.OutputPath,
                                definitionName = m_settings.DefinitionName,
				targetDb = m_settings.CurrentObject,
				targetScriptableObject = m_typePaneFunction.SelectedMappingTargetTypes.typeString,
				mappingMode = m_mappingFunction.MappingMode,
				keyProperty = m_settings.KeyId,
				useKeyFiltering = m_settings.UseKeyFiltering,
				targetFieldType = m_mappingFunction.CurrentMappingMethod.MethodTargetArrayType,
				targetFieldName = m_mappingFunction.CurrentMappingMethod.MethodTarget?.fieldName,
				mappingData = m_mappingFunction.CurrentMappingMethod.GetMappingData(),
			};

			var jsonText = JsonUtility.ToJson(soSetting);

			File.WriteAllText(filePath, jsonText);
			AssetDatabase.Refresh();

			ImportMenu.RefreshImportMenu().Forget();
			Debug.Log("インポート定義を作成しました");
		}

                public void ReadFile(NotionImporterSettings settings, string json) {
                        m_settings = settings; // 設定とJSONを受け取り内部状態を復元

                        var definition = JsonUtility.FromJson<ScriptableObjectImportDefinition>(json);

                        if (definition == null) {
                                EditorUtility.DisplayDialog("エラー", "定義ファイルの解析に失敗しました", "OK");

                                return;
                        }

                        var db = m_settings.objects.FirstOrDefault(obj => obj.id == definition.targetDb.id);

                        if (db == null) {
                                EditorUtility.DisplayDialog("エラー", "定義ファイルに指定されているデータベースが存在しませんでした", "OK");

                                return;
                        }

                        m_settings.CurrentObjectId = db.id.GetHashCode(); // データベース選択と基本設定の復元
                        ParentFunction.NotionTree.SetSelection(new List<int> { db.id.GetHashCode() });

                        m_settings.DefinitionName = definition.definitionName;
                        m_settings.OutputPath = definition.outputPath;
                        m_settings.KeyId = definition.keyProperty;
                        m_settings.UseKeyFiltering = definition.useKeyFiltering;

                        m_typePaneFunction.EnsureTypeList(m_settings); // 型リストを確実に初期化

                        var typeItems = m_typePaneFunction.MappingTargetTypes ?? Array.Empty<TypeItem>();
                        var typeIndex = Array.FindIndex(typeItems, itm => itm.typeString == definition.targetScriptableObject);

                        if (typeIndex < 0) {
                                EditorUtility.DisplayDialog("エラー", "定義ファイルの処理対象の型が見つかりませんでした", "OK");

                                return;
                        }

                        var targetTypeItem = typeItems[typeIndex];
                        m_typePaneFunction.SelectedTypeIndex = typeIndex;

                        m_mappingFunction.m_targetType = targetTypeItem; // 内部状態を復元してUIの再初期化を抑止
                        m_mappingFunction.m_currentObject = m_settings.CurrentObject;
                        m_mappingFunction.MappingMode = definition.mappingMode;

                        if (definition.mappingMode == MappingMode.Array || definition.mappingMode == MappingMode.List) {
                                if (!ApplyCollectionTarget(definition, targetTypeItem)) {
                                        return;
                                }
                        } else {
                                m_mappingFunction.CurrentMappingMethod.Initialize(m_settings, targetTypeItem); // 通常マッピングは対象型をそのまま初期化
                        }

                        ApplyMappingData(definition.mappingData);
                }

                /// <summary>配列/リストマッピング設定を復元する</summary>
                private bool ApplyCollectionTarget(ScriptableObjectImportDefinition definition, TypeItem rootTypeItem) {
                        var scriptableType = rootTypeItem.targetType; // 定義ファイルから配列ターゲットの情報を復元

                        if (scriptableType == null) {
                                EditorUtility.DisplayDialog("エラー", "対象のスクリプタブルオブジェクト型を取得出来ませんでした", "OK");

                                return false;
                        }

                        if (string.IsNullOrEmpty(definition.targetFieldName)) {
                                EditorUtility.DisplayDialog("エラー", "配列/リストの対象フィールド名が取得出来ませんでした", "OK");

                                return false;
                        }

                        var fieldInfo = FindFieldRecursive(scriptableType, definition.targetFieldName);

                        if (fieldInfo == null) {
                                EditorUtility.DisplayDialog("エラー", "定義ファイルに記載されたフィールドが見つかりませんでした", "OK");

                                return false;
                        }

                        var methodTarget = new MappingItem {
                                doMaching = true,
                                fieldName = fieldInfo.Name,
                                fieldInfo = fieldInfo,
                                fieldType = fieldInfo.FieldType,
                                isArray = definition.mappingMode == MappingMode.Array,
                                isList = definition.mappingMode == MappingMode.List,
                        };

                        m_mappingFunction.CurrentMappingMethod.MethodTarget = methodTarget;

                        var elementType = GetCollectionElementType(fieldInfo.FieldType);

                        if (elementType == null) {
                                EditorUtility.DisplayDialog("エラー", "コレクションの要素型を特定出来ませんでした", "OK");

                                return false;
                        }

                        var elementTypeItem = CreateTypeItem(elementType);

                        m_mappingFunction.CurrentMappingMethod.Initialize(m_settings, elementTypeItem);

                        return true;
                }

                /// <summary>マッピング設定を復元する</summary>
                private void ApplyMappingData(MappingData[] mappingDataArray) {
                        var mappingItems = m_mappingFunction.CurrentMappingMethod.MethodMappingItems; // マッピングデータをフィールド名で参照できるよう辞書化

                        if (mappingItems == null) {
                                return;
                        }

                        var lookup = (mappingDataArray ?? Array.Empty<MappingData>())
                                .ToDictionary(data => data.targetFieldName, data => data);

                        foreach (var item in mappingItems) {
                                if (!lookup.TryGetValue(item.fieldName, out var data)) {
                                        item.doMaching = false;
                                        continue;
                                }

                                var propertyIndex = Array.FindIndex(item.targetProperties, prop => prop.id == data.targetPropertyId);

                                if (propertyIndex < 0) {
                                        item.doMaching = false;
                                        Debug.LogWarning($"NotionImporter: {item.fieldName} に対応するNotionプロパティが見つかりませんでした");

                                        continue;
                                }

                                item.doMaching = true;
                                item.propertyIndex = propertyIndex;
                        }
                }

                /// <summary>指定したフィールドを継承階層から探索する</summary>
                private static FieldInfo FindFieldRecursive(Type type, string fieldName) {
                        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                        while (type != null) {
                                var field = type.GetField(fieldName, Flags);

                                if (field != null) {
                                        return field;
                                }

                                type = type.BaseType;
                        }

                        return null;
                }

                /// <summary>配列/リストの要素型を取得する</summary>
                private static Type GetCollectionElementType(Type collectionType) {
                        if (collectionType == null) {
                                return null;
                        }

                        if (collectionType.IsArray) {
                                return collectionType.GetElementType();
                        }

                        if (collectionType.IsGenericType && collectionType.GetGenericArguments().Length == 1) {
                                return collectionType.GetGenericArguments()[0];
                        }

                        return null;
                }

                /// <summary>指定型からTypeItemを生成</summary>
                private static TypeItem CreateTypeItem(Type type) {
                        if (type == null) {
                                return null;
                        }

                        return new TypeItem {
                                typeName = type.Name,
                                typeFullName = type.FullName,
                                assemblyName = type.Assembly.GetName().Name,
                        };
                }

	}

}