using UnityEngine;

public interface IParryable
{
    /// <summary>
    /// เรียกเมื่อผู้เล่น Parry การโจมตีสำเร็จ
    /// </summary>
    /// <param name="parrySourcePosition">ตำแหน่งของผู้เล่นที่ทำการ Parry (เพื่อคำนวณทิศทางกระเด็น)</param>
    void OnParrySuccess(Vector3 parrySourcePosition);
}