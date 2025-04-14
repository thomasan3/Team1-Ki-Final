using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SFMTexture
{
    [RequireComponent(typeof(Image))]
    public class RandomColorFade : MonoBehaviour
    {
        private Image imageComponent;
        public float minWaitTime = 1.0f;
        public float maxWaitTime = 3.0f;
        public float fadeDuration = 2.0f;

        void Start()
        {
            imageComponent = GetComponent<Image>();
            StartCoroutine(ColorFadeCoroutine());
        }

        private IEnumerator ColorFadeCoroutine()
        {
            while (true)
            {
                Color newColor = GetBrightRandomColor();
                yield return StartCoroutine(FadeToColor(newColor));
                yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
            }
        }

        private IEnumerator FadeToColor(Color targetColor)
        {
            Color startColor = imageComponent.color;
            float time = 0;

            while (time < fadeDuration)
            {
                imageComponent.color = Color.Lerp(startColor, targetColor, time / fadeDuration);
                time += Time.deltaTime;
                yield return null;
            }

            imageComponent.color = targetColor;
        }
        private Color GetBrightRandomColor()
        {
            float r = Random.value;
            float g = Random.value;
            float b = Random.value;

            // 确保至少有一个颜色分量大于或等于0.8
            float maxColorComponent = Mathf.Max(Mathf.Max(r, g), b);
            if (maxColorComponent < 0.8f)
            {
                if (r == maxColorComponent)
                    r = 0.8f;
                else if (g == maxColorComponent)
                    g = 0.8f;
                else
                    b = 0.8f;
            }

            return new Color(r, g, b);
        }
    }
}