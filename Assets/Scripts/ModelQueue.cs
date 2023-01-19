using System.IO;
using UnityEngine;

public class ModelQueue
{
    private static ModelQueue instance;
    public static ModelQueue Instance
    {
        get
        {
            if (instance == null)
                instance = new();

            return instance;
        }
    }

    private readonly FileInfo[] modelInfos;
    private int iterator;

    ModelQueue()
    {
        var directory = new DirectoryInfo(Application.dataPath + "/Resources/Models/");
        modelInfos = directory.GetFiles("*.fbx", SearchOption.AllDirectories);
        iterator = 0;
    }

    public (GameObject, string) RetrieveNext()
    {
        if (iterator == modelInfos.Length)
            return (null, null);

        var info = modelInfos[iterator++];
        var resourcePath = GetResourcePathFromFileInfo(info);
        var model = Resources.Load<GameObject>(resourcePath);
        Resources.UnloadUnusedAssets();

        return (model, resourcePath);
    }

    private string GetResourcePathFromFileInfo(FileInfo info)
    {
        string resourcePath = info.DirectoryName + "/" + Path.GetFileNameWithoutExtension(info.Name);
        string resourcesDirectory = Application.dataPath + "/Resources/";
        int index = resourcePath.IndexOf(resourcesDirectory);
        return resourcePath.Remove(index, resourcesDirectory.Length);
    }
}
