using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OdinsTrapTrigger
{
    public class ObjectEnabled : MonoBehaviour, Hoverable, Interactable
    {
        public string m_name = "Trap Trigger";
        public EffectList m_activateEffect = new EffectList();
        public EffectList m_deactivateEffect = new EffectList();
        private ZNetView m_nview;
        private Piece m_piece;
        public GameObject m_enabledObject;

        private void Awake()
        {
            this.m_nview = this.GetComponent<ZNetView>();
            if (!this.m_nview.IsValid())
                return;
            this.GetComponent<WearNTear>().m_onDamaged += new Action(this.OnDamaged);
            this.m_piece = this.GetComponent<Piece>();
            this.InvokeRepeating("UpdateStatus", 0.0f, 1f);
            this.m_nview.Register<long>("ToggleEnabled", new Action<long, long>(this.RPC_ToggleEnabled));
        }

        private void UpdateStatus()
        {
            bool flag = this.IsEnabled();
            m_enabledObject.SetActive(flag);
        }


        public string GetHoverText()
        {
            if (!PrivateArea.CheckAccess(this.transform.position, flash: false))
                return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");

            if (this.IsEnabled())
            {
                return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] Disable");
            }
            else
            {
                return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] Enable");
            }
        }

        public string GetHoverName() => this.m_name;

        private void RPC_ToggleEnabled(long uid, long playerID)
        {
            ZLog.Log((object)("Toggle enabled from " + (object)playerID + "  creator is " + (object)this.m_piece.GetCreator()));
            if (!this.m_nview.IsOwner() || this.m_piece.GetCreator() != playerID)
                return;
            this.SetEnabled(!this.IsEnabled());
        }

        public bool IsEnabled() => this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("enabled");

        private void SetEnabled(bool enabled)
        {
            this.m_nview.GetZDO().Set(nameof(enabled), enabled);
            this.UpdateStatus();
            if (enabled)
                this.m_activateEffect.Create(this.transform.position, this.transform.rotation);
            else
                this.m_deactivateEffect.Create(this.transform.position, this.transform.rotation);
        }

        public bool Interact(Humanoid character, bool repeat, bool alt)
        {
            if (repeat)
                return false;
            if (!PrivateArea.CheckAccess(this.transform.position))
                return true;
            this.m_nview.InvokeRPC("ToggleEnabled", (object)character.GetComponent<Player>().GetPlayerID());
            character.Message(MessageHud.MessageType.Center, m_name + " was " + (this.IsEnabled() ? "turned on" : "turned off"));
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public void Setup(string name) => this.m_nview.GetZDO().Set("creatorName", name);

        private void OnDamaged()
        {
            if (!this.IsEnabled())
                return;
        }
    }
}