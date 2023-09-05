using BuildSystem;
using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// Doc: https://docs.unity3d.com/ScriptReference/SettingsProvider.html
/// Built in USS variables: https://docs.unity3d.com/2022.1/Documentation/Manual/UIE-USS-UnityVariables.html
/// </summary>
public static class ProjectSettingsRegister
{
	private const string LABEL = "Build System";

	[SettingsProvider]
	public static SettingsProvider CreateMyCustomSettingsProvider()
	{
		// First parameter is the path in the Settings window.
		// Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
		var provider = new SettingsProvider($"Project/{LABEL}", SettingsScope.Project)
		{
			label = LABEL,
			keywords = BuildConfig.GetKeywords(),
			activateHandler = OnActivate,
			titleBarGuiHandler = TitleBarGuiHandler,
			deactivateHandler = DeactivateHandler,
			inspectorUpdateHandler = InspectorUpdateHandler,
			footerBarGuiHandler = FooterBarGuiHandler,
			hasSearchInterestHandler = HasSearchInterestHandler
		};

		return provider;
	}

	private static void OnActivate(string searchContext, VisualElement rootElement)
	{
		var settings = BuildConfig.GetOrCreateSettings();

		var title = new Label { text = LABEL };
		// title.AddToClassList("title");
		rootElement.Add(title);

		var properties = new VisualElement { style = { flexDirection = FlexDirection.Column } };
		// properties.AddToClassList("property-list");
		rootElement.Add(properties);

		var tf = new TextField { label = nameof(settings.Url), value = settings.Url };
		// tf.AddToClassList("property-value");
		properties.Add(tf);
	}
	
	private static bool HasSearchInterestHandler(string arg)
	{
		return false;
	}

	private static void FooterBarGuiHandler()
	{
	}

	private static void InspectorUpdateHandler()
	{
	}

	private static void DeactivateHandler()
	{
	}

	private static void TitleBarGuiHandler()
	{
	}
}