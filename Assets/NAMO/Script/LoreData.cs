using UnityEngine;

[CreateAssetMenu(fileName = "New Lore Data", menuName = "SCARLET/Lore Data")]
public class LoreData : ScriptableObject
{
    public string loreID;
    public string title;
    
    [TextArea(5, 10)]
    public string content;
    
    public Sprite illustration; // (ทางเลือก) รูปภาพประกอบบันทึก
}