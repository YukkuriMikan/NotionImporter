using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NotionImporter {

	public class NotionTree : TreeView {

		private NotionObject[] m_notionObjects;

		public NotionObject this[string id] {
			get {
				return m_notionObjects.FirstOrDefault(obj => obj.id == id);
			}
		}

		public NotionObject this[int hashId] {
			get {
				return m_notionObjects.FirstOrDefault(obj => obj.id.GetHashCode() == hashId);
			}
		}

		public NotionTree(TreeViewState state) : base(state) { }

		public NotionTree(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader) { }

		public void Initialize(NotionObject[] objects) {
			m_notionObjects = objects;

			Reload();
		}

		protected override TreeViewItem BuildRoot() {
			var treeRoot = new TreeViewItem(id: 0, depth: -1, displayName: "Notionオブジェクト");
			var rootObjs = m_notionObjects.Where(obj => obj.parent == null);

			foreach (var obj in rootObjs) {
				var rootItem = CreateTreeViewItem(obj);

				treeRoot.AddChild(rootItem);

				AddChildrenRecursive(obj, rootItem);
			}

			SetupDepthsFromParentsAndChildren(treeRoot);

			return treeRoot;

		}

		private void AddChildrenRecursive(NotionObject parentObj, TreeViewItem parentItem) {
			var children = m_notionObjects.Where(obj => obj.parent?.Id == parentObj.id);

			foreach (var child in children) {
				var childItem = CreateTreeViewItem(child);
				parentItem.AddChild(childItem);

				AddChildrenRecursive(child, childItem);
			}
		}

		private TreeViewItem CreateTreeViewItem(NotionObject obj) => new() { id = obj.id.GetHashCode(), displayName = obj.MainTitle };


		protected override void RowGUI(RowGUIArgs args) {
			var elementWidth = 16f;
			var padding = 2f;
			var containerTex = EditorGUIUtility.Load("d_FolderOpened Icon") as Texture2D;
			var databaseTex = EditorGUIUtility.Load("PreviewPackageInUse") as Texture2D;
			var toggleRect = args.rowRect;
			toggleRect.x += GetContentIndent(args.item); // 描画位置はこのように取得
			toggleRect.width = elementWidth;

			if (this[args.item.id].objectType == NotionObjectType.Container) {
				GUI.DrawTexture(toggleRect, containerTex);

				toggleRect = args.rowRect;
				toggleRect.x += GetContentIndent(args.item) + elementWidth + padding; // 描画位置はこのように取得
				toggleRect.width = 16f;
				args.selected = EditorGUI.ToggleLeft(toggleRect, "", args.selected);

			} else if (this[args.item.id].objectType == NotionObjectType.Database) {
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