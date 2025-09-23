using NotionImporter.Functions.SubFunction;

namespace NotionImporter.Functions {

        /// <summary>Notionインポータの機能インターフェイス</summary>
        public interface IMainFunction {

                /// <summary>サブ機能の一覧</summary>
                public ISubFunction[] SubFunctions { get; }

                /// <summary>選択中のサブ機能インデックス</summary>
                public int SelectedSubFunctionIndex { get; set; }

                /// <summary>機能名</summary>
                public string FunctionName { get; }

                /// <summary>Notionツリーの参照</summary>
                public NotionTree NotionTree { get; }

                /// <summary>Notionインポータの機能を描画する</summary>
                public void DrawFunction(MainImportWindow parent, NotionImporterSettings settings);

        }

}