using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NotionImporter {

	/// <summary>NotionオブジェクトをTreeViewで表示します。</summary>
	public class NotionTree : TreeView<int> {

		private NotionObject[] m_notionObjects; // 表示対象のNotionオブジェクト一覧

		/// <summary>NotionオブジェクトをIDで取得します。</summary>
		public NotionObject this[string id] {
			get {
				return m_notionObjects.FirstOrDefault(obj => obj.id == id); // IDが一致するオブジェクトを返す
			}
		}

		/// <summary>NotionオブジェクトをハッシュIDで取得します。</summary>
		public NotionObject this[int hashId] {
			get {
				return m_notionObjects.FirstOrDefault(obj => obj.id.GetHashCode() == hashId); // ハッシュ値で一致するオブジェクトを返す
			}
		}

		/// <summary>シンプルなヘッダー構成で初期化します。</summary>
		public NotionTree(TreeViewState<int> state) : base(state) { }

		/// <summary>複数列ヘッダー付きで初期化します。</summary>
		public NotionTree(TreeViewState<int> state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader) { }

		/// <summary>TreeViewに表示するデータを設定します。</summary>
		public void Initialize(NotionObject[] objects) {
			m_notionObjects = objects; // 外部から渡されたオブジェクトを保持

			Reload();
		}

		/// <summary>ルートノードを生成します。</summary>
		protected override TreeViewItem<int> BuildRoot() {
			var treeRoot = new TreeViewItem<int>(id: 0, depth: -1, displayName: "Notionオブジェクト");
			var rootObjs = m_notionObjects.Where(obj => obj.parent == null);

			foreach (var obj in rootObjs) {
				var rootItem = CreateTreeViewItem(obj);

				treeRoot.AddChild(rootItem);

				AddChildrenRecursive(obj, rootItem);
			}

			SetupDepthsFromParentsAndChildren(treeRoot);

			return treeRoot;

		}

		/// <summary>子ノードを再帰的に追加します。</summary>
		private void AddChildrenRecursive(NotionObject parentObj, TreeViewItem<int> parentItem) {
			var children = m_notionObjects.Where(obj => obj.parent?.Id == parentObj.id);

			foreach (var child in children) {
				var childItem = CreateTreeViewItem(child);
				parentItem.AddChild(childItem);

				AddChildrenRecursive(child, childItem);
			}
		}

		/// <summary>NotionオブジェクトをTreeViewItemに変換します。</summary>
		private TreeViewItem<int> CreateTreeViewItem(NotionObject obj) => new(obj.id.GetHashCode()) {
			displayName = obj.MainTitle
		}; // オブジェクトIDをハッシュ化してツリー項目を生成


		/// <summary>各行の描画をカスタマイズします。</summary>
		protected override void RowGUI(RowGUIArgs args) {
			var elementWidth = 16f;
			var padding = 2f;
			var containerTex = EditorGUIUtility.Load("d_FolderOpened Icon") as Texture2D;
			var databaseTex = EditorGUIUtility.Load("PreviewPackageInUse") as Texture2D;
			var toggleRect = args.rowRect;
			toggleRect.x += GetContentIndent(args.item); // 描画位置はこのように取得
			toggleRect.width = elementWidth;

			if(this[args.item.id].objectType == NotionObjectType.Container) {
				GUI.DrawTexture(toggleRect, containerTex);

				toggleRect = args.rowRect;
				toggleRect.x += GetContentIndent(args.item) + elementWidth + padding; // 描画位置はこのように取得
				toggleRect.width = 16f;
				args.selected = EditorGUI.ToggleLeft(toggleRect, "", args.selected);

			} else if(this[args.item.id].objectType == NotionObjectType.Database) {
				GUI.DrawTexture(toggleRect, databaseTex);

				toggleRect = args.rowRect;
				toggleRect.x += GetContentIndent(args.item) + elementWidth + padding; // 描画位置はこのように取得
				toggleRect.width = 16f;
				args.selected = EditorGUI.ToggleLeft(toggleRect, "", args.selected);
			}

			extraSpaceBeforeIconAndLabel = elementWidth * 2 + padding; // アイコンを表示した分ラベルをの開始位置をずらす

			base.RowGUI(args);
		}
	}
}
