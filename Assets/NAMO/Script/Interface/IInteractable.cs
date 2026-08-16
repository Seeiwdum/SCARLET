using UnityEngine;
/// <summary>
/// Interface สำหรับวัตถุที่ผู้เล่นสามารถกดปุ่มเพื่อ Interact (สำรวจ/พูดคุย/ใช้งาน) ได้
/// </summary>
public interface IInteractable
{
    // ฟังก์ชันที่จะทำงานเมื่อผู้เล่นกดปุ่มสำรวจ
    void Interact();
}