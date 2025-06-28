//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.InputSystem;

//public class PaintManager : MonoBehaviour
//{
//    [Serializable]
//    public class ColorProperty
//    {
//        public string name;
//        public float bounceFactor;
//        public float duration;
//        public float speedBoost;
//        public Color paintColor;
//    }

//    [Serializable]
//    public class HiddenPath
//    {
//        public Vector2 position;
//        public Vector2 size;
//        public bool revealed;
//    }

//    [SerializeField] private float brushSize = 20f;
//    [SerializeField] private float brushHeight = 5f;
//    [SerializeField] private GameObject platformPrefab;
//    [SerializeField] private GameObject particlePrefab;
//    [SerializeField] private Transform platformsParent;
//    [SerializeField] private string selectedColor = "purple";
//    [SerializeField] private GameObject player; // Reference to the player GameObject
//    [SerializeField] private ParticleSystem paintParticles; // Reusable particle system
//    [SerializeField] private VirtualJoystick joystick; // Reference to the virtual joystick

//    [SerializeField]
//    private Dictionary<string, ColorProperty> colorProperties = new Dictionary<string, ColorProperty>()
//    {
//        { "purple", new ColorProperty { name = "Platform", paintColor = new Color(0.55f, 0.27f, 0.68f, 1f) } },
//        { "blue", new ColorProperty { name = "Bouncy", bounceFactor = 6f, paintColor = new Color(0.2f, 0.6f, 0.9f, 1f) } },
//        { "red", new ColorProperty { name = "Temporary", duration = 3f, paintColor = new Color(0.9f, 0.3f, 0.2f, 1f) } },
//        { "yellow", new ColorProperty { name = "Speed", speedBoost = 2f, paintColor = new Color(0.95f, 0.77f, 0.06f, 1f) } }
//    };

//    [SerializeField] private List<HiddenPath> hiddenPaths = new List<HiddenPath>();

//    private List<GameObject> brushStrokes = new List<GameObject>();
//    private Animator playerAnimator; // Reference to the player's Animator

//    public event Action<GameObject> OnPaintApplied;
//    public event Action<HiddenPath> OnPathRevealed;

//    private void Start()
//    {
//        InitializeHiddenPaths();
//        // Get the Animator component from the player
//        if (player != null)
//        {
//            playerAnimator = player.GetComponent<Animator>();
//            if (playerAnimator == null)
//            {
//                Debug.LogError("Player GameObject does not have an Animator component!");
//            }
//        }
//        else
//        {
//            Debug.LogError("Player reference is not set in PaintManager!");
//        }

//        // Ensure paintParticles doesn't loop
//        if (paintParticles != null)
//        {
//            var main = paintParticles.main;
//            main.loop = false; // Disable looping
//            paintParticles.Stop(); // Ensure it starts stopped
//        }
//    }

//    private void Update()
//    {
//        HandleInput();
//        UpdateTemporaryPlatforms();
//    }

//    private void InitializeHiddenPaths()
//    {
//        hiddenPaths.Add(new HiddenPath { position = new Vector2(300f, 350f), size = new Vector2(100f, 20f), revealed = false });
//        hiddenPaths.Add(new HiddenPath { position = new Vector2(450f, 250f), size = new Vector2(80f, 20f), revealed = false });
//        hiddenPaths.Add(new HiddenPath { position = new Vector2(650f, 200f), size = new Vector2(100f, 20f), revealed = false });
//        hiddenPaths.Add(new HiddenPath { position = new Vector2(200f, 320f), size = new Vector2(50f, 20f), revealed = false });

//        foreach (var path in hiddenPaths)
//        {
//            GameObject pathVisual = new GameObject("HiddenPath");
//            pathVisual.transform.position = new Vector3(path.position.x, path.position.y, 0f);
//            pathVisual.transform.parent = platformsParent;

//            SpriteRenderer renderer = pathVisual.AddComponent<SpriteRenderer>();
//            renderer.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);
//            renderer.transform.localScale = new Vector3(path.size.x / 10f, path.size.y / 10f, 1f);
//        }
//    }

//    private void HandleInput()
//    {
//        //Check if the mouse is over a UI element or the joystick is active
//        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() || (joystick != null && joystick.IsDragging))
//        {
//            return; // If over UI or joystick is active, skip painting and animation
//        }

