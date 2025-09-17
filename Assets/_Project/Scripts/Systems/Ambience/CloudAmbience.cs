using System.Collections.Generic;
using UnityEngine;

namespace ShapeBlaster.Systems.Ambience
{
    /// <summary>
    /// Spawns and drifts cloud sprites across the scene to add ambient motion.
    /// Attach to an empty GameObject, assign sprites, and tweak the ranges to taste.
    /// </summary>
    [DisallowMultipleComponent]
    public class CloudAmbience : MonoBehaviour
    {
        private const int CurrentSerializationVersion = 1;

        [Header("Assets")]
        [Tooltip("Sprites to use for the drifting clouds.")]
        [SerializeField] private Sprite[] cloudSprites;
        [SerializeField, Range(1, 32)] private int cloudCount = 6;

        [Header("Layout")]
        [Tooltip("Horizontal span in world units that clouds travel across.")]
        [SerializeField] private float horizontalSpan = 18f;
        [Tooltip("Random Y offset range (local space) applied per cloud.")]
        [SerializeField] private Vector2 heightRange = new Vector2(2f, 6f);
        [Tooltip("Extra padding when recycling clouds so the wrap isn't visible in frame.")]
        [SerializeField] private float recyclePadding = 4f;

        [Header("Motion")]
        [Tooltip("Random speed range in world units per second.")]
        [SerializeField] private Vector2 speedRange = new Vector2(0.4f, 1.2f);
        [Tooltip("Random uniform scale applied to spawned clouds.")]
        [SerializeField] private Vector2 scaleRange = new Vector2(0.45f, 0.75f);
        [Tooltip("Flip a portion of clouds horizontally for variety.")]
        [SerializeField] private bool randomFlipX = true;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private Color tint = Color.white;

        [SerializeField, HideInInspector] private int serializedVersion = 0;

        private readonly List<Cloud> clouds = new();
        private readonly List<GameObject> spawnedClouds = new();
        private float leftEdge;
        private float rightEdge;

        private void Awake()
        {
            UpdateBounds();
        }

        private void Start()
        {
            if (cloudSprites == null || cloudSprites.Length == 0)
            {
                Debug.LogWarning("[CloudAmbience] No cloud sprites assigned. Nothing will spawn.", this);
                enabled = false;
                return;
            }

            SpawnClouds();
        }

        private void OnValidate()
        {
            horizontalSpan = Mathf.Max(1f, horizontalSpan);
            recyclePadding = Mathf.Max(0f, recyclePadding);

            heightRange.x = Mathf.Min(heightRange.x, heightRange.y);
            heightRange.y = Mathf.Max(heightRange.x + 0.1f, heightRange.y);

            if (speedRange.x > speedRange.y) speedRange = new Vector2(speedRange.y, speedRange.x);
            if (scaleRange.x > scaleRange.y) scaleRange = new Vector2(scaleRange.y, scaleRange.x);

            if (serializedVersion < CurrentSerializationVersion)
            {
                sortingOrder = 20;
                serializedVersion = CurrentSerializationVersion;
            }

            UpdateBounds();
            ApplyRenderingSettings();
        }

        private void Update()
        {
            if (clouds.Count == 0) return;
            float delta = Application.isPlaying ? Time.deltaTime : 0f;
            MoveClouds(delta);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < spawnedClouds.Count; i++)
            {
                if (spawnedClouds[i] != null)
                {
                    Destroy(spawnedClouds[i]);
                }
            }
            spawnedClouds.Clear();
            clouds.Clear();
        }

        private void SpawnClouds()
        {
            CleanupClouds();

            for (int i = 0; i < cloudCount; i++)
            {
                CreateCloud(i);
            }
        }

        private void CleanupClouds()
        {
            foreach (var go in spawnedClouds)
            {
                if (go != null) Destroy(go);
            }
            spawnedClouds.Clear();
            clouds.Clear();
        }

        private void CreateCloud(int index)
        {
            var sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
            if (sprite == null) return;

            var go = new GameObject($"Cloud_{index}");
            go.transform.SetParent(transform, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            go.transform.localScale = Vector3.one * scale;
            if (randomFlipX && Random.value > 0.5f)
            {
                renderer.flipX = true;
            }

            float posX = Random.Range(leftEdge, rightEdge);
            float posY = Random.Range(heightRange.x, heightRange.y);
            go.transform.localPosition = new Vector3(posX, posY, 0f);

            float width = sprite.bounds.size.x * scale;
            float speed = Random.Range(speedRange.x, speedRange.y);

            var cloud = new Cloud
            {
                Transform = go.transform,
                Renderer = renderer,
                HalfWidth = width * 0.5f,
                Speed = speed
            };

            clouds.Add(cloud);
            spawnedClouds.Add(go);
        }

        private void MoveClouds(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            for (int i = 0; i < clouds.Count; i++)
            {
                var cloud = clouds[i];
                var t = cloud.Transform;

                float newX = t.localPosition.x + cloud.Speed * deltaTime;
                if (newX - cloud.HalfWidth > rightEdge + recyclePadding)
                {
                    newX = leftEdge - recyclePadding - cloud.HalfWidth;
                    float newY = Random.Range(heightRange.x, heightRange.y);
                    t.localPosition = new Vector3(newX, newY, 0f);

                    if (cloudSprites.Length > 0)
                    {
                        var sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
                        if (sprite != null && sprite != cloud.Renderer.sprite)
                        {
                            cloud.Renderer.sprite = sprite;
                            float scale = t.localScale.x;
                            cloud.HalfWidth = sprite.bounds.size.x * scale * 0.5f;
                        }
                    }

                    if (randomFlipX)
                    {
                        cloud.Renderer.flipX = Random.value > 0.5f;
                    }

                    cloud.Speed = Random.Range(speedRange.x, speedRange.y);
                    clouds[i] = cloud;
                    continue;
                }

                t.localPosition = new Vector3(newX, t.localPosition.y, t.localPosition.z);
            }
        }

        private void ApplyRenderingSettings()
        {
            for (int i = 0; i < clouds.Count; i++)
            {
                var renderer = clouds[i].Renderer;
                if (renderer == null) continue;
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
                renderer.color = tint;
            }
        }

        private void UpdateBounds()
        {
            leftEdge = -horizontalSpan * 0.5f;
            rightEdge = horizontalSpan * 0.5f;
        }

        private struct Cloud
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float HalfWidth;
            public float Speed;
        }
    }
}


