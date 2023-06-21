using UnityEditor;

namespace Endava.Editor.UxmlBinding
{
	public static class UxmlEditorMenuItems
	{
		private const string rootPath = "Window/UI Toolkit/Uxml Binding/";
		[MenuItem(rootPath + "Create ALL Bindings", priority = 1)]
		private static void UiConversionCreateAllWindow()
		{
			UxmlBindingGenerator.BindAll();
		}

		[MenuItem(rootPath + "Sanitize Bindings", priority = 2)]
		private static void UiBindingSanitizeWindow()
		{
			UxmlBindingGenerator.SanitizeBinding();
		}
	}
}
