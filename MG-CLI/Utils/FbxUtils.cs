using Assimp;

namespace MG_CLI;

public static class FbxUtils
{
    public static string[] BuildMeshIndexToNodeNameMap(this Scene scene)
    {
        var names = new string[scene.MeshCount];

        Walk(scene.RootNode);

        // fallback names if some are still missing
        for (int i = 0; i < names.Length; i++)
            if (string.IsNullOrWhiteSpace(names[i]))
                names[i] = $"Mesh_{i:00}";

        return names;

        void Walk(Node n)
        {
            // A node can reference multiple meshes
            foreach (var mi in n.MeshIndices)
            {
                if (mi < 0 || mi >= names.Length) continue;

                // Prefer first name found; or overwrite if you prefer last
                if (string.IsNullOrWhiteSpace(names[mi]))
                    names[mi] = n.Name;
            }

            foreach (var c in n.Children)
                Walk(c);
        }
    }
}