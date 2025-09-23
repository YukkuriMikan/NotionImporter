using System;

namespace NotionImporter {

        /// <summary>NaniScript向けのインポート定義を保持します。</summary>
        [Serializable]
        public class NaniScriptImportDefinition : ImportDefinitionBase {

                /// <summary>NaniScript用の定義タイプを返します。</summary>
                public override string definitionType {
                        get {
                                return "NaniScript"; // 定義タイプの固定文字列を返す
                        }
                }

                public string[] mappingProperties; // マッピング対象プロパティ名の一覧

        }

}