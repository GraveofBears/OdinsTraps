using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OdinsTraps
{
	public class TrapController : MonoBehaviour, Hoverable, Interactable
	{
		public string m_name = "Trap Control";
		public float m_radius = 10f;
		public CircleProjector m_areaMarker;
		public EffectList m_activateEffect = new EffectList();
		public EffectList m_deactivateEffect = new EffectList();
		public GameObject m_connectEffect;
		public GameObject m_inRangeEffect;
		public ZNetView m_nview;

		public static List<TrapController> allTrapControllers = new List<TrapController>();
		public string turnOffMessage = "";
		public string turnOnMessage = "";
		public GameObject m_switchOn;
		public GameObject m_switchOff;

		public bool IsEnabled = false;

		private void Awake()
		{
			if (m_areaMarker)
			{
				m_areaMarker.m_radius = m_radius;
			}

			m_nview = GetComponent<ZNetView>();
			if (!m_nview.IsValid())
			{
				return;
			}

			if (m_areaMarker)
			{
				m_areaMarker.gameObject.SetActive(false);
			}

			if (m_inRangeEffect)
			{
				m_inRangeEffect.SetActive(false);
			}
			
			m_nview.Register("ToggleTrapControllerEnabled", RPC_ToggleTrapControllerEnabled);

			allTrapControllers.Add(this);
			foreach (TrapEnabler trap in TrapEnabler.allTraps.Where(trap => Utils.DistanceXZ(trap.transform.position, transform.position) <= m_radius))
			{
				trap.nearByControllers.Add(this);
			}

			IsEnabled = IsEnabledZDO();
		}

		public void Update()
		{
			bool isManuallyDisabled = IsManuallyDisabled();
			m_switchOn.SetActive(isManuallyDisabled);
			m_switchOff.SetActive(!isManuallyDisabled);

			if (m_nview.IsValid())
			{
				IsEnabled = Physics.OverlapSphereNonAlloc(transform.position, m_radius, new Collider[1], LayerMask.GetMask("character")) != 0;
				if (IsEnabled != IsEnabledZDO() && m_nview.IsOwner())
				{
					ToggleEnabled(() =>
					{
						SetEnabled(IsEnabled);
						SetManuallyDisabled(false);
					});
				}
			}
		}

		private void OnDestroy()
		{
			allTrapControllers.Remove(this);
			foreach (TrapEnabler trapEnabler in TrapEnabler.allTraps)
			{
				trapEnabler.nearByControllers.Remove(this);
			}
		}

		public string GetHoverText()
		{
			if (!PrivateArea.CheckAccess(transform.position, flash: false))
			{
				return Localization.instance.Localize(m_name + "\n$piece_noaccess");
			}
			
			return Localization.instance.Localize(m_name + $"\n[<color=yellow><b>$KEY_Use</b></color>] {(IsManuallyDisabled() ? "$piece_guardstone_activate" : "$piece_guardstone_deactivate")}");
		}

		public string GetHoverName() => m_name;

		private void RPC_ToggleTrapControllerEnabled(long sender)
		{
			if (!m_nview.IsOwner())
			{
				return;
			}

			SetManuallyDisabled(!IsManuallyDisabled());
		}

		public bool IsActive() => IsEnabled && !IsManuallyDisabled();
		public bool IsActiveZDO() => IsEnabledZDO() && !IsManuallyDisabled();
		public bool IsManuallyDisabled() => m_nview.IsValid() && m_nview.GetZDO().GetBool("manually_disabled");
		private bool IsEnabledZDO() => m_nview.IsValid() && m_nview.GetZDO().GetBool("enabled");
		private void SetEnabled(bool enabled) => ToggleEnabled(() => m_nview.GetZDO().Set(nameof(enabled), enabled));
		private void SetManuallyDisabled(bool disabled) => ToggleEnabled(() => m_nview.GetZDO().Set("manually_disabled", disabled));
		
		private void ToggleEnabled(Action toggleEnabled)
		{
			bool active = IsActiveZDO();
			toggleEnabled();
			if (active != IsActiveZDO())
			{
				(active ? m_deactivateEffect : m_activateEffect).Create(transform.position, transform.rotation);
			}
		}
		
		public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

		public bool Interact(Humanoid character, bool repeat, bool alt)
		{
			if (repeat || !PrivateArea.CheckAccess(transform.position))
			{
				return true;
			}

			if (turnOffMessage != "" && turnOnMessage != "")
			{
				character.Message(MessageHud.MessageType.Center, IsManuallyDisabled() ? turnOnMessage : turnOffMessage);
			}
			else
			{
				character.Message(MessageHud.MessageType.Center, m_name + " was " + (IsManuallyDisabled() ? "turned on" : "turned off"));
			}

			m_nview.InvokeRPC("ToggleTrapControllerEnabled");
			
			return true;
		}

		private void HideMarker() => m_areaMarker.gameObject.SetActive(false);

		public void ShowAreaMarker()
		{
			if (!(bool)(Object)m_areaMarker)
			{
				return;
			}
			m_areaMarker.gameObject.SetActive(true);
			CancelInvoke(nameof(HideMarker));
			Invoke(nameof(HideMarker), 0.5f);
		}
	}
}
