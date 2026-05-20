using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeSO", menuName = "Scriptable Objects/RecipeSO")]
[System.Serializable]
public class RecipeData : ScriptableObject
{
    public string recipeID;
    public int adjCardID;
    public int nounCardID;
    public CardSO resultCard;
}
