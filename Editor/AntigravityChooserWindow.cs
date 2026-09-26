/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Google. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using IOPath = System.IO.Path;

namespace Google.Unity.Antigravity.Editor
{
	[InitializeOnLoad]
	public class AntigravityChooserWindow : EditorWindow
	{
		private const string SetupShownPrefKey = "com.unity.ide.antigravity.setup_shown_";
		private const string NeverShowPrefKey = "com.unity.ide.antigravity.never_show_setup";

		private Vector2 _scrollPosition;
		private string _customExecutablePath = string.Empty;

		static AntigravityChooserWindow()
		{
			EditorApplication.delayCall += CheckFirstRun;
		}

		private static void CheckFirstRun()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			if (EditorPrefs.GetBool(NeverShowPrefKey, false))
				return;

			var projectKey = SetupShownPrefKey + Application.dataPath.GetHashCode();
			if (!EditorPrefs.GetBool(projectKey, false))
			{
				EditorPrefs.SetBool(projectKey, true);
				ShowWindow();
			}
		}

		[MenuItem("Window/Antigravity/Code Editor Chooser", false, 100)]
		[MenuItem("Tools/Antigravity/Code Editor Chooser", false, 100)]
		public static void ShowWindow()
		{
			var window = GetWindow<AntigravityChooserWindow>(true, "Antigravity Code Editor Chooser", true);
			window.minSize = new Vector2(540, 580);
			window.Show();
		}

		private void OnEnable()
		{
			_customExecutablePath = EditorPrefs.GetString(AntigravityInstallation.CustomEditorPathKey, string.Empty);
		}

		private struct EditorChoice
		{
			public string Id;
			public string DisplayName;
			public string Version;
			public string Description;
			public string DetectedPath;
			public bool IsInstalled => !string.IsNullOrEmpty(DetectedPath) && File.Exists(DetectedPath);
		}

		private void OnGUI()
		{
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			EditorGUILayout.Space(8);
			GUILayout.Label("Unity AI Code Editor Chooser", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"Choose which AI code editor Unity should use when opening scripts and C# projects. " +
				"This package supports Antigravity IDE, Antigravity 2.0, and OpenAI Codex.",
				MessageType.Info);

			EditorGUILayout.Space(6);

			// Current active editor banner
			DrawCurrentEditorBanner();

			EditorGUILayout.Space(8);
			GUILayout.Label("Available Editors", EditorStyles.boldLabel);
			EditorGUILayout.Space(2);

			var installations = Discovery.GetVisualStudioInstallations().ToList();

			// 1. Antigravity IDE
			var ideChoice = ResolveChoice(
				"ide",
				"Antigravity IDE",
				"Full-featured AI-first IDE built on VS Code with C# bridge, IntelliSense, and inline agent assistance.",
				installations,
				GetDefaultCandidateIde());
			DrawEditorCard(ideChoice);

			// 2. Antigravity 2.0
			var ag2Choice = ResolveChoice(
				"antigravity2",
				"Antigravity 2.0",
				"Next-generation agentic desktop development environment for orchestrating autonomous coding agents.",
				installations,
				GetDefaultCandidateAntigravity2());
			DrawEditorCard(ag2Choice);

			// 3. OpenAI Codex
			var codexChoice = ResolveChoice(
				"codex",
				"OpenAI Codex",
				"Desktop coding assistant and editor environment powered by Codex.",
				installations,
				GetDefaultCandidateCodex());
			DrawEditorCard(codexChoice);

			// Custom Path section
			DrawCustomPathSection();

			EditorGUILayout.Space(12);

			// Bottom Controls
			DrawBottomControls();

			EditorGUILayout.EndScrollView();
		}

