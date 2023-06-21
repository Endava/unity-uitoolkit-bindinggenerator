
namespace Endava.Editor.UxmlBinding
{
	public enum BindingGeneratorType
	{
		/// <summary>
		/// Generates the code for query visual elements within the construction of the object.
		/// </summary>
		OnCreate,
		/// <summary>
		/// Generates the code for query visual elements to the property access.
		/// </summary>
		LazyLoaded,
		/// <summary>
		/// Generates the code for query visual elements all the time (no caching) to save on memory
		/// </summary>
		Proxy
	}
}
