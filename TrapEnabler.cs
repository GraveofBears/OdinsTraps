using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OdinsTraps
{
	public class TrapEnabler : MonoBehaviour, Hoverable
	{
		public string m_name = "";
		public EffectList m_activateEffect = new EffectList();
		public EffectList m_deactivateEffect = new EffectList();
		public ZNetView m_nview;
		public GameObject m_enabledObject;

		public List<TrapController> nearByControllers = new();
		public static List<TrapEnabler> allTraps = new();

		private void Awake()
		{
			m_nview = GetComponent<ZNetView>();
			if (!m_nview.IsValid())
			{
				return;
			}

			if (m_name == "")
			{
				m_name = GetComponent<Piece>().m_name;
			}

			allTraps.Add(this);
			nearByControllers.AddRange(TrapController.allTrapControllers.Where(c => Utils.DistanceXZ(c.transform.position, transform.position) <= c.m_radius));
		}
		
		private void OnDestroy() => allTraps.Remove(this);

		public void Update()
		{
			bool wasEnabled = IsEnabled();
			if (wasEnabled != HasEnabledController() && m_nview.IsValid())
			{
				SetEnabled(!IsEnabled());
			}

			if (m_enabledObject)
			{
				m_enabledObject.SetActive(IsEnabled());
			}
			else
			{
				Debug.LogWarning("No object to Toggle");
			}
		}

		public string GetHoverText() => Localization.instance.Localize(m_name + $" ({(IsEnabled() ? "$piece_guardstone_active" : "$piece_guardstone_inactive")})");

		public string GetHoverName() => m_name;

		private bool HasEnabledController() => nearByControllers.Count == 0 || nearByControllers.Any(c => c.IsActive());

		private bool IsEnabled() => m_nview.IsValid() && m_nview.GetZDO().GetBool("enabled");
		
		private void SetEnabled(bool enabled)
		{
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(nameof(enabled), enabled);
				(enabled ? m_activateEffect : m_deactivateEffect).Create(transform.position, transform.rotation);
			}
		}
	}
}
