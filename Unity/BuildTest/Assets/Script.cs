using UnityEditor;
using UnityEngine;

namespace DefaultNamespace
{
	public class Script : MonoBehaviour
	{
		private void Start()
		{
			Debug.Log(nameof(Script));

// #if UNITY_EDITOR
			EditorApplication.Beep();
// #endif
		}
	}
}