//        // Determine player's facing direction (1 = right, -1 = left)
//        float facingDirection = player.transform.localScale.x;

//        // Handle mouse input (Editor)
//        if (Input.GetMouseButtonDown(0))
//        {
//            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//            mousePos.z = 0;

//            // Check if the paint position is ahead of the player
//            if (!IsPositionAheadOfPlayer(mousePos, facingDirection))
//            {
//                Debug.Log("Mouse click is not ahead of player, skipping painting.");
//                return;
//            }

//            if (playerAnimator != null)
//            {
//                playerAnimator.SetBool("Spell", true);
//                AudioManager.Instance.PlayWaterSound();
//            }
//            ApplyPaint(mousePos);
//        }
//        else if (Input.GetMouseButtonUp(0))
//        {
//            if (playerAnimator != null)
//            {
//                playerAnimator.SetBool("Spell", false);
//            }
//        }
//        else if (Input.GetMouseButton(0))
//        {
//            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//            mousePos.z = 0;

//            if (!IsPositionAheadOfPlayer(mousePos, facingDirection))
//            {
//                Debug.Log("Mouse drag is not ahead of player, skipping painting.");
//                return;
//            }

//            ApplyPaint(mousePos);
//        }

//        // Handle touch input (Mobile)
//        if (Touchscreen.current != null)
//        {
//            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
//            {
//                Vector3 touchPos = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
//                touchPos.z = 0;

//                if (!IsPositionAheadOfPlayer(touchPos, facingDirection))
//                {
//                    Debug.Log("Touch is not ahead of player, skipping painting.");
//                    return;
//                }

//                if (playerAnimator != null)
//                {
//                    playerAnimator.SetBool("Spell", true);
//                    AudioManager.Instance.PlayWaterSound();
//                }
//                ApplyPaint(touchPos);
//            }
//            else if (!Touchscreen.current.primaryTouch.press.isPressed)
//            {
//                if (playerAnimator != null)
//                {
//                    playerAnimator.SetBool("Spell", false);
//                }
//            }
//            else if (Touchscreen.current.primaryTouch.press.isPressed && !EventSystem.current.IsPointerOverGameObject())
//            {
//                Vector3 touchPos = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
//                touchPos.z = 0;

//                if (!IsPositionAheadOfPlayer(touchPos, facingDirection))
//                {
//                    Debug.Log("Touch drag is not ahead of player, skipping painting.");
//                    return;
//                }

//                ApplyPaint(touchPos);
//            }
//        }

//        if (Input.GetKeyDown(KeyCode.Alpha1)) SetPaintColor("purple");
//        if (Input.GetKeyDown(KeyCode.Alpha2)) SetPaintColor("blue");
//        if (Input.GetKeyDown(KeyCode.Alpha3)) SetPaintColor("red");
//        if (Input.GetKeyDown(KeyCode.Alpha4)) SetPaintColor("yellow");
//    }

//    private bool IsPositionAheadOfPlayer(Vector3 paintPosition, float facingDirection)
//    {
//        // Player's position in world space
//        Vector3 playerPos = player.transform.position;

//        // Determine if the paint position is on the correct side relative to the player's facing direction
//        if (facingDirection > 0) // Facing right
//        {
//            return paintPosition.x > playerPos.x; // Paint must be to the right of the player
//        }
//        else // Facing left
//        {
//            return paintPosition.x < playerPos.x; // Paint must be to the left of the player
//        }
//    }

//    private void UpdateTemporaryPlatforms()
//    {
//        for (int i = brushStrokes.Count - 1; i >= 0; i--)
//        {
//            PaintStroke stroke = brushStrokes[i].GetComponent<PaintStroke>();

//            if (stroke != null && stroke.IsTemporary)
//            {
//                if (stroke.RemainingTime <= 0)
//                {
//                    Destroy(brushStrokes[i]);
//                    brushStrokes.RemoveAt(i);
//                }
//                else
//                {
//                    SpriteRenderer renderer = brushStrokes[i].GetComponent<SpriteRenderer>();
//                    if (renderer != null)
//                    {
//                        Color color = renderer.color;
//                        color.a = stroke.RemainingTime / stroke.Duration;
//                        renderer.color = color;
//                    }
//                }
//            }
//        }
//    }

