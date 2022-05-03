using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OdinsTraps
{
    class TrapEnabler : MonoBehaviour, Hoverable, Interactable
    {

        public string m_name = "";
        //  public GameObject m_enabledEffect;
        public EffectList m_activateEffect = new EffectList();
        public string turnOffMessage = "";
        public string turnOnMessage = "";
        public EffectList m_deactivateEffect = new EffectList();
        public ZNetView m_nview;
        public Piece m_piece;
        public GameObject m_enabledObject;

        private void Awake()
        {

            this.m_nview = this.GetComponent<ZNetView>();
            if (!this.m_nview.IsValid())
                return;
            this.GetComponent<WearNTear>().m_onDamaged += new Action(this.OnDamaged);
            this.m_piece = this.GetComponent<Piece>();

            if (m_name == "")
            {
                m_name = this.m_piece.m_name;
                if (m_name == "")
                {
                    m_name = " Active Me";
                }
            }

            this.InvokeRepeating("UpdateStatus", 0.0f, 1f);
            this.m_nview.Register("ToggleEnabled", new Action<long>(this.RPC_ToggleEnabled));
            this.m_nview.Register<long>("ControlerToggleEnabled", new Action<long, long>(this.RPC_ControlerToggleEnabled));
            if (!TrapController.m_traps.Contains<TrapEnabler>(this))
            {
                TrapController.m_traps.Add(this);   
            }          
        }
     
        private void UpdateStatus()
        { 
            bool flag = this.IsEnabled();
            if (m_enabledObject != null)
            {
                m_enabledObject.SetActive(flag);
            }
            else
            {
                Debug.LogWarning("No object to Toggle");
            }

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

        private void RPC_ToggleEnabled(long uid)
        {
            this.SetEnabled(!this.IsEnabled());
        }
        private void RPC_ControlerToggleEnabled(long uid, long state)
        {
            this.SetEnabled(Convert.ToBoolean(state));
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
            this.m_nview.InvokeRPC("ToggleEnabled");

            if (turnOffMessage != "" && turnOnMessage != "")
            {
                character.Message(MessageHud.MessageType.Center, (this.IsEnabled() ? turnOnMessage : turnOffMessage));
            }
            else
            {
                character.Message(MessageHud.MessageType.Center, m_name + " was " + (this.IsEnabled() ? "turned on" : "turned off"));
            }
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public void Setup(string name) => this.m_nview.GetZDO().Set("creatorName", name);

        private void OnDamaged()
        {
            if (!this.IsEnabled())
                return;
        }
        private void OnDestroy() => TrapController.m_traps.Remove(this);
    }
}
