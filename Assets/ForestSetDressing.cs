using System;
using System.Collections.Generic;
using UnityEngine;

public class ForestSetDressing : MonoBehaviour
{
    public GameObject[] treePrefabs;
    public GameObject[] groundPrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] logPrefabs;
    public int treeCount = 84;
    public int groundDetailCount = 140;
    public int rockCount = 24;
    public int logCount = 8;

    private void Start()
    {
        if (transform.Find("Generated Hollowwood") != null) return;
        System.Random random = new System.Random(6724);
        Transform root = new GameObject("Generated Hollowwood").transform;
        root.SetParent(transform, false);
        List<Vector3> gameplayLandmarks = new List<Vector3> { Vector3.zero };
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) gameplayLandmarks.Add(player.transform.position);
        // These are the scene's three relic pickups. Resolve them by name to
        // avoid Unity 6's deprecated global object-search overloads.
        for (int i = 1; i <= 3; i++)
        {
            GameObject pieceObject = GameObject.Find("BridgePiece_0" + i);
            if (pieceObject != null) gameplayLandmarks.Add(pieceObject.transform.position);
        }

        // Fill the expanded map but reserve space at the start, around relics,
        // and around the bridge so important objects stay visible and reachable.
        // Keep tall canopies beyond the camera and relic route. Smaller details
        // still fill the inner clearing without obscuring the player.
        ScatterRing(treePrefabs, treeCount, 22f, 50f, 2f, 6f, 7f, gameplayLandmarks, root, random);
        ScatterRing(groundPrefabs, groundDetailCount, 5f, 48f, 0.65f, 1.45f, 2.6f, gameplayLandmarks, root, random);
        ScatterRing(rockPrefabs, rockCount, 8f, 47f, 0.75f, 1.8f, 4.5f, gameplayLandmarks, root, random);
        ScatterRing(logPrefabs, logCount, 7f, 45f, 0.8f, 1.3f, 3.5f, gameplayLandmarks, root, random);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.fogColor = new Color(0.46f, 0.55f, 0.45f, 1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.68f, 0.74f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.59f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.28f, 0.2f);
    }

    private static void ScatterRing(GameObject[] prefabs, int count, float minRadius, float maxRadius, float minScale, float maxScale, float landmarkClearance, List<Vector3> landmarks, Transform parent, System.Random random)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[random.Next(prefabs.Length)];
            if (prefab == null) continue;

            Vector3 position = Vector3.zero;
            bool foundClearPosition = false;
            for (int attempt = 0; attempt < 32; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                bool nearLandmark = false;
                foreach (Vector3 landmark in landmarks)
                {
                    Vector2 offset = new Vector2(position.x - landmark.x, position.z - landmark.z);
                    if (offset.sqrMagnitude < landmarkClearance * landmarkClearance)
                    {
                        nearLandmark = true;
                        break;
                    }
                }
                if (!nearLandmark)
                {
                    foundClearPosition = true;
                    break;
                }
            }
            if (!foundClearPosition) continue;

            float yaw = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
            GameObject instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), parent);
            instance.name = prefab.name + " (Hollowwood)";
            instance.transform.localScale *= scale;
        }
    }
}
