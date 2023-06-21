using System.IO;
using UnityEngine;

namespace Endava.Editor.UxmlBinding
{
	public static class FileUtils
	{
		public const char WinSeparator = '\\';
		public const char UnixSeparator = '/';

		/// <summary>
		/// Normalize a path by using os related separators.
		/// </summary>
		/// <param name="path">The path you want to normalize.</param>
		/// <returns>Returns a normalized path string, depending on the actual OS unity is running on.</returns>
		public static string Normalize(string path)
		{
			if (string.IsNullOrEmpty(path))
				return path;

			path = Path.DirectorySeparatorChar switch
			{
				WinSeparator  => path.Replace(UnixSeparator, WinSeparator),
				UnixSeparator => path.Replace(WinSeparator, UnixSeparator),
				_             => path
			};

			return path.Replace(string.Concat(WinSeparator, WinSeparator), WinSeparator.ToString());
		}

		/// <summary>
		/// Normalize path string by replace all Windows separators with the unix one.
		/// </summary>
		/// <param name="path">The path you want to normalize.</param>
		/// <returns>Returns a unix normalized path string.</returns>
		public static string NormalizeWindowsToUnix(string path)
		{
			return string.IsNullOrEmpty(path) ? path : path.Replace(WinSeparator, UnixSeparator);
		}

		/// <summary>
		/// Normalize path string by replace all unix separators with the windows one.
		/// </summary>
		/// <param name="path">The path you want to normalize.</param>
		/// <returns>Returns a windows normalized path string.</returns>
		public static string NormalizeUnixToWindows(string path)
		{
			return string.IsNullOrEmpty(path) ? path : path.Replace(UnixSeparator, WinSeparator);
		}

		/// <summary>
		/// Trims possible separators from the path
		/// </summary>
		/// <param name="path">The path you want to normalize.</param>
		/// <returns>Returns a windows normalized path string.</returns>
		public static string Trim(string path)
		{
			return string.IsNullOrEmpty(path) ? path : path.Trim(UnixSeparator, WinSeparator);
		}

		/// <summary>
		/// Converts a relative path of a unity project asset into an absolute path.
		/// Like: "Assets\Folder1\file.txt" to "C:\MyUnityProject\Assets\Folder1\file.txt"
		/// </summary>
		/// <param name="relativePath">The relative path of your file/directory.</param>
		/// <returns>Returns an absolute normalized path of the given relativePath, or null if path might already be rooted.</returns>
		public static string ConvertRelativeProjectPathToAbsolutePath(string relativePath)
		{
			if (string.IsNullOrEmpty(relativePath))
				return relativePath;

			if (Path.IsPathRooted(relativePath))
				return null;

			string result = Path.Combine(AbsoluteProjectPath(), relativePath);
			result = Normalize(result);
			return result;
		}

		/// <summary>
		/// Returns the project path of the current unity project. (The path where the Assets, Temps, Library, etc. is listed)
		/// </summary>
		/// <returns>A unity applications current "project path".</returns>
		public static string AbsoluteProjectPath()
		{
			string result = Path.GetFullPath(Application.dataPath.Replace("/Assets", ""));
			result = Normalize(result);
			return result;
		}
	}
}