		private void DrawCurrentEditorBanner()
		{
			var currentPath = CodeEditor.CurrentEditorInstallation;
			var currentName = "None / Other";

			if (Discovery.TryDiscoverInstallation(currentPath, out var currentInst))
			{
				currentName = currentInst.Name;
			}
			else if (!string.IsNullOrEmpty(currentPath))
			{
				currentName = IOPath.GetFileNameWithoutExtension(currentPath);
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Label("Current Active Editor in Unity:", EditorStyles.miniBoldLabel);

			var nameStyle = new GUIStyle(EditorStyles.label)
			{
				richText = true,
				fontSize = 12,
				fontStyle = FontStyle.Bold
			};

			var isAntigravityFamily = currentPath != null &&
				(currentPath.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0 ||
				 currentPath.IndexOf("codex", StringComparison.OrdinalIgnoreCase) >= 0);

			var badge = isAntigravityFamily ? " <color=#4CAF50>[Connected]</color>" : " <color=grey>[External]</color>";
			GUILayout.Label($"{currentName}{badge}", nameStyle);

			if (!string.IsNullOrEmpty(currentPath))
			{
				GUILayout.Label($"<color=grey>{currentPath}</color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
			}
			EditorGUILayout.EndVertical();
		}

		private EditorChoice ResolveChoice(string id, string defaultDisplayName, string description, List<IVisualStudioInstallation> installations, string fallbackCandidate)
		{
			IVisualStudioInstallation matchedInst = null;

			if (id == "ide")
			{
				matchedInst = installations.FirstOrDefault(i =>
					i.Path.IndexOf("ide", StringComparison.OrdinalIgnoreCase) >= 0 ||
					i.Name.IndexOf("ide", StringComparison.OrdinalIgnoreCase) >= 0);
			}
			else if (id == "antigravity2")
			{
				matchedInst = installations.FirstOrDefault(i =>
					(i.Path.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0 ||
					 i.Name.IndexOf("antigravity", StringComparison.OrdinalIgnoreCase) >= 0) &&
					i.Path.IndexOf("ide", StringComparison.OrdinalIgnoreCase) < 0 &&
					i.Name.IndexOf("ide", StringComparison.OrdinalIgnoreCase) < 0);
			}
			else if (id == "codex")
			{
				matchedInst = installations.FirstOrDefault(i =>
					i.Path.IndexOf("codex", StringComparison.OrdinalIgnoreCase) >= 0 ||
					i.Name.IndexOf("codex", StringComparison.OrdinalIgnoreCase) >= 0);
			}

			var detectedPath = matchedInst?.Path;
			if (string.IsNullOrEmpty(detectedPath) && !string.IsNullOrEmpty(fallbackCandidate) && File.Exists(fallbackCandidate))
			{
				detectedPath = fallbackCandidate;
			}

			var displayName = defaultDisplayName;
			var version = string.Empty;

			if (matchedInst != null)
			{
				displayName = matchedInst.Name;
				if (matchedInst.Version != null && matchedInst.Version.Major > 0)
					version = matchedInst.Version.ToString(3);
			}
			else if (!string.IsNullOrEmpty(detectedPath) && File.Exists(detectedPath))
			{
				try
				{
					var fvi = FileVersionInfo.GetVersionInfo(detectedPath);
					var verStr = !string.IsNullOrEmpty(fvi.FileVersion) ? fvi.FileVersion : fvi.ProductVersion;
					if (!string.IsNullOrEmpty(verStr))
						version = verStr.Split('-').First().Trim();
				}
				catch { }
			}

			return new EditorChoice()
			{
				Id = id,
				DisplayName = displayName,
				Version = version,
				Description = description,
				DetectedPath = detectedPath
			};
		}

		private void DrawEditorCard(EditorChoice choice)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			var currentEditorPath = CodeEditor.CurrentEditorInstallation;
			var isActive = !string.IsNullOrEmpty(choice.DetectedPath) &&
				!string.IsNullOrEmpty(currentEditorPath) &&
				string.Equals(IOPath.GetFullPath(choice.DetectedPath), IOPath.GetFullPath(currentEditorPath), StringComparison.OrdinalIgnoreCase);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(choice.DisplayName, EditorStyles.boldLabel);
			if (isActive)
			{
				var activeBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
				{
					normal = { textColor = new Color(0.2f, 0.75f, 0.2f) }
				};
				GUILayout.Label("★ CURRENTLY ACTIVE", activeBadgeStyle);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Label(choice.Description, EditorStyles.wordWrappedMiniLabel);
			EditorGUILayout.Space(2);

			if (choice.IsInstalled)
			{
				GUILayout.Label($"✔ Detected: <color=grey>{choice.DetectedPath}</color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
			}
			else
			{
				GUILayout.Label("⚠ Not detected in standard install directory", EditorStyles.miniLabel);
			}

			EditorGUILayout.Space(4);
			EditorGUILayout.BeginHorizontal();

			if (isActive)
			{
				GUI.enabled = false;
				GUILayout.Button("✓ Active Editor", GUILayout.Height(24), GUILayout.Width(170));
				GUI.enabled = true;
			}
			else if (choice.IsInstalled)
			{
				if (GUILayout.Button($"Select {choice.DisplayName}", GUILayout.Height(24), GUILayout.Width(170)))
				{
					ApplyEditorSelection(choice.DetectedPath, choice.DisplayName);
				}
			}

			if (GUILayout.Button("Browse...", GUILayout.Height(24), GUILayout.Width(80)))
			{
				BrowseForEditor(choice.DisplayName);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space(4);
		}

		private void DrawCustomPathSection()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Label("Custom Executable Path", EditorStyles.boldLabel);
			GUILayout.Label("Specify a custom executable path if installed in an alternative location:", EditorStyles.miniLabel);

			EditorGUILayout.BeginHorizontal();
			_customExecutablePath = EditorGUILayout.TextField(_customExecutablePath);

			if (GUILayout.Button("Browse...", GUILayout.Width(80)))
			{
				BrowseForEditor("Custom Editor");
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(2);
			if (!string.IsNullOrEmpty(_customExecutablePath) && File.Exists(_customExecutablePath))
			{
				if (GUILayout.Button("Set Custom Path as Active Editor", GUILayout.Height(22), GUILayout.Width(240)))
				{
					ApplyEditorSelection(_customExecutablePath, IOPath.GetFileNameWithoutExtension(_customExecutablePath));
				}
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawBottomControls()
		{
			var neverShow = EditorPrefs.GetBool(NeverShowPrefKey, false);
			var newNeverShow = EditorGUILayout.ToggleLeft("Do not show this setup window automatically on package import", neverShow);
			if (newNeverShow != neverShow)
			{
				EditorPrefs.SetBool(NeverShowPrefKey, newNeverShow);
			}

			EditorGUILayout.Space(6);
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Regenerate Project Files", GUILayout.Height(26)))
			{
				RegenerateProjectFiles();
			}

			if (GUILayout.Button("Close", GUILayout.Height(26), GUILayout.Width(100)))
			{
				Close();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void BrowseForEditor(string targetName)
		{
			var extension = Application.platform == RuntimePlatform.WindowsEditor ? "exe" :
							(Application.platform == RuntimePlatform.OSXEditor ? "app" : "");

			var selected = EditorUtility.OpenFilePanel($"Select {targetName} Executable", "", extension);
			if (!string.IsNullOrEmpty(selected) && File.Exists(selected))
			{
				_customExecutablePath = selected;
				EditorPrefs.SetString(AntigravityInstallation.CustomEditorPathKey, selected);
				ApplyEditorSelection(selected, targetName);
			}
		}

		private void ApplyEditorSelection(string path, string displayName)
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				EditorUtility.DisplayDialog("Editor Not Found", $"The file at path does not exist:\n{path}", "OK");
				return;
			}

			CodeEditor.SetExternalScriptEditor(path);

			if (Discovery.TryDiscoverInstallation(path, out var installation))
			{
				installation.ProjectGenerator.Sync();
			}

			var projectKey = SetupShownPrefKey + Application.dataPath.GetHashCode();
			EditorPrefs.SetBool(projectKey, true);

			Repaint();
			Debug.Log($"[Antigravity] Set active code editor to: {displayName} ({path})");
		}

		private static void RegenerateProjectFiles()
		{
			var currentEditor = CodeEditor.CurrentEditorInstallation;
			if (Discovery.TryDiscoverInstallation(currentEditor, out var installation))
			{
				installation.ProjectGenerator.Sync();
				Debug.Log($"[Antigravity] Successfully regenerated project files for {installation.Name}.");
			}
			else
			{
				Debug.LogWarning("[Antigravity] Could not find installation for current editor to regenerate files.");
			}
		}

		private static string GetDefaultCandidateIde()
		{
#if UNITY_EDITOR_WIN
			var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			foreach (var basePath in new[] { localAppPath, programFiles })
			{
				var p = IOPath.Combine(basePath, "Antigravity IDE", "Antigravity IDE.exe");
				if (File.Exists(p)) return p;
			}
#elif UNITY_EDITOR_OSX
			var p = "/Applications/Antigravity IDE.app";
			if (Directory.Exists(p)) return p;
#elif UNITY_EDITOR_LINUX
			var p = "/usr/bin/antigravity-ide";
			if (File.Exists(p)) return p;
#endif
			return null;
		}

		private static string GetDefaultCandidateAntigravity2()
		{
#if UNITY_EDITOR_WIN
			var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			foreach (var basePath in new[] { localAppPath, programFiles })
			{
				var p1 = IOPath.Combine(basePath, "antigravity", "Antigravity.exe");
				if (File.Exists(p1)) return p1;
				var p2 = IOPath.Combine(basePath, "antigravity", "antigravity.exe");
				if (File.Exists(p2)) return p2;
			}
#elif UNITY_EDITOR_OSX
			var p = "/Applications/Antigravity.app";
			if (Directory.Exists(p)) return p;
#elif UNITY_EDITOR_LINUX
			var p = "/usr/bin/antigravity";
			if (File.Exists(p)) return p;
#endif
			return null;
		}

		private static string GetDefaultCandidateCodex()
		{
#if UNITY_EDITOR_WIN
			var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			foreach (var basePath in new[] { localAppPath, programFiles })
			{
				var p1 = IOPath.Combine(basePath, "Codex", "Codex.exe");
				if (File.Exists(p1)) return p1;
				var p2 = IOPath.Combine(basePath, "OpenAI Codex", "Codex.exe");
				if (File.Exists(p2)) return p2;
			}

			// WindowsApps (e.g. OpenAI.Codex)
			try
			{
				var windowsApps = IOPath.Combine(programFiles, "WindowsApps");
				if (Directory.Exists(windowsApps))
				{
					foreach (var dir in Directory.EnumerateDirectories(windowsApps, "*Codex*"))
					{
						var exe = IOPath.Combine(dir, "app", "Codex.exe");
						if (File.Exists(exe))
							return exe;
					}
				}
			}
			catch { }
#elif UNITY_EDITOR_OSX
			var p = "/Applications/Codex.app";
			if (Directory.Exists(p)) return p;
#elif UNITY_EDITOR_LINUX
			var p = "/usr/bin/codex";
			if (File.Exists(p)) return p;
#endif
			return null;
		}
	}
}
