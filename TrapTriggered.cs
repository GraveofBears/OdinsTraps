using UnityEngine;

namespace OdinsTraps
{
	public class TrapTriggered : MonoBehaviour
	{
		private void Awake()
		{
			if (GetComponentInParent<ZNetView>()?.GetZDO() == null)
			{
				Destroy(this);
			}
		}
		
		private void OnTriggerEnter(Collider other)
		{
			if (other.GetComponent<Player>() == Player.m_localPlayer)
			{
				Player.m_localPlayer.GetSEMan().AddStatusEffect("Trapped");
			}

        }
    }
}
	

