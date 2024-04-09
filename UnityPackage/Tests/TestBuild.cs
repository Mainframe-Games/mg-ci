using Mainframe.CI.Editor;
using NUnit.Framework;

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
    }
}