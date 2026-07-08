using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ActivityActionHUDController : MonoBehaviour {
    [Header("Root")]
    [Tooltip("Root object enabled while an activity action is shown. Empty uses this GameObject.")]
    [SerializeField] GameObject root;
    [Tooltip("Optional toast root shown after an activity completes.")]
    [SerializeField] GameObject resultRoot;

    [Header("Text")]
    [Tooltip("Text showing the running action title.")]
    [SerializeField] Text actionTitleText;
    [Tooltip("Text showing the current action state.")]
    [SerializeField] Text actionStateText;
    [Tooltip("Text showing progress percent.")]
    [SerializeField] Text progressText;
    [Tooltip("Text showing tool usage.")]
    [SerializeField] Text toolText;
    [Tooltip("Text showing stamina or need cost.")]
    [SerializeField] Text staminaText;
    [Tooltip("Text showing companion/partner help.")]
    [SerializeField] Text partnerText;
    [Tooltip("Text shown in the completion toast.")]
    [SerializeField] Text resultTitleText;
    [Tooltip("Body text shown in the completion toast.")]
    [SerializeField] Text resultBodyText;

    [Header("Progress")]
    [Tooltip("Optional Image filled from 0 to 1 while the action runs.")]
    [SerializeField] Image progressFill;
    [Tooltip("If enabled, this HUD is hidden on Awake.")]
    [SerializeField] bool hideOnAwake = true;

    Coroutine resultRoutine;

    void Awake() {
        if(root == null) {
            root = gameObject;
        }

        if(hideOnAwake) {
            Hide();
        }
    }

    public void Show(string title, string state, string tool, string stamina, string partner) {
        if(root != null) {
            root.SetActive(true);
        }
        if(resultRoot != null) {
            resultRoot.SetActive(false);
        }

        SetText(actionTitleText, title);
        SetText(actionStateText, state);
        SetText(toolText, tool);
        SetText(staminaText, stamina);
        SetText(partnerText, partner);
        SetProgress(0f);
    }

    public void SetProgress(float normalizedProgress) {
        float progress = Mathf.Clamp01(normalizedProgress);
        if(progressFill != null) {
            progressFill.fillAmount = progress;
        }
        SetText(progressText, $"{Mathf.RoundToInt(progress * 100f)}%");
    }

    public void ShowResult(string title, string body, float visibleSeconds = 1.5f) {
        if(resultRoot == null) {
            return;
        }

        SetText(resultTitleText, title);
        SetText(resultBodyText, body);
        resultRoot.SetActive(true);

        if(resultRoutine != null) {
            StopCoroutine(resultRoutine);
        }
        if(visibleSeconds > 0f) {
            resultRoutine = StartCoroutine(HideResultAfterDelay(visibleSeconds));
        }
    }

    public void Hide() {
        if(root != null) {
            root.SetActive(false);
        }
        if(resultRoot != null) {
            resultRoot.SetActive(false);
        }
    }

    IEnumerator HideResultAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        if(resultRoot != null) {
            resultRoot.SetActive(false);
        }
        resultRoutine = null;
    }

    static void SetText(Text text, string value) {
        if(text != null) {
            text.text = value ?? string.Empty;
        }
    }
}
