using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// A small coin-catching game: move, catch, and increase the score.
public class CoinCatcherGame : MonoBehaviour
{
    public float basketSpeed = 8f;
    public float coinSpeed = 3f;
    public float spawnInterval = 1f;
    [Range(0f, 1f)] public float bombChance = .2f;
    public int Score { get; private set; }

    readonly List<Transform> coins = new List<Transform>();
    readonly HashSet<Transform> bombs = new HashSet<Transform>();
    Transform basket;
    TMP_Text scoreText;
    Sprite squareSprite, coinSprite;
    float spawnTimer;
    float horizontalLimit;

    void Start()
    {
        var camera = Camera.main;
        if (!camera) camera = FindFirstObjectByType<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.backgroundColor = new Color(.12f, .19f, .27f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        squareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)))));
        texture.Apply();
        coinSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);

        basket = MakeSprite("Basket", squareSprite, new Vector2(0, -3.7f), new Vector2(1.6f, .45f), new Color(.3f, .8f, .5f));
        var canvasObject = new GameObject("Game UI", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        canvasObject.transform.SetParent(transform);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 600);
        scaler.matchWidthOrHeight = .5f;
        scoreText = MakeText(canvasObject.transform, "Score: 0", new Vector2(.5f, 1), new Vector2(0, -45), 32);
        spawnTimer = .5f;
    }

    Transform MakeSprite(string objectName, Sprite sprite, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(objectName, typeof(SpriteRenderer));
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.transform.localScale = size;
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        return go.transform;
    }

    TMP_Text MakeText(Transform parent, string text, Vector2 anchor, Vector2 position, int size)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(850, 60);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text; label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    void Update()
    {
        if (!basket) return;
        var camera = Camera.main;
        if (!camera) camera = FindFirstObjectByType<Camera>();
        horizontalLimit = Mathf.Max(0, camera.orthographicSize * camera.aspect - .9f);
        var keyboard = Keyboard.current;
        float direction = 0;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction--;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction++;
            if (keyboard.rKey.wasPressedThisFrame) ResetGame();
        }
        var position = basket.position;
        position.x = Mathf.Clamp(position.x + direction * basketSpeed * Time.deltaTime, -horizontalLimit, horizontalLimit);
        basket.position = position;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnItem(Random.value < bombChance);
            spawnTimer = Mathf.Max(.1f, spawnInterval);
        }
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            var coin = coins[i];
            float previousY = coin.position.y;
            coin.position += Vector3.down * coinSpeed * Time.deltaTime;
            bool caught = previousY >= -3.3f && coin.position.y <= -3.3f && Mathf.Abs(coin.position.x - basket.position.x) < 1f;
            if (caught) { Score = bombs.Contains(coin) ? 0 : Score + 1; scoreText.text = "Score: " + Score; }
            if (caught || coin.position.y < -5.5f)
            {
                bombs.Remove(coin);
                Destroy(coin.gameObject);
                coins.RemoveAt(i);
            }
        }
    }

    void SpawnItem(bool isBomb)
    {
        var item = MakeSprite(isBomb ? "Bomb" : "Coin", coinSprite,
            new Vector2(Random.Range(-horizontalLimit, horizontalLimit), 4.3f),
            Vector2.one * .5f, isBomb ? new Color(.95f, .2f, .2f) : new Color(1, .8f, .15f));
        coins.Add(item);
        if (isBomb)
        {
            bombs.Add(item);
            var fuse = MakeSprite("Fuse", squareSprite, Vector2.zero, Vector2.one, new Color(1, .8f, .15f));
            fuse.SetParent(item, false);
            fuse.localPosition = new Vector3(.15f, .6f, 0);
            fuse.localScale = new Vector3(.12f, .35f, 1);
        }
    }

    public void ResetGame()
    {
        foreach (var coin in coins) Destroy(coin.gameObject);
        coins.Clear(); bombs.Clear(); Score = 0; scoreText.text = "Score: 0";
        basket.position = new Vector3(0, -3.7f, 0); spawnTimer = .5f;
    }

    void OnDestroy()
    {
        if (coinSprite) { Destroy(coinSprite.texture); Destroy(coinSprite); }
        if (squareSprite) Destroy(squareSprite);
    }
}
