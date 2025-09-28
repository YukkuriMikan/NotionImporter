namespace NotionImporter {

	/// <summary>Notionデータベースのプロパティ型を表します。</summary>
	public enum DbPropertyType {

		title,            // タイトルフィールド
		unique_id,        // 一意なIDフィールド
		rich_text,        // リッチテキストフィールド
		checkbox,         // チェックボックスフィールド
		date,             // 日付フィールド
		number,           // 数値フィールド
		select,           // 単一選択フィールド
		multi_select,     // 複数選択フィールド
		url,              // URLフィールド
		email,            // メールアドレスフィールド
		phone_number,     // 電話番号フィールド
		formula,          // 計算式フィールド
		people,           // ユーザー選択フィールド
		status,           // ステータスフィールド
		files,            // ファイルフィールド
		last_edited_by,   // 最終更新者フィールド
		last_edited_time, // 最終更新日時フィールド
		created_by,       // 作成者フィールド
		created_time,     // 作成日時フィールド
		rollup,           // ロールアップフィールド
		relation,         // リレーションフィールド

	}

}
