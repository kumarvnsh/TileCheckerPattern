using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using Newtonsoft.Json;

public class TileCheckerPattern : MonoBehaviour
{
    private const string API_URL = "https://quicklook.orientbell.com/Task/gettiles.php";
    private const string LOCAL_PATH = "Tiles"; // Relative path within the persistent data path

    private IEnumerator Start()
    {
        // Step 1: Get a list of tile URLs from the API
        UnityWebRequest webRequest = UnityWebRequest.Get(API_URL);
        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to get tiles from API: " + webRequest.error);
            yield break;
        }

        string tilesJson = webRequest.downloadHandler.text;
        Tile[] tiles = JsonConvert.DeserializeObject<Tile[]>(tilesJson);

        // Step 2: Download tiles and save them locally
        for (int i = 0; i < tiles.Length; i++)
        {
            Tile tile = tiles[i];

            UnityWebRequest tileRequest = UnityWebRequestTexture.GetTexture(tile.url);
            yield return tileRequest.SendWebRequest();

            if (tileRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to download tile: " + tile.url);
                continue;
            }

            Texture2D tileTexture = DownloadHandlerTexture.GetContent(tileRequest);
            byte[] tileData = tileTexture.EncodeToPNG();

            string persistentPath = Path.Combine(Application.persistentDataPath, LOCAL_PATH);
            if (!Directory.Exists(persistentPath))
            {
                Directory.CreateDirectory(persistentPath);
            }

            string localFilePath = Path.Combine(persistentPath, "Tile" + i + ".png");
            File.WriteAllBytes(localFilePath, tileData);
        }

        // Step 3: Create a checker pattern using the downloaded tiles
        string[] tileFilePaths = Directory.GetFiles(Path.Combine(Application.persistentDataPath, LOCAL_PATH));

        if (tileFilePaths.Length < 2)
        {
            Debug.LogError("Not enough tile files available.");
            yield break;
        }

        Texture2D[] checkerTextures = new Texture2D[2];

        for (int i = 0; i < 2; i++)
        {
            byte[] textureData = File.ReadAllBytes(tileFilePaths[i]);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(textureData);
            checkerTextures[i] = texture;
        }

        GameObject plane = GameObject.Find("Plane");
        Renderer planeRenderer = plane.GetComponent<Renderer>();
        Material planeMaterial = planeRenderer.material;
        planeMaterial.mainTexture = CreateCheckerPattern(checkerTextures);
    }

    private Texture2D CreateCheckerPattern(Texture2D[] textures)
    {
        int tileSize = textures[0].width; // Assuming all tiles have the same size
        int patternSize = 8; // Size of the checker pattern

        Texture2D checkerTexture = new Texture2D(patternSize * tileSize, patternSize * tileSize);

        for (int y = 0; y < patternSize; y++)
        {
            for (int x = 0; x < patternSize; x++)
            {
                Texture2D tileTexture = textures[(x + y) % 2];
                for (int ty = 0; ty < tileSize; ty++)
                {
                    for (int tx = 0; tx < tileSize; tx++)
                    {
                        Color color = tileTexture.GetPixel(tx, ty);
                        checkerTexture.SetPixel(x * tileSize + tx, y * tileSize + ty, color);
                    }
                }
            }
        }

        checkerTexture.Apply();
        return checkerTexture;
    }

    [System.Serializable]
    private class Tile
    {
        public string url;
        public int width;
        public int height;
    }
}
