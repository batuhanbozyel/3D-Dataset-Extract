using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnvironmentMapSelector
{
    private static EnvironmentMapSelector instance;
    public static EnvironmentMapSelector Instance
    {
        get
        {
            if (instance == null)
                instance = new();

            return instance;
        }
    }

    private static readonly float colorPickChance = 0.1f;
    private readonly Texture[] skyboxes;

    EnvironmentMapSelector()
    {
        var directory = new DirectoryInfo(Application.dataPath + "/Resources/Environment Maps/");
        var fileInfos = directory.GetFiles("*.hdr", SearchOption.AllDirectories);

        skyboxes = new Texture[fileInfos.Length];
        for (int i = 0; i < skyboxes.Length; i++)
        {
            var resourcePath = GetResourcePathFromFileInfo(fileInfos[i]);
            skyboxes[i] = Resources.Load<Texture>(resourcePath);
        }
    }

    public Texture RandomSkybox()
    {
        int rand = Random.Range(0, (int)(skyboxes.Length * (1.0f + colorPickChance)));
        if (rand >= skyboxes.Length)
            return null;

        return skyboxes[rand];
    }

    private string GetResourcePathFromFileInfo(FileInfo info)
    {
        string resourcePath = info.DirectoryName + "/" + Path.GetFileNameWithoutExtension(info.Name);
        string resourcesDirectory = Application.dataPath + "/Resources/";
        int index = resourcePath.IndexOf(resourcesDirectory);
        return resourcePath.Remove(index, resourcesDirectory.Length);
    }
}
