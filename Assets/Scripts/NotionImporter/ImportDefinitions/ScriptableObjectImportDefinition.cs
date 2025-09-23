using System;
using NotionImporter.Functions.SubFunction.ScriptableObjects;

namespace NotionImporter {

        /// <summary>ScriptableObject向けのインポート定義を保持します。</summary>
        [Serializable]
        public class ScriptableObjectImportDefinition : ImportDefinitionBase {

                /// <summary>ScriptableObject用の定義タイプを返します。</summary>
                public override string definitionType {
                        get {
                                return "ScriptableObject"; // 定義タイプの固定文字列を返す
                        }
                }

                public string        targetScriptableObject; // 対象となるスクリプタブルオブジェクトの型名

                public string keyProperty; // グループ化に使用するプロパティID

                public bool useKeyFiltering; // フィルタリングを行うかどうか

                public MappingMode   mappingMode; // マッピングモード（配列などの種別）

                public TypeItem      targetFieldType; // 配列モード時のターゲット型

                public string        targetFieldName; // 配列モード時のターゲットフィールド名

                public MappingData[] mappingData; // マッピング定義の一覧

        }

}