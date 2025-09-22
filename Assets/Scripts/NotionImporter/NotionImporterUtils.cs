using System;
using System.Reflection;
using UnityEditor;

namespace NotionImporter {
	public static class NotionImporterUtils {
		/// <summary> 引数の要素が含まれる配列のインデックスを返す </summary>
		/// <param name="ary">対象の配列</param>
		/// <param name="val">検索する値</param>
		/// <returns>値が含まれるインデックス</returns>
		public static int IndexOf(this Array ary, object val)
			=> Array.IndexOf(ary, val);

		/// <summary>
		/// フィールドの値をセットする。(プライベートでもパブリックでも)
		/// エディタでのみ使用可。ランタイムスクリプトでは使用しない事！
		/// </summary>
		/// <param name="cls">対象インスタンス</param>
		/// <param name="name">フィールド名</param>
		/// <param name="val">値</param>
		/// <typeparam name="T">対象インスタンスの型</typeparam>
		public static void SetField<T>(this T cls, string name, object val) {
			var field = cls.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			if(field == null) field = typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			field.SetValue(cls, val);
		}

		public static void AddMenuItem(string name, string shortcut, bool isChecked, int priority, Action execute,
			Func<bool> validate) {
			var addMenuItemMethod = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
			addMenuItemMethod?.Invoke(null, new object[] {
				name,
				shortcut,
				isChecked,
				priority,
				execute,
				validate
			});
		}

		/// <summary> 区切り線を追加 </summary>
		/// <param name="name">メニュー名(「Test/」の様に区切り文字で終える)</param>
		/// <param name="priority">優先度</param>
		public static void AddSeparator(string name, int priority) {
			var addSeparatorMethod = typeof(Menu).GetMethod("AddSeparator", BindingFlags.Static | BindingFlags.NonPublic);
			addSeparatorMethod?.Invoke(null, new object[] {
				name,
				priority
			});
		}

		public static bool ExistsMenuItem(string name) {
			var menuItemExistsMethod = typeof(Menu).GetMethod("MenuItemExists", BindingFlags.Static | BindingFlags.NonPublic);
			var result = menuItemExistsMethod.Invoke(null, new[] {
				name
			});

			return (bool)result;
		}

		public static void RemoveMenuItem(string name) {
			var removeMenuItemMethod = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
			removeMenuItemMethod?.Invoke(null, new object[] {
				name
			});
		}

		/// <summary> メニューのリビルド、動的なものも含めて全てリセットされる </summary>
		public static void RebuildAllMenus() {
			//var removeMenuItemMethod = typeof(Menu).GetMethod("RebuildAllMenus", BindingFlags.Static | BindingFlags.NonPublic);
			//removeMenuItemMethod?.Invoke(null, null);
		}

		public static void Update() {
			var internalUpdateAllMenus =
				typeof(EditorUtility).GetMethod("Internal_UpdateAllMenus", BindingFlags.Static | BindingFlags.NonPublic);
			internalUpdateAllMenus?.Invoke(null, null);

			var shortcutIntegrationType = Type.GetType("UnityEditor.ShortcutManagement.ShortcutIntegration, UnityEditor.CoreModule");
			var instanceProp = shortcutIntegrationType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
			var instance = instanceProp?.GetValue(null);
			var rebuildShortcutsMethod =
				instance?.GetType().GetMethod("RebuildShortcuts", BindingFlags.Instance | BindingFlags.NonPublic);
			rebuildShortcutsMethod?.Invoke(instance, null);
		}
	}
}
