using UnityEditor;
using UnityEngine;

namespace DefaultNamespace
{
	public class Script : MonoBehaviour
	{
		private void Start()
		{
			Debug.Log(nameof(Script));

			// comment out #if for a build failure
#if UNITY_EDITOR
			EditorApplication.Beep();
#endif
		}
	}
}