using TMPro;
using UnityEngine;

//Client-side visual for a customer, instantiated from the Customer prefab by CustomerManager.
//GDD section 23: the hungrier they get, the more they hunch, shake and look like zombies.
public class CustomerVisual : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static readonly Color[] MoodColors =
    {
        new Color(0.30f, 0.55f, 0.85f), //Normal: office shirt
        new Color(0.75f, 0.75f, 0.35f), //Faminto
        new Color(0.90f, 0.50f, 0.20f), //Desesperado
        new Color(0.85f, 0.15f, 0.15f), //Furioso
        new Color(0.45f, 0.10f, 0.40f), //Colapso
    };
    private static readonly string[] MoodLabels = { "Normal", "Faminto", "DESESPERADO", "FURIOSO!", "COLAPSO!!" };
    private static readonly float[] MoodShake = { 0f, 0.01f, 0.03f, 0.06f, 0.1f };
    private static readonly float[] MoodLean = { 0f, 6f, 14f, 22f, 30f };

    [SerializeField] private Transform body;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Renderer[] eyeRenderers;
    [SerializeField] private TextMeshPro overheadText;
    private MaterialPropertyBlock propertyBlock;

    private CustomerData data;
    private string recipeName;
    private Vector3 targetPosition;
    private string popupText;
    private float popupTimer;
    private float punchTimer;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    public void SetData(CustomerData data, string recipeName)
    {
        this.data = data;
        this.recipeName = recipeName;
        targetPosition = data.position;
    }

    public void PlayReaction(CustomerManager.Reaction reaction)
    {
        switch (reaction)
        {
            case CustomerManager.Reaction.Fed:
                ShowPopup("<color=#7CFC00>Obrigado! :)</color>", 2.5f);
                break;
            case CustomerManager.Reaction.FedByGun:
                ShowPopup("<color=#FFD700>nham... obrigado?</color>", 2f);
                punchTimer = 0.25f;
                break;
            case CustomerManager.Reaction.WrongOrder:
                ShowPopup("<color=#FF5050>NÃO FOI ISSO QUE EU PEDI!</color>", 1.5f);
                break;
            case CustomerManager.Reaction.Hit:
                ShowPopup(data.Phase == CustomerPhase.Leaving ? "Ei! Eu já comi!" : "AI! (nham)", 0.8f);
                punchTimer = 0.25f;
                break;
        }
    }

    private void ShowPopup(string text, float duration)
    {
        popupText = text;
        popupTimer = duration;
    }

    private void Update()
    {
        Vector3 previousPosition = transform.position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-12f * Time.deltaTime));

        Vector3 moveDirection = transform.position - previousPosition;
        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude > 0.00001f && !data.stunned)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDirection.normalized, Time.deltaTime * 10f);
        }

        //Fed customers instantly regain their composure
        int moodIndex = data.Phase == CustomerPhase.Leaving ? 0 : data.mood;

        float shake = MoodShake[moodIndex];
        float punch = punchTimer > 0f ? 1f + punchTimer : 1f;
        punchTimer -= Time.deltaTime;
        body.localPosition = new Vector3(Random.Range(-shake, shake), 1f, Random.Range(-shake, shake));
        body.localRotation = Quaternion.Euler(data.stunned ? -25f : MoodLean[moodIndex], 0f, 0f);
        body.localScale = new Vector3(0.8f * punch, 1f, 0.8f * punch);

        propertyBlock.SetColor(BaseColorId, MoodColors[moodIndex]);
        bodyRenderer.SetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, moodIndex >= (int)CustomerMood.Furioso ? Color.red : Color.black);
        foreach (Renderer eyeRenderer in eyeRenderers)
        {
            eyeRenderer.SetPropertyBlock(propertyBlock);
        }

        UpdateOverheadText(moodIndex);
    }

    private void UpdateOverheadText(int moodIndex)
    {
        popupTimer -= Time.deltaTime;
        if (popupTimer > 0f)
        {
            overheadText.text = popupText;
        }
        else if (data.Phase == CustomerPhase.Leaving)
        {
            overheadText.text = "";
        }
        else
        {
            int filled = Mathf.RoundToInt(data.rage / (float)CustomerManager.RAGE_MAX * 10f);
            string colorHex = ColorUtility.ToHtmlStringRGB(MoodColors[moodIndex]);
            overheadText.text = $"<b>{recipeName}</b>\n<color=#{colorHex}>{MoodLabels[moodIndex]}\n{new string('|', filled)}<alpha=#44>{new string('|', 10 - filled)}</color>";
        }
    }

    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            overheadText.transform.rotation = mainCamera.transform.rotation;
        }
    }
}
