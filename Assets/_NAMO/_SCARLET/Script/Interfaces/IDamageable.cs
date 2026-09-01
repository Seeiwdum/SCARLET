using UnityEngine;

/// <summary>
/// Interface สำหรับทุกสิ่งในเกมที่สามารถถูกโจมตีและรับความเสียหายได้
/// </summary>
public interface IDamageable
{
    // บังคับว่าใครก็ตามที่มี IDamageable ต้องมีฟังก์ชันนี้
    void TakeDamage(int damage, Vector3 sourcePosition);
}