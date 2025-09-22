using NotionImporter;

public class NotionConnector {

	/// <summary> インポート設定 </summary>
	public NotionImporterSettings ImporterSettings { get; set; }

	/// <summary> 接続したかのフラグ </summary>
	public bool IsConnected { get; private set; } = false;

	/// <summary> 初期化フラグ </summary>
	private bool m_isInitialized = false;

	private MainImportWindow m_mainWindow;

	public NotionConnector(MainImportWindow mainWindow) {
		m_mainWindow = mainWindow;
	}

	/// <summary> NotionAPIキー設定の描画 </summary>
	/// <returns>APIキーが変更されたか？</returns>
	public void InitialConnect() {
		if (m_isInitialized) return;
		m_mainWindow.CurrentStatusString = "初期化中";


		m_isInitialized = true;

		if (ImporterSettings == null) {
			ImporterSettings = NotionImporterSettings.LoadSetting();
		}

		ForceConnect();
	}

	public void ForceConnect() {
		m_mainWindow.CurrentStatusString = "接続開始";
		ImporterSettings.RefreshDatabaseInfo();

		if (ImporterSettings.connectionSucceed) {
			m_mainWindow.CurrentStatusString = "接続成功";
			NotionImporterSettings.SaveSetting(ImporterSettings);
			IsConnected = ImporterSettings.connectionSucceed;
		} else {
			m_mainWindow.CurrentStatusString = "接続失敗";

		}
	}

}