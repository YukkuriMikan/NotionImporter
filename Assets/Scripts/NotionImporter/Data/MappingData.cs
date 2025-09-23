using System;

namespace NotionImporter {

        /// <summary>Notionプロパティとフィールドの対応付け情報を保持します。</summary>
        [Serializable]
        public class MappingData {

                public string         targetFieldName; // マッピング先のフィールド名
                public string         targetPropertyName; // 対応するプロパティ名
                public string         targetPropertyId; // 対応するプロパティID
                public DbPropertyType targetPropertyType; // 対応するプロパティの型

        }

}