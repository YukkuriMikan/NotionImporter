using System;

namespace NotionImporter {

        /// <summary>I2 Localization向けのインポート定義を保持します。</summary>
        [Serializable]
        public class I2LocalizeImportDefinition : ImportDefinitionBase {

                /// <summary>I2 Localization用の定義タイプを返します。</summary>
                public override string definitionType {
                        get {
                                return "I2Localization"; // 定義タイプの固定文字列を返す
                        }
                }

                public string targetSourceGuid; // I2のターゲットソースGUID

                public string[] languages; // 対応する言語一覧

                public string[] languageIds; // 言語ごとの識別子一覧

        }

}