//    public GameObject ApplyPaint(Vector3 position)
//    {
//        GameObject newPlatform = Instantiate(platformPrefab, position, Quaternion.identity, platformsParent);
//        newPlatform.name = $"{selectedColor}Platform";
//        newPlatform.tag = "Paint";
//        newPlatform.layer = LayerMask.NameToLayer("Platforms"); // Ensure it's on the correct layer
//        newPlatform.transform.localScale = new Vector3(brushSize / 10f, brushHeight / 10f, 1f);

//        PaintStroke stroke = newPlatform.AddComponent<PaintStroke>();
//        stroke.Initialize(selectedColor, GetColorProperties(selectedColor));

//        SpriteRenderer renderer = newPlatform.GetComponent<SpriteRenderer>();
//        if (renderer != null && colorProperties.TryGetValue(selectedColor, out ColorProperty props))
//        {
//            renderer.color = props.paintColor;
//            // Configure and play the paint particles
//            if (paintParticles != null)
//            {
//                paintParticles.transform.position = position; // Move to paint location
//                var main = paintParticles.main;
//                main.startColor = props.paintColor; // Set color

//                paintParticles.Play(); // Play once

//            }
//        }

//        BoxCollider2D collider = newPlatform.GetComponent<BoxCollider2D>();
//        if (collider == null)
//        {
//            collider = newPlatform.AddComponent<BoxCollider2D>();
//        }

//        CreatePaintParticles(position, selectedColor); // Keep this for additional effects if desired
//        brushStrokes.Add(newPlatform);
//        CheckHiddenPathInteraction(position);
//        OnPaintApplied?.Invoke(newPlatform);

//        return newPlatform;
//    }

//    private void CreatePaintParticles(Vector3 position, string colorType)
//    {
//        int particleCount = GetParticleCountForColor(colorType);
//        GameObject particleSystem = Instantiate(particlePrefab, position, Quaternion.identity);
//        ParticleSystem particles = particleSystem.GetComponent<ParticleSystem>();

//        if (particles != null && colorProperties.TryGetValue(colorType, out ColorProperty props))
//        {
//            var main = particles.main;
//            main.startColor = props.paintColor;
//            main.loop = false; // Ensure no looping for instantiated particles

//            switch (colorType)
//            {
//                case "red":
//                    main.startLifetime = 0.5f;
//                    break;
//                case "yellow":
//                    var velocity = particles.velocityOverLifetime;
//                    velocity.enabled = true;
//                    velocity.x = new ParticleSystem.MinMaxCurve(2.0f, 5.0f);
//                    break;
//            }

//            var emission = particles.emission;
//            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));
//            Destroy(particleSystem, main.startLifetime.constant + 0.5f); // Destroy after playing once
//        }
//    }

//    private int GetParticleCountForColor(string colorType)
//    {
//        switch (colorType)
//        {
//            case "blue": return 12;
//            case "red": return 8;
//            case "yellow": return 15;
//            case "purple": return 10;
//            default: return 8;
//        }
//    }

//    private ColorProperty GetColorProperties(string colorType)
//    {
//        if (colorProperties.TryGetValue(colorType, out ColorProperty property))
//        {
//            return property;
//        }
//        return new ColorProperty { name = "Default" };
//    }

//    private void CheckHiddenPathInteraction(Vector3 position)
//    {
//        for (int i = 0; i < hiddenPaths.Count; i++)
//        {
//            HiddenPath path = hiddenPaths[i];
//            if (!path.revealed &&
//                position.x >= path.position.x - 10 &&
//                position.x <= path.position.x + path.size.x + 10 &&
//                position.y >= path.position.y - 10 &&
//                position.y <= path.position.y + path.size.y + 10)
//            {
//                hiddenPaths[i].revealed = true;
//                GameObject pathPlatform = Instantiate(platformPrefab,
//                    new Vector3(path.position.x + path.size.x / 2, path.position.y, 0f),
//                    Quaternion.identity,
//                    platformsParent);

//                pathPlatform.name = "RevealedPath";
//                pathPlatform.tag = "Platform"; // Tag as Platform for consistency
//                pathPlatform.layer = LayerMask.NameToLayer("Platforms");
//                pathPlatform.transform.localScale = new Vector3(path.size.x / 10f, path.size.y / 10f, 1f);

//                SpriteRenderer renderer = pathPlatform.GetComponent<SpriteRenderer>();
//                if (renderer != null)
//                {
//                    renderer.color = new Color(0.4f, 0.6f, 1.0f);
//                }

