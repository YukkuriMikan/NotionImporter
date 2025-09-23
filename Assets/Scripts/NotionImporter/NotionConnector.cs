using NotionImporter;

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
                if (m_isInitialized) return; // 二重初期化を防止する

                m_mainWindow.CurrentStatusString = "初期化中"; // ステータス更新を行い初期化開始を通知する

                m_isInitialized = true; // 初期化フラグを立てる

                EnsureSettingsLoaded(); // 設定が未ロードであれば読み込む

                ForceConnect(); // 設定を使って接続処理を開始する
        }

        /// <summary> Notionへの接続処理を実行する </summary>
        public void ForceConnect() {
                EnsureSettingsLoaded(); // 設定が準備されているか確認する

                m_mainWindow.CurrentStatusString = "接続開始"; // ステータスを接続開始に変更する

                ImporterSettings.RefreshDatabaseInfo(); // データベース情報の更新を行う

                if (ImporterSettings.connectionSucceed) { // 成否に応じてステータスと保存処理を分岐する
                        m_mainWindow.CurrentStatusString = "接続成功";
                        NotionImporterSettings.SaveSetting(ImporterSettings);
                        IsConnected = ImporterSettings.connectionSucceed;
                } else {
                        m_mainWindow.CurrentStatusString = "接続失敗";
                }
        }

        /// <summary> 設定が読み込まれているか確認し必要ならロードする </summary>
        private void EnsureSettingsLoaded() {
                if (ImporterSettings == null) { // 設定が未設定の場合は保存データから読み込む
                        ImporterSettings = NotionImporterSettings.LoadSetting();
                }
        }

}