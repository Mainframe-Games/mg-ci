using System.Linq;
using Mainframe.CI.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Mainframe.CI.Tests
{
    public class TestBuild
    {
        // A Test behaves as an ordinary method
        [Test]
        public void TestBuildPlayer()
        {
            BuildScript.BuildPlayer();
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        // [UnityTest]
        // public IEnumerator TestBuildProcessWithEnumeratorPasses()
        // {
        //     // Use the Assert class to test conditions.
        //     // Use yield to skip a frame.
        //     yield return null;
        // }

        [Test]
        public void TestGettingScenes()
        {
            var scenesArg = "Assets/Scenes/Scene.unity,Assets/Scenes/Scene2.unity";
            var sceneNames = scenesArg.Split(',');
            var scenePaths = sceneNames
                .Select(AssetDatabase.AssetPathToGUID)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
        }
    }
}