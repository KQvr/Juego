using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BlockMenuUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private Transform blockGrid;
    [SerializeField] private Button backButton;
    [SerializeField] private Button backToMainMenuButton;

    [Header("Escena Principal")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Prefab de boton")]
    [SerializeField] private GameObject blockButtonPrefab;

    [Header("Colores")]
    [SerializeField] private Color unlockedColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.85f, 0.4f);

    private readonly List<GameObject> blockButtons = new();

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(ShowMenu);

        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(GoToMainMenu);

        if (BlockManager.Instance != null)
        {
            BlockManager.Instance.OnBlockStarsChanged += OnStarsChanged;
            BlockManager.Instance.OnBlockUnlocked += OnBlockUnlocked;
        }

        BuildButtons();
        ShowMenu();
    }

    void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(ShowMenu);

        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.RemoveListener(GoToMainMenu);

        if (BlockManager.Instance != null)
        {
            BlockManager.Instance.OnBlockStarsChanged -= OnStarsChanged;
            BlockManager.Instance.OnBlockUnlocked -= OnBlockUnlocked;
        }
    }

    // -----------------------------------------------------------------------
    // Construccion de botones
    // -----------------------------------------------------------------------

    private void BuildButtons()
    {
        if (blockButtonPrefab == null)
        {
            Debug.LogWarning("[BlockMenuUI] Falta blockButtonPrefab.");
            return;
        }
        if (blockGrid == null)
        {
            Debug.LogWarning("[BlockMenuUI] Falta blockGrid.");
            return;
        }

        foreach (var btn in blockButtons)
            if (btn != null) Destroy(btn);
        blockButtons.Clear();

        var blocks = BlockManager.Instance?.GetBlocks();
        if (blocks == null || blocks.Count == 0)
        {
            Debug.LogWarning("[BlockMenuUI] BlockManager no tiene bloques asignados.");
            return;
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            int index = i;
            var go = Instantiate(blockButtonPrefab, blockGrid);
            var btn = go.GetComponent<Button>();

            if (btn != null)
                btn.onClick.AddListener(() => EnterBlock(index));

            blockButtons.Add(go);
            RefreshButton(go, blocks[i], i);
        }
    }

    private void RefreshButton(GameObject go, BlockContentSO block, int index)
    {
        if (go == null || block == null) return;

        bool unlocked = BlockManager.Instance?.IsUnlocked(block.blockId) ?? false;
        int stars = BlockManager.Instance?.GetStars(block.blockId) ?? 0;

        var ui = go.GetComponent<BlockButtonUI>();
        if (ui != null)
        {
            if (ui.nameText != null) ui.nameText.text = $"Bloque {index + 1}\n{block.blockName}";
            if (ui.starsText != null) ui.starsText.text = BuildStars(stars);
            if (ui.descText != null) ui.descText.text = unlocked ? block.description : "Bloqueado";
            if (ui.lockOverlay != null) ui.lockOverlay.SetActive(!unlocked);
        }

        Color targetColor = stars == 3 ? completedColor :
                            unlocked ? unlockedColor : lockedColor;

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
            btn.interactable = unlocked;
        }

        var img = go.GetComponent<Image>();
        if (img != null) img.color = targetColor;
    }

    private string BuildStars(int stars) => stars switch
    {
        1 => "★☆☆",
        2 => "★★☆",
        3 => "★★★",
        _ => "☆☆☆"
    };

    // -----------------------------------------------------------------------
    // Navegacion
    // -----------------------------------------------------------------------

    private void EnterBlock(int index)
    {
        BlockManager.Instance?.ShowBlock(index);
        HideMenu();
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ShowMenu()
    {
        if (menuCanvas != null) menuCanvas.SetActive(true);
        if (backButton != null) backButton.gameObject.SetActive(false);

        BlockManager.Instance?.HideAllActivities();
        RefreshAllButtons();
    }

    public void HideMenu()
    {
        if (menuCanvas != null) menuCanvas.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    private void RefreshAllButtons()
    {
        var blocks = BlockManager.Instance?.GetBlocks();
        if (blocks == null) return;

        for (int i = 0; i < blockButtons.Count && i < blocks.Count; i++)
            RefreshButton(blockButtons[i], blocks[i], i);
    }

    private void OnStarsChanged(string blockId, int stars) => RefreshAllButtons();
    private void OnBlockUnlocked(string blockId) => RefreshAllButtons();
}