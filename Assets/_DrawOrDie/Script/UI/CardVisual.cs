using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 카드 카테고리별 프레임 색상 + 중앙 아이콘 적용 (손패/조합 미리보기/덱 뷰어 카드 공용)
public class CardVisual : MonoBehaviour
{
    public CardCategory category;
    public Image frameImage; // 카테고리 색상 프레임 (아이콘 위에 그려짐)
    public Image iconImage;  // 프레임 창 뒤에 보이는 중앙 아이콘
    public TMP_Text nameText; // 배너 색에 맞춰 글자색 변경
    public CardFrameSet frameSet;
    public CardIconDatabase iconDatabase;

    public void Apply(ICard card)
    {
        Apply(card.Category, iconDatabase != null ? iconDatabase.GetIcon(card) : null);
    }

    public void Apply(Combination combo)
    {
        Apply(CardCategory.Combination, iconDatabase != null ? iconDatabase.GetCombinationIcon(combo.combinationId) : null);
    }

    // 아이콘이 없으면 창이 막힌 프레임 사용
    private void Apply(CardCategory newCategory, Sprite icon)
    {
        category = newCategory;
        bool hasIcon = icon != null;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = hasIcon;
        }

        CardFrameSet.Frame frame = frameSet != null ? frameSet.Get(newCategory) : null;
        if (frame == null) return;

        if (frameImage != null) frameImage.sprite = hasIcon ? frame.windowFrame : frame.baseFrame;
        if (nameText != null) nameText.color = frame.nameColor;
    }
}
