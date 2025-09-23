using System;
using System.Reflection;
using UnityEditor;

namespace NotionImporter {
	/// <summary>エディタのメニュー操作を補助するユーティリティ群です。</summary>
	public static class NotionImporterUtils {
		/// <summary> 引数の要素が含まれる配列のインデックスを返す </summary>
		/// <param name="ary">対象の配列</param>
		/// <param name="val">検索する値</param>
		/// <returns>値が含まれるインデックス</returns>
		public static int IndexOf(this Array ary, object val) // Array.IndexOf を利用して位置を調べる
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
			var field = cls.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance); // 反射で指定されたフィールドを取得

			if(field == null) field = typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

			field.SetValue(cls, val); // フィールドに値を設定
		}

		/// <summary>Unityのメニュー項目を動的に追加します。</summary>
		/// <param name="name">メニュー名</param>
		/// <param name="shortcut">ショートカット</param>
		/// <param name="isChecked">チェック状態</param>
		/// <param name="priority">表示優先度</param>
		/// <param name="execute">実行アクション</param>
		/// <param name="validate">検証用デリゲート</param>
		public static void AddMenuItem(string name, string shortcut, bool isChecked, int priority, Action execute,
			Func<bool> validate) {
			var addMenuItemMethod = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic); // 内部APIを呼び出してメニューを登録
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
			var addSeparatorMethod = typeof(Menu).GetMethod("AddSeparator", BindingFlags.Static | BindingFlags.NonPublic); // メニューの区切り線を挿入
			addSeparatorMethod?.Invoke(null, new object[] {
				name,
				priority
			});
		}

		/// <summary>指定したメニュー項目が存在するか確認します。</summary>
		/// <param name="name">対象メニュー名</param>
		/// <returns>存在する場合は true</returns>
		public static bool ExistsMenuItem(string name) {
			var menuItemExistsMethod = typeof(Menu).GetMethod("MenuItemExists", BindingFlags.Static | BindingFlags.NonPublic); // 内部APIでメニューの有無を確認
			var result = menuItemExistsMethod.Invoke(null, new[] {
				name
			});

			return (bool)result;
		}

		/// <summary>指定したメニュー項目を削除します。</summary>
		/// <param name="name">削除するメニュー名</param>
		public static void RemoveMenuItem(string name) {
			var removeMenuItemMethod = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic); // 内部APIを呼び出してメニューを削除
			removeMenuItemMethod?.Invoke(null, new object[] {
				name
			});
		}

		/// <summary> メニューのリビルド、動的なものも含めて全てリセットされる </summary>
		public static void RebuildAllMenus() {
			/* var removeMenuItemMethod = typeof(Menu).GetMethod("RebuildAllMenus", BindingFlags.Static | BindingFlags.NonPublic);
			 * removeMenuItemMethod?.Invoke(null, null);
			 */
		}

		/// <summary>エディタのメニュー状態を最新化します。</summary>
		public static void Update() {
			var internalUpdateAllMenus = typeof(EditorUtility).GetMethod("Internal_UpdateAllMenus", BindingFlags.Static | BindingFlags.NonPublic); // メニューを再構築
			internalUpdateAllMenus?.Invoke(null, null);

			var shortcutIntegrationType = Type.GetType("UnityEditor.ShortcutManagement.ShortcutIntegration, UnityEditor.CoreModule"); // ショートカット設定も再構築
			var instanceProp = shortcutIntegrationType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
			var instance = instanceProp?.GetValue(null);
			var rebuildShortcutsMethod =
				instance?.GetType().GetMethod("RebuildShortcuts", BindingFlags.Instance | BindingFlags.NonPublic);
			rebuildShortcutsMethod?.Invoke(instance, null);
		}
	}
}
