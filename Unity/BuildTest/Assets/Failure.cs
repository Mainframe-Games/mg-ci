using UnityEditor;
using UnityEngine;

public class Failure : MonoBehaviour
{
	private void Start()
	{
		Debug.Log(nameof(Failure));

#if UNITY_EDITOR
		EditorApplication.Beep();
#endif // move this line down to make it fail
	}
}