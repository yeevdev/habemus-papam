using UnityEngine;

public enum ItemGrade { Common, Rare }
public enum ItemExpirationType { Conclave, Day, Permanent, Special }

public abstract class Item : ScriptableObject
{
    public string itemID;
    public string itemName;
    public ItemGrade itemGrade;
    public ItemExpirationType itemExpirationType;

    // 공통
    public virtual void OnAcquire() { }
    public virtual void OnRemove() { }

    // 사용
    public virtual void OnUse() { }

}
