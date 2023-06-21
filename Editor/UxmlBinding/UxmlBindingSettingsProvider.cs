using UnityEditor;
using UnityEngine.UIElements;
using InspectorEditor = UnityEditor.Editor;

namespace Endava.Editor.UxmlBinding
{
	internal class UxmlBindingSettingsProvider : SettingsProvider
	{
		private InspectorEditor m_settingsInspector;

		public UxmlBindingSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			m_settingsInspector = InspectorEditor.CreateEditor(UxmlBindingSettings.GetOrCreateSettings());
		}

		public override void OnGUI(string searchContext)
		{
			base.OnGUI(searchContext);

			EditorGUI.BeginChangeCheck();

			if (m_settingsInspector)
			{
				m_settingsInspector.OnInspectorGUI();
			}

			if (EditorGUI.EndChangeCheck())
				m_settingsInspector.serializedObject.ApplyModifiedProperties();
		}

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			var provider = new UxmlBindingSettingsProvider("Project/Uxml Binding", SettingsScope.Project);

			// optional todo adding specific keywords for search bar
			provider.keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(UxmlBindingSettings.BindingSettingsDefaultPath)));

			return provider;
		}
	}
}
