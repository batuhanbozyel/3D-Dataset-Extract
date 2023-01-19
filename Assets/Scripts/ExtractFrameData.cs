using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class ExtractFrameData : MonoBehaviour
{
    private static readonly float cameraAnglePerIteration = 5.0f;
    private static readonly float cameraDistanceVaryRange = 1.0f;
    private static readonly float cameraHeightVaryRange = 0.1f;
    private static readonly float cameraTiltAngleRange = 5.0f;

    private Material depthMaterial;
    private Material normalMaterial;

    private GameObject currentModel;
    private int currentCameraVariation;

    private string baseOutputDirectory;
    private string depthOutputDirectory;
    private string normalOutputDirectory;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;

        var normalShader = Shader.Find("Custom/RenderNormal");
        normalMaterial = new Material(normalShader);
        normalMaterial.hideFlags = HideFlags.HideAndDontSave;

        var depthShader = Shader.Find("Custom/RenderDepth");
        depthMaterial = new Material(depthShader);
        depthMaterial.hideFlags = HideFlags.HideAndDontSave;

        originalCameraPosition = Camera.main.transform.position;
        originalCameraRotation = Camera.main.transform.rotation;

        RetrieveNextModelFromQueue();
    }

    private void OnDisable()
    {
        if (depthMaterial != null)
            DestroyImmediate(depthMaterial);

        if (normalMaterial != null)
            DestroyImmediate(normalMaterial);
    }

    private void Update()
    {
        if (currentModel == null)
            return;

        var target = currentModel.transform.position;
        target.y = Camera.main.transform.position.y;

        Camera.main.transform.SetPositionAndRotation(originalCameraPosition + new Vector3(0.0f, Random.Range(-cameraHeightVaryRange, cameraHeightVaryRange), Random.Range(0.0f, cameraDistanceVaryRange)), originalCameraRotation);
        Camera.main.transform.RotateAround(currentModel.transform.position, Vector3.up, GetCurrentCameraAngle());
        Camera.main.transform.RotateAround(currentModel.transform.position, Vector3.forward, Random.Range(-cameraTiltAngleRange, cameraTiltAngleRange));
        Camera.main.transform.LookAt(target);

        var texture = EnvironmentMapSelector.Instance.RandomSkybox();
        var color = texture == null ? Random.ColorHSV() : Color.gray;

        var skybox = Camera.main.GetComponent<Skybox>();
        var currentTexture = skybox.material.GetTexture("_MainTex");

        if (currentTexture != null)
            Resources.UnloadAsset(currentTexture);

        skybox.material.SetColor("_Tint", color);
        skybox.material.SetTexture("_MainTex", texture);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        var filename = GetCurrentVariationFilename();
        currentCameraVariation++;

        if (GetCurrentCameraAngle() >= (360.0f - Mathf.Epsilon))
        {
            Destroy(currentModel);
            RetrieveNextModelFromQueue();
        }

        if (currentModel == null)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            Graphics.Blit(src, dest);
            return;
        }

        // Depth map
        var depthCapture = new RenderTexture(Screen.width, Screen.height, 24);
        Graphics.Blit(src, depthCapture, depthMaterial);
        ExtractRenderResult(depthOutputDirectory, filename);

        // Normal map
        var normalCapture = new RenderTexture(Screen.width, Screen.height, 24);
        Graphics.Blit(src, normalCapture, normalMaterial);
        ExtractRenderResult(normalOutputDirectory, filename);

        // Base map
        Graphics.Blit(src, dest);
        ExtractRenderResult(baseOutputDirectory, filename);
    }

    private static void ExtractRenderResult(string outputDirectory, string filename)
    {
        var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        var image = texture.EncodeToPNG();

        System.IO.File.WriteAllBytes(outputDirectory + "/" + filename, image);
    }

    private float GetCurrentCameraAngle()
    {
        return cameraAnglePerIteration * currentCameraVariation;
    }

    private string GetCurrentVariationFilename()
    {
        return "cameraAngle" + GetCurrentCameraAngle().ToString() + ".png";
    }

    private void RetrieveNextModelFromQueue()
    {
        currentCameraVariation = 0;

        var (model, path) = ModelQueue.Instance.RetrieveNext();
        if (model == null) return;

        var modelTransform = GameObject.Find("Character Transform").transform;
        currentModel = Instantiate(model, modelTransform);
        var meshRenderers = currentModel.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
        {
            foreach (var material in meshRenderer.materials)
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }
        }
        
        int directoryIndex = path.LastIndexOf("/");
        var modelDircetory = path.Substring(0, directoryIndex + 1);
        
        var outputDirectory = Application.dataPath + "/Dataset/" + modelDircetory;
        System.IO.Directory.CreateDirectory(outputDirectory);

        baseOutputDirectory = outputDirectory + "/Base";
        System.IO.Directory.CreateDirectory(baseOutputDirectory);

        depthOutputDirectory = outputDirectory + "/Depth";
        System.IO.Directory.CreateDirectory(depthOutputDirectory);

        normalOutputDirectory = outputDirectory + "/Normal";
        System.IO.Directory.CreateDirectory(normalOutputDirectory);
    }
}
