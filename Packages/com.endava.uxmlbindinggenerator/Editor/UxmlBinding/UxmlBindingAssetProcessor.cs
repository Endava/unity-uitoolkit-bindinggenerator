using System.IO;
using UnityEditor;
using UnityEngine;

namespace Endava.Editor.UxmlBinding
{
	/// <summary>
	/// This post process asset importer will handle updates within uxml files and write or update existing binding cs files.
	/// </summary>
	public class UxmlBindingAssetProcessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
		{
			// dont allow any post process on batch build mode!
			if (Application.isBatchMode) return;

			// take deletion into account
			foreach (string path in deleted) // TODO FIX THIS BEFORE RE ADDING
				DeleteRemovedBinding(path);

			foreach (string path in imported)
				GenerateNewBinding(path);

			//todo: later delete and create a new one
			//for (int i = 0; i < movedAssets.Length; i++)
			//    Log.Info("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);

			AssetDatabase.Refresh();
		}

		private static void GenerateNewBinding(string path)
		{
			if (Path.GetExtension(path) != UxmlBindingGenerator.fileType)
				return;

			var settings = UxmlBindingSettings.GetOrCreateSettings();
			if (!settings.generateBindingAutomaticallyOnImport)
				return;

			UxmlBindingGenerator.GenerateUxmlBinding(path);
		}

		private static void DeleteRemovedBinding(string path)
		{
			if (Path.GetExtension(path) != UxmlBindingGenerator.fileType)
				return;

			var settings = UxmlBindingSettings.GetOrCreateSettings();
			if (!settings.generateBindingAutomaticallyOnImport)
				return;

			string existingBindingPath = UxmlBindingGenerator.GetBindingClassOfUxml(path);
			string absFile = $"{FileUtils.Normalize(Path.Combine(Application.dataPath, existingBindingPath))}.cs";

			if (!File.Exists(absFile)) return;

			if(settings.logVerbose)
				Debug.Log($"Delete Binding file \"{absFile}\", since uxml has been deleted!");

			File.Delete(absFile);

			string metaFile = $"{absFile}.meta";
			if (File.Exists(metaFile))
			{
				File.Delete(metaFile);
			}
		}
	}
}
