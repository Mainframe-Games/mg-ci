using System;
using System.IO;
using System.Reflection;
using BuildSystem;
using BuildSystem.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// Doc: https://docs.unity3d.com/ScriptReference/SettingsProvider.html
/// Built in USS variables: https://docs.unity3d.com/2022.1/Documentation/Manual/UIE-USS-UnityVariables.html
/// </summary>
public class ProjectSettingsRegister : SettingsProvider
{
	private const string LABEL = "Build System";
	private static BuildConfig ConfigInstance => ScriptableObjectCreator.GetOrCreateAsset<BuildConfig>("Assets/Settings/BuildSettings/BuildConfig.asset");

	private ProjectSettingsRegister(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
	{
	}

	[SettingsProvider]
	public static SettingsProvider CreateMyCustomSettingsProvider()
	{
		return new ProjectSettingsRegister($"Project/{LABEL}", SettingsScope.Project)
		{
			keywords = BuildConfig.GetKeywords()
		};
	}

	public override void OnActivate(string searchContext, VisualElement rootElement)
	{
		var title = new Label { text = LABEL, style = { fontSize = 20 } };
		title.AddToClassList("title");
		rootElement.Add(title);

		var objField = new ObjectField("Config") { objectType = typeof(BuildConfig) };
		objField.value = ConfigInstance;
		objField.RegisterValueChangedCallback(evt =>
		{
			Debug.Log($"New Config: {evt.newValue}", evt.newValue);
			DrawConfig(rootElement, evt.newValue);
		});
		rootElement.Add(objField);
		
		if (objField.value)
			DrawConfig(rootElement, objField.value);
	}

	private static void DrawConfig(VisualElement rootElement, Object config)
	{
		var serialisedSettings = new SerializedObject(config);
		GetElementsFromFields(config, serialisedSettings, rootElement);
		rootElement.Bind(serialisedSettings);

		var saveButton = new Button(SaveJson) { text = "Save JSON" };
		rootElement.Add(saveButton);
	}
	
	private static void SaveJson()
	{
		ConfigInstance.Save();
	}

	private static void GetElementsFromFields(object obj, SerializedObject serializedObject, VisualElement rootElement)
	{
		var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

		foreach (var fieldInfo in fields)
		{
			var field = new PropertyField(serializedObject.FindProperty(fieldInfo.Name));
			rootElement.Add(field);
		}
	}
}