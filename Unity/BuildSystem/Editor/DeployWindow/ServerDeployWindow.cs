using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BuildSystem
{
	public class ServerDeployWindow : EditorWindow
	{
		[MenuItem("Window/Quick Server Deploy")]
		private static void ShowWindow()
		{
			GetWindow<ServerDeployWindow>("Server Deploy").Show();
		}

		public string SteamSdk;
		public string SteamVdf;
		public string SteamSetLive;

		public string SteamUsername;
		public string SteamPassword;
		public string SteamGuard;

		public BuildSettings BuildSettings;
		private BuildGroup _buildSettingsGroup;
		
		private SerializedObject _so;
		private SerializedProperty _buildSettings;

		private DateTime _lastDeployTime;
		public bool AutoSwitchBack;

		private static string SteamCmdExtension => Environment.OSVersion.Platform is PlatformID.MacOSX ? "sh" : "exe";
		
		private static bool NeedsBuild
		{
			get => PlayerPrefs.GetInt("NeedsBuild") == 1;
			set
			{
				PlayerPrefs.SetInt("NeedsBuild", value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

		private void OnEnable()
		{
			Debug.Log($"OnEnable :: NeedsBuild: {NeedsBuild}");
			InternalVersion.Load();
			
			_so = new SerializedObject(this);
			_buildSettings = _so.FindProperty(nameof(BuildSettings));

			SteamSdk = PlayerPrefs.GetString(nameof(SteamSdk), string.Empty);
			SteamVdf = PlayerPrefs.GetString(nameof(SteamVdf), string.Empty);
			SteamSetLive = PlayerPrefs.GetString(nameof(SteamSetLive), string.Empty);

			var savedDeployTime = PlayerPrefs.GetString(nameof(_lastDeployTime));
			_lastDeployTime = DateTime.TryParse(savedDeployTime, out var dt) ? dt : DateTime.MinValue;

			if (NeedsBuild)
			{
				Build();
				NeedsBuild = false;
			}
		}

		private void OnDisable()
		{
			PlayerPrefs.Save();
		}

		private void OnGUI()
		{
			_buildSettingsGroup.TargetGroup = BuildSettings ? BuildSettings.TargetGroup : default;
			_buildSettingsGroup.Target = BuildSettings ? BuildSettings.Target : default;
			_buildSettingsGroup.SubTarget = BuildSettings ? BuildSettings.SubTarget : default;
			
			EditorGUILayout.BeginHorizontal();
			{
				DrawTextField(nameof(SteamSdk), ref SteamSdk, true);
				if (GUILayout.Button("...", GUILayout.Width(30)))
					OpenFilePanel("Steam SDK", SteamCmdExtension, ref SteamSdk);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				DrawTextField(nameof(SteamVdf), ref SteamVdf, true);
				if (GUILayout.Button("...", GUILayout.Width(30)))
					OpenFilePanel("Steam VDF", "vdf", ref SteamVdf);
			}
			EditorGUILayout.EndHorizontal();

			DrawTextField(nameof(SteamSetLive), ref SteamSetLive, true);
			DrawTextField(nameof(SteamUsername), ref SteamUsername);
			SteamPassword = EditorGUILayout.PasswordField(nameof(SteamPassword), SteamPassword);
			DrawTextField(nameof(SteamGuard), ref SteamGuard);

			// build settings
			EditorGUILayout.PropertyField(_buildSettings);
			EditorGUILayout.LabelField(nameof(InternalVersion), InternalVersion.Version);
			AutoSwitchBack = EditorGUILayout.Toggle(nameof(AutoSwitchBack), AutoSwitchBack);
			
			// build
			DrawButtons();

			_so.ApplyModifiedProperties();
		}

		private static void DrawTextField(string label, ref string value, bool save = false)
		{
			var newValue = EditorGUILayout.TextField(label, value);
			if (save && newValue != value)
				PlayerPrefs.SetString(label, newValue);
			value = newValue;
		}

		private void DrawButtons()
		{
			EditorGUILayout.BeginHorizontal();
			if (DrawButton("Switch To Build Target", enabledPredicate: CanSwitchToBuildTarget))
				SwitchTarget(_buildSettingsGroup);
			if (DrawButton("Switch To Platform Default", enabledPredicate: () => !BuildGroup.Current.Equals(BuildGroup.DefaultPlatform)))
				SwitchTarget(BuildGroup.DefaultPlatform);
			EditorGUILayout.EndHorizontal();
			
			
			EditorGUILayout.BeginHorizontal();
			if (DrawButton("Build", 30, CanBuild))
				Build();

			var canDeploy = CanDeploy(out var buildFile);
			if (DrawButton("Deploy To Steam", 30, () => canDeploy))
				Deploy();
			EditorGUILayout.EndHorizontal();

			// meta data
			if (buildFile?.Exists ?? false)
			{
				EditorGUILayout.LabelField($"Last Build: {buildFile.LastWriteTime} ({GetMinsAgo(buildFile.LastWriteTime)})");
				EditorGUILayout.LabelField($"Last Deploy: {_lastDeployTime} ({GetMinsAgo(_lastDeployTime)})");
			}
		}

		private static string GetMinsAgo(DateTime time)
		{
			return $"{(DateTime.Now - time).TotalMinutes:0} mins ago";
		}

		private static bool DrawButton(string label, int height = 20, Func<bool> enabledPredicate = null)
		{
			GUI.enabled = enabledPredicate?.Invoke() ?? true;
			var isPressed = GUILayout.Button(label, GUILayout.Height(height));
			GUI.enabled = true;
			return isPressed;
		}

		private bool CanSwitchToBuildTarget()
		{
			return BuildSettings && !_buildSettingsGroup.Equals(BuildGroup.Current);
		}

		private bool CanBuild()
		{
			return BuildSettings && _buildSettingsGroup.Equals(BuildGroup.Current);
		}

		private bool CanDeploy(out FileInfo buildFile)
		{
			var isInfoFilled = !string.IsNullOrEmpty(SteamUsername)
			                   && !string.IsNullOrEmpty(SteamPassword)
			                   && !string.IsNullOrEmpty(SteamSdk);

			buildFile = GetBuildFile();
			var buildFileExists = buildFile?.Exists ?? false;
			return isInfoFilled && buildFileExists;
		}

		private static void OpenFilePanel(string title, string extension, ref string result)
		{
			var path = string.IsNullOrEmpty(result) ? Environment.CurrentDirectory : result;
			var output = EditorUtility.OpenFilePanel(title, path, extension);
			if (!string.IsNullOrEmpty(output))
				result = output;
		}

		private void Build()
		{
			SwitchTarget(_buildSettingsGroup, true);
			InternalVersion.Bump();
			Debug.Log("Building Player...");
			BuildScript.BuildPlayer(BuildSettings);
			
			if (AutoSwitchBack)
				SwitchTarget(BuildGroup.DefaultPlatform);
		}

		private static void SwitchTarget(BuildGroup build, bool triggerBuild = false)
		{
			if (build.Equals(BuildGroup.Current))
				return;
			
			Debug.Log($"Switching to {build}");
			
            EditorUserBuildSettings.standaloneBuildSubtarget = build.SubTarget;
            EditorUserBuildSettings.SwitchActiveBuildTarget(build.TargetGroup, build.Target);
            
            Debug.Log("Forcing recompile...");
			NeedsBuild = triggerBuild;
            var sw = Stopwatch.StartNew();
            CompilationPipeline.RequestScriptCompilation();
            CompilationPipeline.compilationFinished += o =>
            {
	            Debug.Log($"Recompile completed {sw.ElapsedMilliseconds / 1000f:0.0}s");
            };
		}

		private FileInfo GetBuildFile()
		{
			if (!BuildSettings)
				return null;

			var path = Path.Combine(BuildSettings.BuildPath, Application.productName + BuildSettings.Extension);
			return new FileInfo(path);
		}

		private async void Deploy()
		{
			SetVdfProperties(SteamVdf,
				("Desc", $"v{InternalVersion.Version}"),
				("SetLive", SteamSetLive));
			
			// deploy
			var args = new StringBuilder();
			args.Append("+login");
			args.Append($" {SteamUsername}");
			args.Append($" {SteamPassword}");
			args.Append($" {SteamGuard}");
			args.Append($" +run_app_build \"{SteamVdf}\"");
			args.Append(" +quit");
			await Run(SteamSdk, args.ToString());

			_lastDeployTime = DateTime.Now;
			PlayerPrefs.SetString(nameof(_lastDeployTime), _lastDeployTime.ToString());
			
			Debug.Log("Deployed Completed");
		}
		
		private static void SetVdfProperties(string vdfPath, params (string key, string value)[] values)
		{
			if (!File.Exists(vdfPath))
				throw new FileNotFoundException($"File doesn't exist at {vdfPath}");
		
			var vdfLines = File.ReadAllLines(vdfPath);

			foreach ((string key, string value) in values)
			{
				if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
					continue;
			
				foreach (var line in vdfLines)
				{
					if (!line.Contains($"\"{key}\""))
						continue;

					var index = Array.IndexOf(vdfLines, line);
					vdfLines[index] = $"\t\"{key}\" \"{value}\"";
				}
			}

			File.WriteAllText(vdfPath, string.Join("\n", vdfLines));
		}

		private static async Task Run(string fileName, string ags)
		{
			Debug.Log($"[CMD] {fileName} {ags}");

			var procStartInfo = new ProcessStartInfo(fileName)
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Environment.CurrentDirectory,
				Arguments = ags
			};

			var process = Process.Start(procStartInfo);
			process.BeginOutputReadLine();
			process.OutputDataReceived += (sender, args) =>
			{
				if (!string.IsNullOrEmpty(args.Data))
					Debug.Log(args.Data);
			};

			while (!process.HasExited)
				await Task.Yield();
			
			Debug.Log($"Exit: {process.ExitCode}");
		}
	}
}