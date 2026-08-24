using System;
using UnityEngine;

/// <summary>
/// ศูนย์กลาง Event Bus สำหรับให้ระบบต่างๆ ส่งและรับสัญญาณหากัน (Observer Pattern)
/// </summary>
public static class GameEvents
{
    // ==========================================
    // [ Player Events ] สัญญาณเกี่ยวกับตัวละครผู้เล่น
    // ==========================================
    // ส่งค่า (เลือดปัจจุบัน, เลือดสูงสุด) เพื่อให้ UI อัปเดต
    public static Action<int, int> OnPlayerHealthChanged; 
    
    // ส่งค่า (เกจไฟปัจจุบัน, เกจสูงสุด) เพื่อให้ UI อัปเดต
    public static Action<float, float> OnFlameEnergyChanged; 
    
    public static Action OnPlayerDied;

    // ส่งค่าบูลีนแจ้งเตือนว่าผู้เล่นกำลังสวมหรือถอดฮู้ดอยู่ (true = สวม, false = ถอด)
    public static Action<bool> OnHoodToggled;

    // ==========================================
    // [ Combat Events ] สัญญาณเกี่ยวกับการต่อสู้
    // ==========================================
    // เรียกเมื่อฟันโดนศัตรู (เอาไว้สั่งกล้องสั่น หรือทำ Hitstop หยุดเฟรม)
    public static Action OnEnemyHit; 

    // ==========================================
    // [ Skill Events ] สัญญาณเกี่ยวกับสกิล
    // ==========================================
    // เรียกเมื่อวาร์ปไปหาดาบ หรือ เหยียบดาบเด้งตัวสำเร็จ
    public static Action OnSwordVaultPerformed; 

    // ==========================================
    // [ Quest & Dialogue Events ] สัญญาณเควสต์และบทสนทนา
    // ==========================================
    // ส่งค่า (หัวใจเพลิงที่เก็บได้, หัวใจเพลิงทั้งหมด) เพื่อให้อัปเดต UI และประภาคาร
    public static System.Action<int, int> OnFlameHeartCollected;
    
    // ส่งสัญญาณเมื่อเริ่มและจบบทสนทนา (เพื่อให้ PlayerController หยุดเดินชั่วคราว)
    public static System.Action OnDialogueStarted;
    public static System.Action OnDialogueEnded;
}