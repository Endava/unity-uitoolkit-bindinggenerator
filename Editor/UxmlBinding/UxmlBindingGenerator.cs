using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Endava.Editor.UxmlBinding
{
	/// <summary>
	/// This class allows you to generate UIBuilder "Binding" classes.
	/// You can pass an *.uxml file and generate all "named" properties automatically and isolated.
	/// </summary>
	public static class UxmlBindingGenerator
	{
		public static readonly string fileType = ".uxml";
		public static readonly string filterUxmls = "t:VisualTreeAsset";

		private const string NamespacePlaceholder = "#ROOTNAMESPACE#";
		private const string ClassPlaceholder = "#CLASSNAME#";
		private const string PropertiesPlaceholder = "#PROPERTIES#";
		private const string PropertiesConstructionPlaceholder = "#PROPERTYCONSTRUCTION#";

		private static UxmlBindingSettings s_settings;

		public static void BindAll()
		{
			string filter = filterUxmls;
			string[] folder = new string[] { "Assets" };
			string[] assets = AssetDatabase.FindAssets(filter, folder);
			string msg = $"Do you really want to bind {assets.Length} entries?";
			bool accepted = EditorUtility.DisplayDialog("Bindings", msg, "Bind", "Abort");

			if (!accepted) return;

			s_settings = UxmlBindingSettings.GetOrCreateSettings();

			int count = 0;
			var convertedFiles = new List<string>();
			var skippedFiles = new List<string>();
			foreach (string asset in assets)
			{
				string path = AssetDatabase.GUIDToAssetPath(asset);
				string file = Path.GetFileNameWithoutExtension(path);

				count++;
				EditorUtility.DisplayProgressBar("Convert", file, (float)count / (float)assets.Length);

				if (GenerateUxmlBindingInternal(path))
					convertedFiles.Add(path);
				else
					skippedFiles.Add(path);
			}

			EditorUtility.ClearProgressBar();
			Debug.Log($"Converted {convertedFiles.Count}/{assets.Length} uxml files successfully! (Skipped {skippedFiles.Count}/{assets.Length} files)");
			AssetDatabase.Refresh();
		}

		public static void SanitizeBinding()
		{
			string filter = filterUxmls;
			string[] folder = new string[] { "Assets" };
			string[] assets = AssetDatabase.FindAssets(filter, folder);

			s_settings = UxmlBindingSettings.GetOrCreateSettings();
			assets = assets
				.Select(x => AssetDatabase.GUIDToAssetPath(x))
				.Where(x => !IsUxmlPathIgnored(x))
				.Select(x => ConvertUxmlPathToValidName(x))
				.ToArray();
			var bindingWithoutUxmlList = new List<(string,string)>();
			foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, s_settings.outputDirectory), "*.cs"))
			{
				var fileNameOnly = Path.GetFileNameWithoutExtension(file);
				if (!assets.Contains(fileNameOnly, StringComparer.InvariantCultureIgnoreCase))
				{
					bindingWithoutUxmlList.Add((file, fileNameOnly));
				}
			}

			if(bindingWithoutUxmlList.Count > 0)
			{
				string msg = $"Found {bindingWithoutUxmlList.Count} binding files without uxml relation. Do you want to delete them?";
				switch(EditorUtility.DisplayDialogComplex("Binding Check", msg, "Delete All", "Cancel", "File by File"))
				{
					case 1: break; // Cancel
					case 0: // Delete All
						foreach(var entry in bindingWithoutUxmlList)
						{
							var filePath = entry.Item1;
							var fileName = entry.Item2;
							if (!File.Exists(filePath)) continue;
							Debug.LogWarning($"Delete Binding file {fileName}, since uxml has been deleted!");
							File.Delete(filePath);
						}
						Debug.Log($"Successfully deleted {bindingWithoutUxmlList.Count} unreferenced \"Binding\" files!");
						break;

					case 2: // Delete Files by request file by file
						var filesDeleted = 0;
						foreach (var entry in bindingWithoutUxmlList)
						{
							var filePath = entry.Item1;
							var fileName = entry.Item2;
							if (!File.Exists(filePath)) continue;

							bool accepted = EditorUtility.DisplayDialog("Binding Check", $"Do you want to delete \"{fileName}\"?", "Yes", "No");
							if(accepted)
							{
								Debug.LogWarning($"Delete Binding file {fileName}, since uxml has been deleted!");
								File.Delete(filePath);
								filesDeleted++;
							}
						}
						Debug.Log($"Successfully deleted {filesDeleted} unreferenced \"Binding\" files!");
						break;
				}

				AssetDatabase.Refresh();
			}
			else
			{
				Debug.Log($"Binding Sanitize check completed! No issues detected!");
			}
		}

		/// <summary>
		/// Create uxml file bindings for all selected files.
		/// </summary>
		[MenuItem("Assets/Uxml Binding/Create Binding")]
		private static void DoGenerateBindingForUxml()
		{
			s_settings = UxmlBindingSettings.GetOrCreateSettings();
			foreach(var currentSelectedObject in Selection.objects)
			{
				string assetPath = AssetDatabase.GetAssetPath(currentSelectedObject);
				if(Path.GetExtension(assetPath) == fileType)
					GenerateUxmlBindingInternal(assetPath, true);
			}
			
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// Checks the current selected objects and returns true, when all of them are valid uxml files
		/// </summary>
		/// <returns>Returns true if all the selected objects are uxmls, otherwise false.</returns>
		[MenuItem("Assets/Uxml Binding/Create Binding", true)]
		private static bool DoGenerateBindingForUxmlValidate()
		{
			foreach(var selectedObject in Selection.objects)
			{
				string assetPath = AssetDatabase.GetAssetPath(selectedObject);
				if (Path.GetExtension(assetPath) != fileType) return false;
			}
			return true;
		}

		public static bool GenerateUxmlBinding(string uxmlPath)
		{
			s_settings = UxmlBindingSettings.GetOrCreateSettings();
			return GenerateUxmlBindingInternal(uxmlPath);
		}

		public static string GetBindingClassOfUxml(string uxmlPath)
		{
			s_settings = UxmlBindingSettings.GetOrCreateSettings();
			return Path.Combine(s_settings.outputDirectory, ConvertUxmlPathToValidName(uxmlPath));
		}

		private static bool GenerateUxmlBindingInternal(string uxmlPath, bool force = false)
		{
			if (!force && IsUxmlPathIgnored(uxmlPath)) return false;

			if (!s_settings.IsValid(out var errorReason))
			{
				Debug.LogError($"Binding generator invalid: {errorReason}");
				return false;
			}

			if(s_settings.UsesPackageTemplateScript())
			{
				Debug.LogWarning("C# script template of package used - Please create a custom script template (or copy package template) to your project structure (safety reasons!).");
			}

			if (s_settings.logVerbose)
				Debug.Log($"Start to generate uxml binding file for \"{uxmlPath}\"");

			try
			{
				List<(Type, string name)> uxmlContent = ParseUxml(uxmlPath);
				List<(Type type, string name)> filteredUxmlContent = ValidateParsedUxmlEntries(uxmlContent);

				string fileName = ConvertUxmlPathToValidName(uxmlPath);
				WriteBindingCSFile(fileName, filteredUxmlContent);

				if (s_settings.logVerbose)
					Debug.Log($"Successfully generated \"{fileName}\" for file \"{uxmlPath}\"");

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to generate uxml binding for file \"{uxmlPath}\". Reason: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Converts a uxml filename into a "valid" usable class name identifier.
		/// </summary>
		private static string ConvertUxmlPathToValidName(string uxmlPath)
		{
			if (string.IsNullOrEmpty(uxmlPath)) return null;

			string result = string.Format(s_settings.classNameFormat, Path.GetFileNameWithoutExtension(uxmlPath)); // extract filename and apply template
			result = result[0].ToString().ToUpper() + result[1..]; // make first letter uppercase
			result = Regex.Replace(result, "[.-]", "_"); // replace invalid characters within the className
			return result;
		}

		private static List<(Type, string name)> ParseUxml(string uxmlPath)
		{
			var uiAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
			if (uiAsset == null)
			{
				Debug.LogError($"File {uxmlPath} does not exist!");
				return null;
			}

			var result = new List<(Type, string name)>();
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			// add all "visual element derived classes" to the list - including their name
			PropertyInfo prop = uiAsset.GetType().GetProperty("visualElementAssets", flags);
			foreach (object entry in (IList)prop.GetValue(uiAsset))
			{
				PropertyInfo elementProp = entry.GetType().GetProperty("fullTypeName", flags);
				string elementTypeStr = (string)elementProp?.GetValue(entry);
				Type elementType = GetAssemblyType(elementTypeStr);

				if (elementType == null)
					continue;

				string elementName = GetNamePropertyFromVisualAssetProperties(entry);

				//Debug.Log($"{elementType} | {elementName}");
				result.Add((elementType, elementName));
			}

			// add template assets to the list as well - but use visual element as their "base type"
			PropertyInfo templateAssetProp = uiAsset.GetType().GetProperty("templateAssets", flags);
			foreach (object entry in (IList)templateAssetProp.GetValue(uiAsset))
			{
				string elementName = GetNamePropertyFromVisualAssetProperties(entry);

				//Debug.Log($"{elementType} | {elementName}");
				result.Add((typeof(VisualElement), elementName));
			}


			return result;

			string GetNamePropertyFromVisualAssetProperties(object entry)
			{
				string elementName = string.Empty;
				FieldInfo elementPropInfo = entry.GetType().GetField("m_Properties", flags);
				if(elementPropInfo == null) elementPropInfo = entry.GetType().BaseType?.GetField("m_Properties", flags);

				if (elementPropInfo != null)
				{
					var elementPropList = (IList)elementPropInfo.GetValue(entry);

					for (int i = 0; i < elementPropList.Count; i += 2)
					{
						string key = Convert.ToString(elementPropList[i]);
						string val = Convert.ToString(elementPropList[i + 1]);

						if (key == "name")
						{
							elementName = val;
							break;
						}
					}
				}

				return elementName;
			}
		}
		private static Type GetAssemblyType(string typeString)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly assembly in assemblies)
			{
				Type type = assembly.GetType(typeString);

				if (type != null)
					return type;
			}

			return null;
		}

		private static List<(Type type, string name)> ValidateParsedUxmlEntries(List<(Type type, string name)> entries)
		{
			if (entries == null || entries.Count == 0) return entries;

			IEnumerable<string> duplicates = entries.Select(x => x.name)
				.GroupBy(x => x)
				.SelectMany(g => g.Skip(1));

			var result = new List<(Type type, string name)>();
			foreach ((Type type, string name) entry in entries)
			{
				// all entries needs a valid type!
				if (entry.type == null)
					continue;

				// exclude entries, where no name has been defined
				if (string.IsNullOrEmpty(entry.name))
				{
					if(s_settings.logVerbose)
						Debug.LogWarning($"[UxmlBindingGenerator] Element of type {entry.type} with no name will be skipped. Add a name to fix.");

					continue;
				}

				// do not allow entries, which have duplicated names
				if (duplicates.Contains(entry.name))
				{
					if (s_settings.logVerbose)
						Debug.LogWarning($"[UxmlBindingGenerator] Duplicate named element {entry.name} will be skipped. Rename the element to fix.");

					continue;
				}

				result.Add(entry);
			}

			return result;
		}

		private static void WriteBindingCSFile(string className, List<(Type, string)> properties)
		{
			if (string.IsNullOrEmpty(className)) throw new ArgumentNullException(@"""className"" cannot be null or empty");
			if (properties == null) throw new ArgumentNullException(@"""properties"" cannot be null or empty");

			// read template from file and check for correctness
			if(!s_settings.LoadScriptTemplateFileContent(out var templateContent)) throw new FileLoadException("Template file could not be found or was empty!");
			if (!templateContent.Contains(NamespacePlaceholder)) throw new DataMisalignedException($"Template file should contain {NamespacePlaceholder}");
			if (!templateContent.Contains(ClassPlaceholder)) throw new DataMisalignedException($"Template file should contain {ClassPlaceholder}");
			if (!templateContent.Contains(PropertiesPlaceholder)) throw new DataMisalignedException($"Template file should contain {PropertiesPlaceholder}");
			if (!templateContent.Contains(PropertiesConstructionPlaceholder)) throw new DataMisalignedException($"Template file should contain {PropertiesConstructionPlaceholder}");

			// replace the template placeholders with content
			templateContent = templateContent.Replace(NamespacePlaceholder, s_settings.rootNamespace);
			templateContent = templateContent.Replace(ClassPlaceholder, className);

			var propertiesContent = new List<string>();
			var propertiesConstructor = new List<string>();
			var additionalUsings = new HashSet<string>();
			foreach ((Type type, string name) property in properties)
			{
				if (!additionalUsings.Contains(property.type.Namespace))
					additionalUsings.Add(property.type.Namespace);

				string fixedPropertyName = Regex.Replace(property.name, @"[\#\-\<\>]", "");
				fixedPropertyName = char.IsLetter(fixedPropertyName[0]) ? fixedPropertyName : $"_{fixedPropertyName}";

				switch (s_settings.bindingGeneratorType)
				{
					case BindingGeneratorType.OnCreate:
						propertiesContent.Add($"public readonly {property.type.Name} {fixedPropertyName};");
						propertiesConstructor.Add($"{fixedPropertyName} = parent.Q<{property.type.Name}>(\"{property.name}\");");
						break;
					case BindingGeneratorType.LazyLoaded:
						propertiesContent.Add($"private {property.type.Name} _{fixedPropertyName};");
						propertiesContent.Add($@"public {property.type.Name} {fixedPropertyName} => _{fixedPropertyName} ?? (_{fixedPropertyName} = _____root_____.Q<{property.type.Name}>(""{property.name}""));");
						propertiesConstructor.Add($"_{fixedPropertyName} = null;");
						break;
					case BindingGeneratorType.Proxy:
						propertiesContent.Add($"public {property.type.Name} {fixedPropertyName} => _____root_____.Q<{property.type.Name}>(\"{property.name}\");");
						break;
					default:
						throw new NotImplementedException();
				}
			}

			string levelOfIndentation;
			// replace the properties by keeping the indentation
			if(propertiesContent.Count > 0)
			{
				Match match = Regex.Match(templateContent, $@"([\t ]*)({PropertiesPlaceholder})");
				if (match.Success)
				{
					levelOfIndentation = match.Groups[1].Value;
				}
				else
				{
					levelOfIndentation = string.Empty;
					Debug.LogWarning($"Indentation check for {PropertiesPlaceholder} failed!");
				}
				templateContent = templateContent.Replace(PropertiesPlaceholder, string.Join("\n" + levelOfIndentation, propertiesContent));
			}
			else
			{
				templateContent = templateContent.Replace(PropertiesPlaceholder, string.Empty);
			}

			// replace the initialization within the constructor by keeping the indentation
			if(propertiesConstructor.Count > 0)
			{
				Match match = Regex.Match(templateContent, $@"([\t ]*)({PropertiesConstructionPlaceholder})");
				if (match.Success)
				{
					levelOfIndentation = match.Groups[1].Value;
				}
				else
				{
					levelOfIndentation = string.Empty;
					Debug.LogWarning($"indentation check for {PropertiesConstructionPlaceholder} failed!");
				}
				templateContent = templateContent.Replace(PropertiesConstructionPlaceholder, string.Join("\n" + levelOfIndentation, propertiesConstructor));
			}
			else
			{
				templateContent = templateContent.Replace(PropertiesConstructionPlaceholder, string.Empty);
			}

			// write replaced content to cs file destination
			string outputFolder = FileUtils.Normalize(Path.Combine(Application.dataPath, s_settings.outputDirectory));
			var outputDirectory = new DirectoryInfo(outputFolder);
			outputDirectory?.Create(); // create the folder if it does not exists (will do nothing if exist)
			string outputFile = FileUtils.Normalize(Path.Combine(outputFolder, $"{className}.cs"));
			File.WriteAllText(outputFile, templateContent);
		}

		private static bool IsUxmlPathIgnored(string uxmlPath)
		{
			if (s_settings.ignoredFolders != null)
			{
				string uxmlFolderNormalized = FileUtils.Trim(FileUtils.NormalizeWindowsToUnix(Path.GetDirectoryName(uxmlPath))).ToLower();
				foreach (var ignoredFolderAsset in s_settings.ignoredFolders)
				{
					if (ignoredFolderAsset == null) continue;

					var ignoredFolderPath = AssetDatabase.GetAssetPath(ignoredFolderAsset);
					if(!Directory.Exists(ignoredFolderPath))
					{
						Debug.LogWarning($"Entry in ignored path is not a folder type: {ignoredFolderPath}");
						continue;
					}

					string normalizedIgnoredFolder = FileUtils.Trim(FileUtils.NormalizeWindowsToUnix(ignoredFolderPath)).ToLower();
					if (uxmlFolderNormalized.Contains(normalizedIgnoredFolder))
					{
						if (s_settings.logVerbose)
							Debug.Log($"Ignore uxml {uxmlPath} since it inside ignored folder {ignoredFolderPath}");

						return true;
					}
				}
			}

			return false;
		}
	}
}
