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

        if (healthText == null)
        {
            Debug.LogError("HealthBar: No TMP text found");
            return;
        }

        // Start full health
        target = 1f;

        // Gradient color for max HP
        Color c = healthBarGradient.Evaluate(target);

        healthText.color = c;

        // (DISABLED) Bar visuals
        // if (healthBarImage != null)
        // {
        //     healthBarImage.fillAmount = 1f;
        //     healthBarImage.color = c;
        // }

        healthText.text = "HP 100/100";
    }

    public void UpdateHealthBar(float maxHealth, float currentHealth)
    {
        if (healthText == null)
        {
            Debug.LogError("HealthBar: Missing TMP text.");
            return;
        }

        target = currentHealth / maxHealth;

        if (drainHealthBarCoroutine != null)
            StopCoroutine(drainHealthBarCoroutine);

        drainHealthBarCoroutine = StartCoroutine(DrainHealthBar(maxHealth, currentHealth));
    }

    private IEnumerator DrainHealthBar(float maxHealth, float currentHealth)
    {
        float elapsedTime = 0f;

        // Starting colors
        Color startTextColor = healthText.color;
        Color targetColor = healthBarGradient.Evaluate(target);

        // Starting health number (from text or assume full)
        float startHealth = Mathf.RoundToInt(currentHealth);

        while (elapsedTime < timeToDrain)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / timeToDrain);

            // (DISABLED) Bar visual updates
            // if (healthBarImage != null)
            // {
            //     healthBarImage.fillAmount = Mathf.Lerp(startFill, target, t);
            //     healthBarImage.color = Color.Lerp(startColor, targetColor, t);
            // }

            // HP TEXT COLOR LERP
            healthText.color = Color.Lerp(startTextColor, targetColor, t);

            float lerpedHealth = Mathf.Lerp(startHealth, currentHealth, t);
            healthText.text = $"HP {Mathf.RoundToInt(lerpedHealth)}/{Mathf.RoundToInt(maxHealth)}";

            yield return null;
        }

        // Final values
        healthText.color = targetColor;
        healthText.text = $"HP {Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
    }
}
