using System; // Exception処理用

namespace NotionImporter {
	/// <summary> Notionとの接続処理を管理するクラス </summary>
	public class NotionConnector {

		/// <summary> インポート設定 </summary>
		public NotionImporterSettings ImporterSettings { get; set; }

		/// <summary> 接続したかのフラグ </summary>
		public bool IsConnected { get; private set; } = false;

		private bool m_isInitialized = false; // 初期化済みかどうかのフラグ

		private readonly MainImportWindow m_mainWindow; // メインウィンドウの参照

		/// <summary> メインウィンドウを受け取りコネクタを初期化する </summary>
		public NotionConnector(MainImportWindow mainWindow) {
			m_mainWindow = mainWindow; // 参照を保持して後続処理で使用する
		}

		/// <summary> 接続初期化処理を実施する </summary>
		public void InitialConnect() {
			if(m_isInitialized) return; // 二重初期化を防止する

			try {
				m_mainWindow.CurrentStatusString = "初期化中"; // ステータス更新を行い初期化開始を通知する

				m_isInitialized = true; // 初期化フラグを立てる

				if(!EnsureSettingsLoaded()) { // 設定読み込みに失敗した場合は再試行可能にして終了
					m_isInitialized = false;
					return;
				}

				ForceConnect(); // 設定を使って接続処理を開始する
			} catch (Exception ex) {
				m_isInitialized = false; // エラー時は再初期化できるようにフラグを戻す
				m_mainWindow.ReportError("初期化処理", ex);
			}
		}

		/// <summary> Notionへの接続処理を実行する </summary>
		public void ForceConnect() {
			try {
				if(!EnsureSettingsLoaded()) { // 設定が準備できなければ接続処理を行わない
					IsConnected = false;
					return;
				}

				m_mainWindow.CurrentStatusString = "接続開始"; // ステータスを接続開始に変更する

				ImporterSettings.RefreshDatabaseInfo(); // データベース情報の更新を行う

				if(ImporterSettings.connectionSucceed) { // 成否に応じてステータスと保存処理を分岐する
					m_mainWindow.CurrentStatusString = "接続成功";
					NotionImporterSettings.SaveSetting(ImporterSettings);
					IsConnected = true;
				} else {
					IsConnected = false;
					m_mainWindow.CurrentStatusString = "接続失敗";
				}
			} catch (Exception ex) {
				IsConnected = false; // エラー時は接続失敗扱い
				m_mainWindow.ReportError("Notionへの接続処理", ex);
			}
		}

		/// <summary> 設定が読み込まれているか確認し必要ならロードする </summary>
		private bool EnsureSettingsLoaded() {
			if(ImporterSettings != null) { // 既に読み込み済みなら成功扱い
				return true;
			}

			try {
				ImporterSettings = NotionImporterSettings.LoadSetting();
			} catch (Exception ex) {
				m_mainWindow.ReportError("設定ファイルの読み込み", ex); // 読み込み失敗時はエラー表示
				return false;
			}

			if(ImporterSettings == null) { // 設定ファイルが存在しない場合は新規インスタンスを作成
				ImporterSettings = new NotionImporterSettings();
				m_mainWindow.CurrentStatusString = "設定ファイルが見つかりませんでした。新規設定を使用します";
			}

			return ImporterSettings != null;
		}

	}
}
