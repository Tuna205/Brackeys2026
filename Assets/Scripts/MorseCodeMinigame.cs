using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MorseCodeMinigame : MonoBehaviour
{
    private enum MorseSymbolType
    {
        Dot,
        Dash
    }

    private sealed class ActiveSymbol
    {
        public RectTransform RectTransform;
        public MorseSymbolType Type;
        public RectTransform DashFillTransform;
        public bool DashHoldActive;
        public float DashHoldProgress;
    }

    private const float SymbolY = 80f;
    private const float DashWidth = 100f;
    private const float SymbolSpeed = 300f;
    private const float MinimumSpawnInterval = 0.4f;
    private const float MaximumSpawnInterval = 0.8f;
    private const float CorrectScore = 10f;
    private const float IncorrectScore = -5f;
    private const float SuspitionPerTick = 10f;
    private const float SuspitionTickInterval = 1f;

    [SerializeField] private RectTransform panelRectTransform = null;
    [SerializeField] private RectTransform symbolContainer = null;
    [SerializeField] private RectTransform inputZone = null;
    [SerializeField] private RectTransform dotPrefab = null;
    [SerializeField] private RectTransform dashPrefab = null;
    [SerializeField, Min(0.1f)] private float dashHoldDuration = 0.2f;

    private readonly List<ActiveSymbol> activeSymbols = new();

    private Sprite symbolSprite;
    private Texture2D symbolTexture;
    private float spawnTimer;
    private float suspitionTickTimer;
    private bool missingInfoWasReported;
    private bool missingSuspitionWasReported;
    private bool ignoreSpaceUntilReleased;

    private void Awake()
    {
        if (panelRectTransform == null
            || symbolContainer == null
            || inputZone == null
            || dotPrefab == null
            || dashPrefab == null)
        {
            Debug.LogError("Morse Code Minigame is missing its scene UI references.", this);
            enabled = false;
            return;
        }

        symbolSprite = CreateSymbolSprite();
        spawnTimer = 0.75f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
            return;
        }

        bool spaceIsHeld = keyboard != null && keyboard.spaceKey.isPressed;

        if (ignoreSpaceUntilReleased && !spaceIsHeld)
        {
            ignoreSpaceUntilReleased = false;
        }

        if (!ignoreSpaceUntilReleased
            && keyboard != null
            && keyboard.spaceKey.wasPressedThisFrame)
        {
            HandleSpacePressed();
        }

        MoveSymbols(spaceIsHeld);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnSymbol();
            spawnTimer = Random.Range(MinimumSpawnInterval, MaximumSpawnInterval);
        }

        suspitionTickTimer -= Time.deltaTime;
        while (suspitionTickTimer <= 0f)
        {
            AddSuspition(SuspitionPerTick);
            suspitionTickTimer += SuspitionTickInterval;
        }
    }

    private void OnEnable()
    {
        ignoreSpaceUntilReleased = true;
        suspitionTickTimer = SuspitionTickInterval;

        if (activeSymbols.Count == 0)
        {
            SpawnOpeningSymbols();
        }
    }

    private void OnDestroy()
    {
        if (symbolSprite != null)
        {
            Destroy(symbolSprite);
        }

        if (symbolTexture != null)
        {
            Destroy(symbolTexture);
        }
    }

    private void SpawnSymbol()
    {
        SpawnSymbol(panelRectTransform.rect.width + DashWidth);
    }

    private void SpawnOpeningSymbols()
    {
        float nextSymbolX = panelRectTransform.rect.width + DashWidth;
        float halfwayX = panelRectTransform.rect.width * 0.5f;

        while (nextSymbolX >= halfwayX)
        {
            SpawnSymbol(nextSymbolX);
            float interval = Random.Range(MinimumSpawnInterval, MaximumSpawnInterval);
            nextSymbolX -= SymbolSpeed * interval;
        }

        spawnTimer = Random.Range(MinimumSpawnInterval, MaximumSpawnInterval);
    }

    private void SpawnSymbol(float positionX)
    {
        MorseSymbolType type = Random.value < 0.6f
            ? MorseSymbolType.Dot
            : MorseSymbolType.Dash;

        RectTransform symbolRect = Instantiate(
            type == MorseSymbolType.Dot ? dotPrefab : dashPrefab,
            symbolContainer);
        symbolRect.name = type == MorseSymbolType.Dot ? "Morse Dot" : "Morse Dash";
        symbolRect.anchoredPosition = new Vector2(
            positionX,
            SymbolY);

        if (type == MorseSymbolType.Dot)
        {
            symbolRect.GetComponent<Image>().sprite = symbolSprite;
        }

        RectTransform dashFillTransform = null;
        if (type == MorseSymbolType.Dash)
        {
            dashFillTransform = (RectTransform)symbolRect.GetChild(0);
            SetDashFillScale(dashFillTransform, 0f);
        }

        activeSymbols.Add(new ActiveSymbol
        {
            RectTransform = symbolRect,
            Type = type,
            DashFillTransform = dashFillTransform
        });
    }

    private void MoveSymbols(bool spaceIsHeld)
    {
        for (int i = activeSymbols.Count - 1; i >= 0; i--)
        {
            ActiveSymbol symbol = activeSymbols[i];
            symbol.RectTransform.anchoredPosition += Vector2.left
                * (SymbolSpeed * Time.deltaTime);

            if (symbol.Type == MorseSymbolType.Dash)
            {
                if (symbol.DashHoldActive)
                {
                    if (!spaceIsHeld)
                    {
                        ResetDashHold(symbol);
                    }
                    else
                    {
                        symbol.DashHoldProgress += Time.deltaTime
                            / Mathf.Max(0.1f, dashHoldDuration);
                        SetDashFillScale(
                            symbol.DashFillTransform,
                            symbol.DashHoldProgress);

                        if (symbol.DashHoldProgress >= 1f)
                        {
                            AddInfo(CorrectScore);
                            RemoveSymbolAt(i);
                            continue;
                        }
                    }
                }
            }

            float symbolHalfWidth = symbol.RectTransform.rect.width * 0.5f;
            if (symbol.RectTransform.anchoredPosition.x + symbolHalfWidth < 0f)
            {
                AddInfo(IncorrectScore);
                RemoveSymbolAt(i);
            }
        }
    }

    private void HandleSpacePressed()
    {
        int dotIndex = FindSymbolInsideTarget(MorseSymbolType.Dot);
        if (dotIndex >= 0)
        {
            AddInfo(CorrectScore);
            RemoveSymbolAt(dotIndex);
            return;
        }

        int dashIndex = FindSymbolInsideTarget(MorseSymbolType.Dash);
        if (dashIndex >= 0)
        {
            activeSymbols[dashIndex].DashHoldActive = true;
            return;
        }

        AddInfo(IncorrectScore);
    }

    private static void ResetDashHold(ActiveSymbol symbol)
    {
        symbol.DashHoldActive = false;
        symbol.DashHoldProgress = 0f;

        if (symbol.DashFillTransform != null)
        {
            SetDashFillScale(symbol.DashFillTransform, 0f);
        }
    }

    private static void SetDashFillScale(RectTransform fillTransform, float progress)
    {
        Vector3 scale = fillTransform.localScale;
        scale.x = Mathf.Clamp01(progress);
        fillTransform.localScale = scale;
    }

    private int FindSymbolInsideTarget(MorseSymbolType type)
    {
        for (int i = 0; i < activeSymbols.Count; i++)
        {
            ActiveSymbol symbol = activeSymbols[i];
            if (symbol.Type == type && IsInsideTarget(symbol))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsInsideTarget(ActiveSymbol symbol)
    {
        Bounds symbolBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            panelRectTransform,
            symbol.RectTransform);
        Bounds inputBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
            panelRectTransform,
            inputZone);

        return symbolBounds.max.x >= inputBounds.min.x
            && symbolBounds.min.x <= inputBounds.max.x
            && symbolBounds.max.y >= inputBounds.min.y
            && symbolBounds.min.y <= inputBounds.max.y;
    }

    private void RemoveSymbolAt(int index)
    {
        ActiveSymbol symbol = activeSymbols[index];
        activeSymbols.RemoveAt(index);

        if (symbol.RectTransform != null)
        {
            Destroy(symbol.RectTransform.gameObject);
        }
    }

    private void AddInfo(float amount)
    {
        if (Info.instance != null)
        {
            Info.instance.Add(amount);
            return;
        }

        if (!missingInfoWasReported)
        {
            Debug.LogError("Morse Code Minigame could not find the Info system.", this);
            missingInfoWasReported = true;
        }
    }

    private void AddSuspition(float amount)
    {
        if (Suspition.instance != null)
        {
            Suspition.instance.Add(amount);
            return;
        }

        if (!missingSuspitionWasReported)
        {
            Debug.LogError("Morse Code Minigame could not find the Suspition system.", this);
            missingSuspitionWasReported = true;
        }
    }

    private Sprite CreateSymbolSprite()
    {
        const int textureSize = 32;
        const float radius = 15.5f;
        Vector2 center = Vector2.one * ((textureSize - 1) * 0.5f);

        symbolTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false)
        {
            name = "Morse Symbol Shape",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float alpha = Mathf.Clamp01(radius + 0.5f
                    - Vector2.Distance(new Vector2(x, y), center));
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        symbolTexture.SetPixels(pixels);
        symbolTexture.Apply();

        return Sprite.Create(
            symbolTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            Vector4.one * 15f);
    }

}
