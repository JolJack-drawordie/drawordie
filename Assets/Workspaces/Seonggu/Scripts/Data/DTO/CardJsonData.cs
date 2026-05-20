using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CardJsonData
{
    [Header("카드 정보")]
    public int cardID;
    public string cardName;
    public string category; // 형용사, 동명사, 결과 카드
    

    [Header("특수 효과 리스트")]
    public List<EffectJsonData> effects = new List<EffectJsonData>();

    [Header("이미지")]
    public Sprite cardIcon;

    [Header("능력치")]
    public int power;
    public string description;
    public bool isAlltarget = false;
}
