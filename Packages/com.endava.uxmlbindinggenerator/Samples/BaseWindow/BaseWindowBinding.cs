//<auto-generated>
// This code was generated by a tool.
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//</auto-generated>

using UnityEngine.UIElements;

namespace UxmlBindings
{
	public partial struct BaseWindowBinding : IBaseBinding
	{
		private VisualElement _____root_____; // avoid name collisions!
		public VisualElement GetRootElement() => _____root_____;

		public VisualElement window => _____root_____.Q<VisualElement>("window");
		public VisualElement frame => _____root_____.Q<VisualElement>("frame");
		public VisualElement contentContainer => _____root_____.Q<VisualElement>("contentContainer");
		public VisualElement buttonAnchor => _____root_____.Q<VisualElement>("buttonAnchor");
		public Button closeBtn => _____root_____.Q<Button>("closeBtn");

		public BaseWindowBinding(VisualElement parent)
		{
			_____root_____ = parent;
			
		}
	}
}
