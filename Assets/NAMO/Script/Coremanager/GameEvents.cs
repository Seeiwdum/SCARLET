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
}