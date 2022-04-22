using System.Linq;
using UnityEngine;

namespace OdinsTraps
{
    public class SE_Trapped : SE_Stats
    {
        public void OnEnable()
        {
            m_name = "Trapped!";
            m_icon = OdinsTraps.UnplacedMetalTrap?.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons.First();
            m_ttl = 10;
            m_speedModifier = -1;           
        }
        
    }
}