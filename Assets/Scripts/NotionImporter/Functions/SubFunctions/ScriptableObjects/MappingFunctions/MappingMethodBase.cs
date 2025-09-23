using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NotionImporter.Functions.SubFunction.ScriptableObjects {

        /// <summary>ScriptableObjectのマッピング処理の基本クラスです。</summary>
        public abstract class MappingMethodBase {

                protected NotionImporterSettings m_settings; // 利用中のインポート設定

                /// <summary>メソッドが必要とするターゲットアイテム</summary>
                public virtual MappingItem MethodTarget { get; set; }

                public virtual TypeItem MethodTargetType {
			get {
				return new TypeItem {
					typeName = MethodTarget?.fieldInfo.FieldType.Name,
					typeFullName = MethodTarget?.fieldInfo.FieldType.FullName,
					assemblyName = MethodTarget?.fieldInfo.FieldType.Assembly.GetName().Name,
				};
			}
		}

		public virtual TypeItem MethodTargetArrayType {
			get {
				return MethodTargetType;
			}
		}

                /// <summary>マッピング対象となるフィールド情報</summary>
                public MappingItem[] MethodMappingItems { get; set; }

		public abstract void DrawPaneHeader();

		public abstract void DrawTargetType();

		public virtual void DrawKeyRow() { }

		public abstract void DrawMappingRow(MappingFunction func, MappingItem itm);

		public virtual MappingData[] GetMappingData() =>
			MethodMappingItems.Where(itm => itm.doMaching)
				.Select(itm => new MappingData {
					targetFieldName = itm.fieldName,
					targetPropertyId = itm.targetProperties[itm.propertyIndex].id,
					targetPropertyName = itm.targetProperties[itm.propertyIndex].name,
					targetPropertyType = itm.targetProperties[itm.propertyIndex].type,
				}).ToArray();

		#region マッピング対象の取得関連
		/// <summary> マッピングアイテムクラスを取得 </summary>
		/// <param name="settings">Notion接続設定</param>
		/// <param name="targetTypeItem">マッピング対象の型</param>
                public void Initialize(NotionImporterSettings settings, TypeItem targetTypeItem) {
                        m_settings = settings; // Notion設定と対象型を基に候補を生成

			MethodMappingItems = targetTypeItem // リフレクションで対象スクリプタブルオブジェクトが持つフィールドを列挙する
				.targetType
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(fld => !fld.Name.StartsWith("<")) // 自動実装プロパティの自動生成フィールドを弾く
				.Select(fld =>
					new MappingItem {
						fieldName = fld.Name,
						fieldInfo = fld,
						fieldType = fld.FieldType,
						isArray = fld.FieldType.IsArray,
						isList = fld.FieldType.Name == "List`1" && fld.FieldType.IsGenericType,
						innerFieldInfo = fld.FieldType.IsPrimitive || fld.FieldType.IsArray || fld.FieldType.Name == "List`1"
							? null
							: fld.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
						targetProperties = GetMappingTargetProperty(fld.FieldType),
					})
				.ToArray();
		}

		/// <summary> マッピングのターゲットとなるフィールドの取得 </summary>
		/// <param name="type">対象の型オブジェクト</param>
		/// <returns>対象のマッピングフィールド</returns>
                private NotionProperty[] GetMappingTargetProperty(Type type) {
                        switch (type) { // フィールド型に対応したNotionプロパティを抽出
				case Type t1 when t1 == typeof(string): //文字列
					return m_settings.CurrentProperty;

				case Type t when //数字まとめ
					t == typeof(byte) ||
					t == typeof(Char) ||
					t == typeof(short) ||
					t == typeof(ushort) ||
					t == typeof(int) ||
					t == typeof(uint) ||
					t == typeof(long) ||
					t == typeof(ulong) ||
					t == typeof(float) ||
					t == typeof(double) ||
					t == typeof(decimal):

					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.number)
						.ToArray();

				case Type t when t == typeof(bool):
					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.checkbox)
						.ToArray();

				case Type t when t == typeof(DateTime):
					return m_settings.CurrentProperty
						.Where(prop =>
							prop.type == DbPropertyType.date ||
							prop.type == DbPropertyType.created_time ||
							prop.type == DbPropertyType.last_edited_time)
						.ToArray();

				case Type t when t == typeof(Uri):
					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.url)
						.ToArray();

				case Type t when t.BaseType == typeof(Enum): //Enum
					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.select ||
										prop.type == DbPropertyType.multi_select ||
										prop.type == DbPropertyType.relation)
						.ToArray();

				case Type t when t == typeof(Texture) || t == typeof(Texture2D):
					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.files)
						.ToArray();

				case Type t when t == typeof(Sprite):
					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.files)
						.ToArray();

				case Type t when t == typeof(string[]): // stringの配列のみ、対応可能
					return m_settings.CurrentProperty
						.Where(prop => prop.type == DbPropertyType.relation || prop.type == DbPropertyType.multi_select)
						.ToArray();

				default:
					if (type.IsArray || (type.Name == "List`1" && type.IsGenericType) ||
						(type.Name == "Dictionary`2" && type.IsGenericType)) {
						return Array.Empty<NotionProperty>();
					}

					Debug.LogWarning($"対応していない型:{type.FullName}がクラスに含まれています");

					return m_settings.CurrentProperty;
			}
		}
		#endregion

	}

}