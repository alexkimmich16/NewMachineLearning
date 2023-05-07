using UnityEngine;
using System;
public class Save : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //JsonUtility.ToJson(SlotsHolder);
        //RecieveInfo info = JsonUtility.FromJson<RecieveInfo>(JSONText);
    }

    [Serializable]
    public struct RecieveInfo
    {
        public string _id;
        public string walletAddress;
        //public List<SlotInfo> nodes;
        public string updatedAt;
        public int itemsToday;
        public string playerName;
    }
}
