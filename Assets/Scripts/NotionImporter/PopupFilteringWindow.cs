using System;
using UnityEditor;
using UnityEngine;

namespace Kokorowa {

	public class PopupFilteringWindow : EditorWindow {

		private string[] KeyItems { get; set; }

		private int targetKeyIndex { get; set; }

		private Action<string> FilteringProcess { get; set; }

		public static void Open(string[] keyItems, Action<string> process) {
			var window = CreateInstance<PopupFilteringWindow>();

			window.KeyItems = keyItems;
			window.FilteringProcess = process;

			window.Show();
		}

		private void OnGUI() {
			targetKeyIndex = EditorGUILayout.Popup(targetKeyIndex, KeyItems);

			if (GUILayout.Button("実行")) {
				FilteringProcess(KeyItems[targetKeyIndex]);

				this.Close();
			}
		}

	}

}