//                GameObject revealEffect = new GameObject("RevealEffect");
//                revealEffect.transform.position = new Vector3(path.position.x + path.size.x / 2, path.position.y, 0f);
//                ParticleSystem particles = revealEffect.AddComponent<ParticleSystem>();
//                var main = particles.main;
//                main.startColor = new Color(0.4f, 0.6f, 1.0f, 0.7f);
//                main.startSize = 0.5f;
//                main.startLifetime = 1.0f;
//                var emission = particles.emission;
//                emission.rateOverTime = 20;
//                Destroy(revealEffect, 2.0f);

//                OnPathRevealed?.Invoke(path);
//                Debug.Log($"Hidden path revealed at ({path.position.x}, {path.position.y})");
//                break;
//            }
//        }
//    }

//    public bool SetPaintColor(string colorName)
//    {
//        if (colorProperties.ContainsKey(colorName))
//        {
//            selectedColor = colorName;
//            Debug.Log($"Paint color changed to {colorName}");
//            return true;
//        }
//        Debug.LogWarning($"Invalid paint color: {colorName}");
//        return false;
//    }

//    public List<GameObject> GetActiveBrushStrokes()
//    {
//        brushStrokes.RemoveAll(item => item == null);
//        return brushStrokes;
//    }

//    public List<HiddenPath> GetRevealedPaths()
//    {
//        return hiddenPaths.FindAll(path => path.revealed);
//    }

//    public void ClearAllPaint()
//    {
//        foreach (var stroke in brushStrokes)
//        {
//            if (stroke != null)
//            {
//                Destroy(stroke);
//            }
//        }
//        brushStrokes.Clear();
//        Debug.Log("All paint cleared from the world");
//    }
//}

//public class PaintStroke : MonoBehaviour
//{
//    public string PaintType { get; private set; }
//    public float BounceFactor { get; private set; }
//    public float SpeedBoost { get; private set; }
//    public bool IsTemporary { get; private set; }
//    public float Duration { get; private set; }
//    public float RemainingTime { get; private set; }

//    private float creationTime;

//    public void Initialize(string paintType, PaintManager.ColorProperty properties)
//    {
//        PaintType = paintType;
//        creationTime = Time.time;

//        switch (paintType)
//        {
//            case "blue":
//                BounceFactor = properties.bounceFactor;
//                break;
//            case "red":
//                IsTemporary = true;
//                Duration = properties.duration;
//                RemainingTime = Duration;
//                break;
//            case "yellow":
//                SpeedBoost = properties.speedBoost;
//                break;
//        }
//    }

