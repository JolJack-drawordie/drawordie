using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardSO : ScriptableObject
{
    [Header("카드 정보")]
    public int cardID;
    public string cardName;
    public CardCategory category; // 형용사, 동명사, 결과 카드

    [Header("특수 효과 리스트")]
    public List<EffectData> effects = new List<EffectData>();

    [Header("이미지")]
    public Sprite cardIcon;

    [Header("능력치")]
    public int power;
    public string description;
    public bool isAlltarget = false;
}
