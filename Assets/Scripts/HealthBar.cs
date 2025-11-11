using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private float timeToDrain = 0.25f;
    [SerializeField] private Gradient healthBarGradient;

    [Header("Optional Manual Assignments")]
    [SerializeField] private Image healthBarImage;
    [SerializeField] private TextMeshProUGUI healthText;

    private float target = 1f;
    private Coroutine drainHealthBarCoroutine;

    void Start()
    {
        if (healthBarImage == null)
            healthBarImage = transform.Find("Health_Bar_Image")?.GetComponent<Image>();

        if (healthText == null)
            healthText = transform.Find("Health_Bar_Percentage")?.GetComponent<TextMeshProUGUI>();

        if (healthBarImage == null)
        {
            Debug.LogError("HealthBar: No Image component found! No Bueno");
            return;
        }

        if (healthText == null)
        {
            Debug.LogError("HealthBar: No TMP text found! Almost");
            return;
        }

        target = 1f;
        healthBarImage.fillAmount = 1f;
        healthBarImage.color = healthBarGradient.Evaluate(target);
        healthText.text = "100%";
    }

    public void UpdateHealthBar(float maxHealth, float currentHealth)
    {
        if (healthBarImage == null || healthText == null)
        {
            Debug.LogError("HealthBar: Missing Stuffo! Check if Health_Bar_Image and Health_Bar_Percentage are assigned.");
            return;
        }

        target = currentHealth / maxHealth;

        if (drainHealthBarCoroutine != null)
            StopCoroutine(drainHealthBarCoroutine);

        drainHealthBarCoroutine = StartCoroutine(DrainHealthBar(maxHealth, currentHealth));
    }

    private IEnumerator DrainHealthBar(float maxHealth, float currentHealth)
    {
        float fillAmount = healthBarImage.fillAmount;
        Color startColor = healthBarImage.color;
        Color targetColor = healthBarGradient.Evaluate(target);

        float startHealth = fillAmount * maxHealth;
        float elapsedTime = 0f;

        while (elapsedTime < timeToDrain)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / timeToDrain);

            // smoothhhhhh
            healthBarImage.fillAmount = Mathf.Lerp(fillAmount, target, t);
            healthBarImage.color = Color.Lerp(startColor, targetColor, t);

            float lerpedHealth = Mathf.Lerp(startHealth, currentHealth, t);
            healthText.text = $"HP {Mathf.RoundToInt(lerpedHealth)}/{Mathf.RoundToInt(maxHealth)}";

            yield return null;
        }

        // finalize
        healthBarImage.fillAmount = target;
        healthBarImage.color = targetColor;
        healthText.text = $"HP {Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
    }
}