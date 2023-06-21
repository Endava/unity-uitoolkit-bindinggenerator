using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Endava.Editor.UxmlBinding
{
	internal class UxmlBindingSettings : ScriptableObject
	{
		public static readonly string BindingSettingsDefaultPath = @"Assets\Editor\UxmlBindingSettings.asset";
		public static readonly string PackageContentPath = @"Packages\com.endava.uxmlbindinggenerator";

		[SerializeField]
		private TextAsset m_scriptTemplatePath;
		public TextAsset scriptTemplatePath => m_scriptTemplatePath;

		[SerializeField]
		private BindingGeneratorType m_bindingGeneratorType;
		public BindingGeneratorType bindingGeneratorType => m_bindingGeneratorType;

		[SerializeField]
		private string m_outputDirectory;
		public string outputDirectory => m_outputDirectory;

		[SerializeField]
		private string m_rootNamespace;
		public string rootNamespace => m_rootNamespace;

		[SerializeField]
		private string m_classNameFormat;
		public string classNameFormat => m_classNameFormat;

		[SerializeField]
		private bool m_generateBindingAutomaticallyOnImport = false;
		public bool generateBindingAutomaticallyOnImport => m_generateBindingAutomaticallyOnImport;

		[SerializeField]
		private bool m_logVerbose = false;
		public bool logVerbose => m_logVerbose;

		[SerializeField]
		private List<DefaultAsset> m_ignoredFolders = new();
		public List<DefaultAsset> ignoredFolders => m_ignoredFolders;

		internal static UxmlBindingSettings GetOrCreateSettings()
		{
			var settings = AssetDatabase.LoadAssetAtPath<UxmlBindingSettings>(BindingSettingsDefaultPath);

			if (settings != null) return settings;

			var targetPath = Path.GetDirectoryName(BindingSettingsDefaultPath);
			if (!Directory.Exists(targetPath))
				Directory.CreateDirectory(targetPath); // make sure we create the folder

			settings = ScriptableObject.CreateInstance<UxmlBindingSettings>();
			settings.m_scriptTemplatePath = null; //$@"{PackageContentPath}\Editor\UxmlBinding\UxmlBindingC#Template.cs.txt";
			settings.m_bindingGeneratorType = BindingGeneratorType.OnCreate;
			settings.m_outputDirectory = @"Bindings";
			settings.m_rootNamespace = "UxmlBindings";
			settings.m_classNameFormat = "{0}Binding";
			settings.m_generateBindingAutomaticallyOnImport = false;
			settings.m_logVerbose = false;
			settings.m_ignoredFolders = new List<DefaultAsset>();

			AssetDatabase.CreateAsset(settings, BindingSettingsDefaultPath);
			AssetDatabase.SaveAssets();
			return settings;
		}

		internal bool UsesPackageTemplateScript() => m_scriptTemplatePath == null;

		/// <summary>
		/// Load the script template file content or returns an error reason as content.
		/// </summary>
		/// <param name="content">The users script template file content, or the in-package script template file content as fallback.</param>
		/// <returns>Returns true, if the content is valid (not empty!), otherwise false.</returns>
		internal bool LoadScriptTemplateFileContent(out string content)
		{
			// handle if the template asset is empty (fallback to projects inner binding template)
			if (m_scriptTemplatePath == null)
			{
				var inPackageScriptTemplatePath = FileUtils.Normalize(Path.Combine(Application.dataPath.Replace("Assets", ""), $@"{PackageContentPath}\Editor\UxmlBinding\UxmlBindingC#Template.cs.txt"));
				if (File.Exists(inPackageScriptTemplatePath))
				{
					content = File.ReadAllText(inPackageScriptTemplatePath);
					if (!string.IsNullOrEmpty(content))
					{
						return true;
					}
					else
					{
						content = $"File \"{inPackageScriptTemplatePath}\"  is empty";
						return false;
					}
				}
				else
				{
					content = $"File \"{inPackageScriptTemplatePath}\" not found";
					return false;
				}
			}
			else
			{
				content = scriptTemplatePath.text;
				return !string.IsNullOrEmpty(content);
			}
		}

		internal bool IsValid(out string reason)
		{
			reason = string.Empty;

			if (string.IsNullOrEmpty(m_rootNamespace))
			{
				reason = "root namespace cannot be empty!";
				return false;
			}

			if (string.IsNullOrEmpty(m_classNameFormat))
			{
				reason = "class name format cannot be empty!";
				return false;
			}

			if (!m_classNameFormat.Contains("{0}"))
			{
				reason = "class name format should contain parameter placeholder \"{0}\"!";
				return false;
			}

			return true;
		}
	}
}
