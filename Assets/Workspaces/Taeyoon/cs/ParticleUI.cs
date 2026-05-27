using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ParticleUI : MonoBehaviour
{
    public int particleCount = 60;
    public float minSize = 2f;
    public float maxSize = 6f;

    void Start()
    {
        for (int i = 0; i < particleCount; i++)
        {
            CreateParticle();
        }
    }

    void CreateParticle()
    {
        GameObject obj = new GameObject("Particle");
        obj.transform.SetParent(transform, false);

        Image img = obj.AddComponent<Image>();
        RectTransform rect = obj.GetComponent<RectTransform>();

        float size = Random.Range(minSize, maxSize);
        rect.sizeDelta = new Vector2(size, size);

        float delay = Random.Range(0f, 5f);
        img.color = new Color(0.6f, 0.6f, 0.7f, 0f);

        DOVirtual.DelayedCall(delay, () => AnimateParticle(rect, img));
    }

    void AnimateParticle(RectTransform rect, Image img)
    {
        rect.DOKill();
        img.DOKill();

        float startX = Random.Range(-960f, 960f);
        float startY = Random.Range(-540f, 200f);
        float targetY = startY + Random.Range(200f, 500f);
        float duration = Random.Range(5f, 10f);
        float alpha = Random.Range(0.2f, 0.5f);

        rect.anchoredPosition = new Vector2(startX, startY);
        img.color = new Color(0.6f, 0.6f, 0.7f, alpha);

        rect.DOAnchorPosY(targetY, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => AnimateParticle(rect, img));

        img.DOFade(0f, duration)
            .SetEase(Ease.InQuad);
    }
}
