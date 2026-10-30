using UnityEngine;

[CreateAssetMenu(fileName = "NewText", menuName = "Du Lieu/Text Don Gian")]
public class TextData : ScriptableObject
{
    // [TextArea] giúp ô nhập chữ trong Inspector to ra để dễ gõ nhiều dòng
    [TextArea]
    public string noiDung; 
}