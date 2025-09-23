using System;
using UnityEditor;
using UnityEngine;

namespace Kokorowa {

	/// <summary>フィルタキーを選択するポップアップウィンドウです。</summary>
	public class PopupFilteringWindow : EditorWindow {

		/// <summary>選択可能なキー一覧</summary>
		private string[] KeyItems { get; set; }

		/// <summary>選択中のキーインデックス</summary>
		private int targetKeyIndex { get; set; }

		/// <summary>選択結果を処理するコールバック</summary>
		private Action<string> FilteringProcess { get; set; }

		/// <summary>フィルタリングウィンドウを表示します。</summary>
		public static void Open(string[] keyItems, Action<string> process) {
			var window = CreateInstance<PopupFilteringWindow>(); // パラメータを受け取ってウィンドウを生成

			window.KeyItems = keyItems;
			window.FilteringProcess = process;

			window.Show();
		}

		/// <summary>ウィンドウのGUIを描画します。</summary>
		private void OnGUI() {
			targetKeyIndex = EditorGUILayout.Popup(targetKeyIndex, KeyItems); // キー選択用のポップアップを表示

			if(GUILayout.Button("実行")) {
				FilteringProcess(KeyItems[targetKeyIndex]); // 選択されたキーをコールバックに渡す

				this.Close();
			}
		}

	}

}