//    private void Update()
//    {
//        if (IsTemporary)
//        {
//            RemainingTime = Duration - (Time.time - creationTime);
//        }
//    }
//}



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PaintManager : MonoBehaviour
{
    [Serializable]
    public class ColorProperty
    {
        public string name;
        public float bounceFactor;
        public float duration;
        public float speedBoost;
        public Color paintColor;
        public float lifetime = -1f;
    }

    [Serializable]
    public class HiddenPath
    {
        public Vector2 position;
        public Vector2 size;
        public bool revealed;
    }

    [SerializeField] private float brushSize = 20f;
    [SerializeField] private float brushHeight = 5f;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Transform platformsParent;
    [SerializeField] private string selectedColor = "purple";
    [SerializeField] private GameObject player;
    [SerializeField] private ParticleSystem paintParticles;
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private float paintParticleLifetime = 3f; // NEW PARAMETER for particle lifetime

    [SerializeField]
    private Dictionary<string, ColorProperty> colorProperties = new Dictionary<string, ColorProperty>()
    {
        { "purple", new ColorProperty { name = "Platform", paintColor = new Color(0.55f, 0.27f, 0.68f, 1f), lifetime = 30f } },
        { "blue", new ColorProperty { name = "Bouncy", bounceFactor = 6f, paintColor = new Color(0.2f, 0.6f, 0.9f, 1f), lifetime = 30f } },
        { "red", new ColorProperty { name = "Temporary", duration = 3f, paintColor = new Color(0.9f, 0.3f, 0.2f, 1f), lifetime = 30f } },
        { "yellow", new ColorProperty { name = "Speed", speedBoost = 1.5f, paintColor = new Color(0.95f, 0.77f, 0.06f, 1f), lifetime = 30f } }
    };

    [SerializeField] private List<HiddenPath> hiddenPaths = new List<HiddenPath>();

    private List<GameObject> brushStrokes = new List<GameObject>();
    private Animator playerAnimator;

    public event Action<GameObject> OnPaintApplied;
    public event Action<HiddenPath> OnPathRevealed;

    private void Start()
    {
        InitializeHiddenPaths();

        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogError("Player GameObject does not have an Animator component!");
            }
        }
        else
        {
            Debug.LogError("Player reference is not set in PaintManager!");
        }

        if (paintParticles != null)
        {
            var main = paintParticles.main;
            main.loop = false;
            paintParticles.Stop();
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateTemporaryPlatforms();
    }

    private void InitializeHiddenPaths()
    {
        hiddenPaths.Add(new HiddenPath { position = new Vector2(300f, 350f), size = new Vector2(100f, 20f), revealed = false });
        hiddenPaths.Add(new HiddenPath { position = new Vector2(450f, 250f), size = new Vector2(80f, 20f), revealed = false });
        hiddenPaths.Add(new HiddenPath { position = new Vector2(650f, 200f), size = new Vector2(100f, 20f), revealed = false });
        hiddenPaths.Add(new HiddenPath { position = new Vector2(200f, 320f), size = new Vector2(50f, 20f), revealed = false });

        foreach (var path in hiddenPaths)
        {
            GameObject pathVisual = new GameObject("HiddenPath");
            pathVisual.transform.position = new Vector3(path.position.x, path.position.y, 0f);
            pathVisual.transform.parent = platformsParent;

            SpriteRenderer renderer = pathVisual.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);
            renderer.transform.localScale = new Vector3(path.size.x / 10f, path.size.y / 10f, 1f);
        }
    }

    private void HandleInput()
    {
        float facingDirection = player.transform.localScale.x;

        if (IsPointerOverUI())
        {
            StopSpellAnimation();
            return;
        }

        bool isPainting = false;
        Vector3 paintPos = Vector3.zero;

        if (!Application.isMobilePlatform)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
            {
                paintPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                paintPos.z = 0;

                if (IsPositionAheadOfPlayer(paintPos, facingDirection))
                {
                    isPainting = true;
                    if (playerAnimator != null && !playerAnimator.GetBool("Spell"))
                    {
                        playerAnimator.SetBool("Spell", true);
                        AudioManager.Instance.PlayWaterSound();
                    }
                    ApplyPaint(paintPos);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                StopSpellAnimation();
            }
        }
        else if (Touchscreen.current != null)
        {
            bool isTouchPressed = Touchscreen.current.primaryTouch.press.isPressed;

            if (isTouchPressed)
            {
                paintPos = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
                paintPos.z = 0;

                if (IsPositionAheadOfPlayer(paintPos, facingDirection))
                {
                    isPainting = true;
                    if (playerAnimator != null && !playerAnimator.GetBool("Spell"))
                    {
                        playerAnimator.SetBool("Spell", true);
                        AudioManager.Instance.PlayWaterSound();
                    }
                    ApplyPaint(paintPos);
                }
            }
            else
            {
                StopSpellAnimation();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetPaintColor("purple");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetPaintColor("blue");
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetPaintColor("red");
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetPaintColor("yellow");

        if (!isPainting)
        {
            StopSpellAnimation();
        }
    }

    private bool IsPointerOverUI()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
#elif UNITY_IOS || UNITY_ANDROID
        if (EventSystem.current == null) return false;
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        else
            return false;
#else
        return false;
#endif
    }

    private void StopSpellAnimation()
    {
        if (playerAnimator != null && playerAnimator.GetBool("Spell"))
        {
            playerAnimator.SetBool("Spell", false);
        }
    }

    private bool IsPositionAheadOfPlayer(Vector3 paintPosition, float facingDirection)
    {
        Vector3 playerPos = player.transform.position;
        return facingDirection > 0 ? paintPosition.x > playerPos.x : paintPosition.x < playerPos.x;
    }


    //private void UpdateTemporaryPlatforms()
    //{
    //    for (int i = brushStrokes.Count - 1; i >= 0; i--)
    //    {
    //        GameObject brush = brushStrokes[i];

    //        if (brush == null)
    //        {
    //            brushStrokes.RemoveAt(i); // Clean up destroyed references
    //            continue;
    //        }

    //        PaintStroke stroke = brush.GetComponent<PaintStroke>();
    //        if (stroke != null && stroke.IsTemporary)
    //        {
    //            if (stroke.RemainingTime <= 0)
    //            {
    //                Destroy(brush);
    //                brushStrokes.RemoveAt(i);
    //            }
    //            else
    //            {
    //                SpriteRenderer renderer = brush.GetComponent<SpriteRenderer>();
    //                if (renderer != null)
    //                {
    //                    Color color = renderer.color;
    //                    color.a = stroke.RemainingTime / stroke.Duration;
    //                    renderer.color = color;
    //                }
    //            }
    //        }
    //    }
    //}


    private void UpdateTemporaryPlatforms()
    {
        for (int i = brushStrokes.Count - 1; i >= 0; i--)
        {
            GameObject brush = brushStrokes[i];

            if (brush == null)
            {
                brushStrokes.RemoveAt(i);
                continue;
            }

            PaintStroke stroke = brush.GetComponent<PaintStroke>();
            if (stroke == null)
            {
                Destroy(brush);
                brushStrokes.RemoveAt(i);
                continue;
            }

            // Handle temporary (e.g. red) paint
            if (stroke.IsTemporary)
            {
                if (stroke.RemainingTime <= 0)
                {
                    Destroy(brush);
                    brushStrokes.RemoveAt(i);
                    continue;
                }
                else
                {
                    SpriteRenderer renderer = brush.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        Color color = renderer.color;
                        color.a = stroke.RemainingTime / stroke.Duration;
                        renderer.color = color;
                    }
                }
            }

            // Handle general expiration for all paint types
            else if (stroke.Lifetime > 0f && Time.time - stroke.CreationTime >= stroke.Lifetime)
            {
                Destroy(brush);
                brushStrokes.RemoveAt(i);
            }
        }
    }



    public GameObject ApplyPaint(Vector3 position)
    {
        GameObject newPlatform = Instantiate(platformPrefab, position, Quaternion.identity, platformsParent);
        newPlatform.name = $"{selectedColor}Platform";
        newPlatform.tag = "Paint";
        newPlatform.layer = LayerMask.NameToLayer("Platforms");
        newPlatform.transform.localScale = new Vector3(brushSize / 10f, brushHeight / 10f, 1f);

        PaintStroke stroke = newPlatform.AddComponent<PaintStroke>();
        stroke.Initialize(selectedColor, GetColorProperties(selectedColor));

        SpriteRenderer renderer = newPlatform.GetComponent<SpriteRenderer>();
        if (renderer != null && colorProperties.TryGetValue(selectedColor, out ColorProperty props))
        {
            renderer.color = props.paintColor;

            if (paintParticles != null)
            {
                paintParticles.transform.position = position;
                var main = paintParticles.main;
                main.startColor = props.paintColor;
                paintParticles.Play();
            }
        }

        BoxCollider2D collider = newPlatform.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = newPlatform.AddComponent<BoxCollider2D>();
        }

        CreatePaintParticles(position, selectedColor);
        brushStrokes.Add(newPlatform);
        CheckHiddenPathInteraction(position);
        OnPaintApplied?.Invoke(newPlatform);
        return newPlatform;
    }


    private void CreatePaintParticles(Vector3 position, string colorType)
    {
        int particleCount = GetParticleCountForColor(colorType);
        GameObject particleSystemObj = Instantiate(particlePrefab, position, Quaternion.identity);
        ParticleSystem particles = particleSystemObj.GetComponent<ParticleSystem>();

        if (particles != null && colorProperties.TryGetValue(colorType, out ColorProperty props))
        {
            var main = particles.main;
            main.startColor = props.paintColor;
            main.loop = false;

            // Ensure particles don’t live longer than expected
            main.startLifetime = paintParticleLifetime;

            switch (colorType)
            {
                case "red":
                    main.startLifetime = Mathf.Min(0.5f, paintParticleLifetime);
                    break;
                case "yellow":
                    var velocity = particles.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.space = ParticleSystemSimulationSpace.World;

                    var xCurve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(1f, 5f));
                    velocity.x = new ParticleSystem.MinMaxCurve(2.0f, xCurve);

                    var yCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
                    velocity.y = new ParticleSystem.MinMaxCurve(0f, yCurve);
                    break;
            }

            var emission = particles.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));
            particles.Play(); // ← force it to play
        }

        Destroy(particleSystemObj, paintParticleLifetime + 0.5f); // Give time for all particles to finish
    }


    private int GetParticleCountForColor(string colorType)
    {
        switch (colorType)
        {
            case "blue": return 12;
            case "red": return 8;
            case "yellow": return 10;
            case "purple": return 10;
            default: return 8;
        }
    }

    private ColorProperty GetColorProperties(string colorType)
    {
        if (colorProperties.TryGetValue(colorType, out ColorProperty property))
        {
            return property;
        }
        return new ColorProperty { name = "Default" };
    }

    private void CheckHiddenPathInteraction(Vector3 position)
    {
        for (int i = 0; i < hiddenPaths.Count; i++)
        {
            HiddenPath path = hiddenPaths[i];
            if (!path.revealed &&
                position.x >= path.position.x - 10 &&
                position.x <= path.position.x + path.size.x + 10 &&
                position.y >= path.position.y - 10 &&
                position.y <= path.position.y + path.size.y + 10)
            {
                hiddenPaths[i].revealed = true;
                GameObject pathPlatform = Instantiate(platformPrefab,
                    new Vector3(path.position.x + path.size.x / 2, path.position.y, 0f),
                    Quaternion.identity,
                    platformsParent);

                pathPlatform.name = "RevealedPath";
                pathPlatform.tag = "Platform";
                pathPlatform.layer = LayerMask.NameToLayer("Platforms");
                pathPlatform.transform.localScale = new Vector3(path.size.x / 10f, path.size.y / 10f, 1f);

                SpriteRenderer renderer = pathPlatform.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(0.4f, 0.6f, 1.0f);
                }

                GameObject revealEffect = new GameObject("RevealEffect");
                revealEffect.transform.position = new Vector3(path.position.x + path.size.x / 2, path.position.y, 0f);
                ParticleSystem particles = revealEffect.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.startColor = new Color(0.4f, 0.6f, 1.0f, 0.7f);
                main.startSize = 0.5f;
                main.startLifetime = 1.0f;
                var emission = particles.emission;
                emission.rateOverTime = 20;
                Destroy(revealEffect, 2.0f);

                OnPathRevealed?.Invoke(path);
                Debug.Log($"Hidden path revealed at ({path.position.x}, {path.position.y})");
                break;
            }
        }
    }

    public bool SetPaintColor(string colorName)
    {
        if (colorProperties.ContainsKey(colorName))
        {
            selectedColor = colorName;
            Debug.Log($"Paint color changed to {colorName}");
            return true;
        }
        Debug.LogWarning($"Invalid paint color: {colorName}");
        return false;
    }

    public List<GameObject> GetActiveBrushStrokes()
    {
        brushStrokes.RemoveAll(item => item == null);
        return brushStrokes;
    }

    public List<HiddenPath> GetRevealedPaths()
    {
        return hiddenPaths.FindAll(path => path.revealed);
    }

    public void ClearAllPaint()
    {
        foreach (var stroke in brushStrokes)
        {
            if (stroke != null)
            {
                Destroy(stroke);
            }
        }
        brushStrokes.Clear();
        Debug.Log("All paint cleared from the world");
    }
}


public class PaintStroke : MonoBehaviour
{
    public string PaintType { get; private set; }
    public float BounceFactor { get; private set; }
    public float SpeedBoost { get; private set; }
    public bool IsTemporary { get; private set; }
    public float Duration { get; private set; }
    public float RemainingTime { get; private set; }

    private float creationTime;
    private float lifetime = 30f;
    public float Lifetime => lifetime;
    public float CreationTime => creationTime;


    public void Initialize(string paintType, PaintManager.ColorProperty properties)
    {
        PaintType = paintType;
        creationTime = Time.time;

        if (properties != null)
        {
            lifetime = properties.lifetime;

            switch (paintType)
            {
                case "blue":
                    BounceFactor = properties.bounceFactor;
                    break;
                case "red":
                    IsTemporary = true;
                    Duration = properties.duration;
                    RemainingTime = Duration;
                    break;
                case "yellow":
                    SpeedBoost = properties.speedBoost;
                    break;
            }

            // Schedule destruction if lifetime is set
            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
            }
        }
    }

    private void Update()
    {
        if (IsTemporary)
        {
            RemainingTime = Duration - (Time.time - creationTime);
        }
    }
}


