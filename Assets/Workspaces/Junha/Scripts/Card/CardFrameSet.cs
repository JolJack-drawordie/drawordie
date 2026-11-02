using System;
using System.Collections.Generic;
using UnityEngine;

// 카드 카테고리별 프레임 스프라이트 묶음 (PixelCardUI 에셋)
[CreateAssetMenu(fileName = "CardFrameSet", menuName = "DrawOrDie/Card Frame Set")]
public class CardFrameSet : ScriptableObject
{
    [Serializable]
    public class Frame
    {
        public string name; // 인스펙터 목록에 보이는 이름
        public CardCategory category;
        public Sprite baseFrame; // 중앙 아이콘이 없을 때 (창이 막힌 프레임)
        public Sprite windowFrame; // 중앙 아이콘이 있을 때 (창이 뚫린 프레임)
        public Color nameColor = Color.white; // 이름 배너 위 글자색
    }

    public List<Frame> frames = new List<Frame>();

    // 해당 카테고리 프레임이 없으면 Basic 프레임으로 대신한다
    public Frame Get(CardCategory category)
    {
        Frame frame = frames.Find(f => f.category == category);
        return frame ?? frames.Find(f => f.category == CardCategory.Basic);
    }
}
