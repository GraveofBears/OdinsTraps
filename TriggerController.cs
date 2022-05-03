using UnityEngine;

namespace OdinsTraps
public class TriggerController : MonoBehaviour
{
    public GameObject toEnable;

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

        if (toEnable)
        {
            toEnable.SetActive(true);
        }
    }
}