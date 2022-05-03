using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OdinsTraps
{
    class TrapController : MonoBehaviour, Hoverable, Interactable
    {
        public string m_name = "Trap Control";
        public float m_radius = 10f;
        public float m_updateConnectionsInterval = 5f;
        public CircleProjector m_areaMarker;
        public EffectList m_activateEffect = new EffectList();
        public EffectList m_deactivateEffect = new EffectList();
        public GameObject m_connectEffect;
        public GameObject m_inRangeEffect;
        public ZNetView m_nview;
        public Piece m_piece;

        private List<GameObject> m_connectionInstances = new List<GameObject>();
        private float m_connectionUpdateTime = -1000f;
        private List<TrapController> m_connectedAreas = new List<TrapController>();
        private bool m_tempChecked;
        private static List<TrapController> m_allAreas = new List<TrapController>();
        public string turnOffMessage = "";
        public string turnOnMessage = "";
        public static List<TrapEnabler> m_traps = new List<TrapEnabler>();
        public GameObject m_switchOn;
        public GameObject m_switchOff;


        private void Awake()
        {
            if ((bool)(UnityEngine.Object)this.m_areaMarker)
                this.m_areaMarker.m_radius = this.m_radius;

            this.m_nview = this.GetComponent<ZNetView>();
            if (!this.m_nview.IsValid())
                return;
            this.GetComponent<WearNTear>().m_onDamaged += new Action(this.OnDamaged);
            this.m_piece = this.GetComponent<Piece>();
            if ((bool)(UnityEngine.Object)this.m_areaMarker)
                this.m_areaMarker.gameObject.SetActive(false);

            if ((bool)(UnityEngine.Object)this.m_inRangeEffect)
                this.m_inRangeEffect.SetActive(false);

            if (!TrapController.m_allAreas.Contains<TrapController>(this))
            {
                TrapController.m_allAreas.Add(this);
            }

            this.InvokeRepeating("UpdateStatus", 0.0f, 1f);
            this.m_nview.Register<long>("ToggleTrapControlerEnabled", new Action<long, long>(this.RPC_ToggleTrapControlerEnabled));
        }
        private void OnDestroy() => TrapController.m_allAreas.Remove(this);

        public string GetHoverText()
        {
            if (!PrivateArea.CheckAccess(this.transform.position, flash: false))
                return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");

            if (this.IsEnabled())
            {
                return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] Turn All traps off");
            }
            else
            {
                return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] Turn All traps on");
            }
        }

        public string GetHoverName() => this.m_name;

        private void RPC_ToggleTrapControlerEnabled(long uid, long playerID)
        {
            ZLog.Log((object)("Toggle enabled from " + (object)playerID + "  creator is " + (object)this.m_piece.GetCreator()));
            if (!this.m_nview.IsOwner() || this.m_piece.GetCreator() != playerID)
                return;
            this.SetEnabled(!this.IsEnabled());
            bool flag = !this.IsEnabled();
            long str = 1;
            if (flag)
            {
                str =0;
            }
            foreach (TrapEnabler trap in TrapController.m_traps)
            {
                trap.m_nview.InvokeRPC("ControlerToggleEnabled", str);
            }
        }

        public bool IsEnabled() => this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool("enabled");

        private void SetEnabled(bool enabled)
        {
            this.m_nview.GetZDO().Set(nameof(enabled), enabled);
            if (enabled)
            {
                this.m_activateEffect.Create(this.transform.position, this.transform.rotation);
                this.m_switchOn.SetActive(true);
                this.m_switchOff.SetActive(false);
            }else
            {
                this.m_deactivateEffect.Create(this.transform.position, this.transform.rotation);
                this.m_switchOn.SetActive(false);
                this.m_switchOff.SetActive(true);
            }
              
        }

        public void Setup(string name) => this.m_nview.GetZDO().Set("creatorName", name);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public bool Interact(Humanoid character, bool repeat, bool alt)
        {
            if (repeat)
                return false;
            if (!PrivateArea.CheckAccess(this.transform.position))
                return true;
            this.m_nview.InvokeRPC("ToggleTrapControlerEnabled", (object)character.GetComponent<Player>().GetPlayerID());

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

        private bool IsInside(Vector3 point, float radius) => (double)Utils.DistanceXZ(this.transform.position, point) < (double)this.m_radius + (double)radius;

        private void HideMarker() => this.m_areaMarker.gameObject.SetActive(false);


        public void ShowAreaMarker()
        {
            if (!(bool)(UnityEngine.Object)this.m_areaMarker)
                return;
            this.m_areaMarker.gameObject.SetActive(true);
            this.CancelInvoke("HideMarker");
            this.Invoke("HideMarker", 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
        }

        private void OnDamaged()
        {
            if (!this.IsEnabled())
                return;
        }
        public void PokeConnectionEffects()
        {
            List<TrapController> connectedAreas = this.GetConnectedAreas();
            this.StartConnectionEffects();
            foreach (TrapController trapController in connectedAreas)
                trapController.StartConnectionEffects();
        }
        private void StartConnectionEffects()
        {
            List<TrapController> trapControllerList = new List<TrapController>();
            foreach (TrapController allArea in TrapController.m_allAreas)
            {
                if (!((UnityEngine.Object)allArea == (UnityEngine.Object)this) && this.IsInside(allArea.transform.position, 0.0f))
                    trapControllerList.Add(allArea);
            }
            Vector3 position = this.transform.position + Vector3.up * 1.4f;
            if (this.m_connectionInstances.Count != trapControllerList.Count)
            {
                this.StopConnectionEffects();
                for (int index = 0; index < trapControllerList.Count; ++index)
                    this.m_connectionInstances.Add(UnityEngine.Object.Instantiate<GameObject>(this.m_connectEffect, position, Quaternion.identity, this.transform));
            }
            if (this.m_connectionInstances.Count == 0)
                return;
            for (int index = 0; index < trapControllerList.Count; ++index)
            {
                Vector3 vector3 = trapControllerList[index].transform.position + Vector3.up * 1.4f - position;
                Quaternion quaternion = Quaternion.LookRotation(vector3.normalized);
                GameObject connectionInstance = this.m_connectionInstances[index];
                connectionInstance.transform.position = position;
                connectionInstance.transform.rotation = quaternion;
                connectionInstance.transform.localScale = new Vector3(1f, 1f, vector3.magnitude);
            }
            this.CancelInvoke("StopConnectionEffects");
            this.Invoke("StopConnectionEffects", 0.3f);
        }

        private void StopConnectionEffects()
        {
            foreach (UnityEngine.Object connectionInstance in this.m_connectionInstances)
                UnityEngine.Object.Destroy(connectionInstance);
            this.m_connectionInstances.Clear();
        }


        private List<TrapController> GetConnectedAreas(bool forceUpdate = false)
        {
            if ((double)Time.time - (double)this.m_connectionUpdateTime > (double)this.m_updateConnectionsInterval | forceUpdate)
            {
                this.GetAllConnectedAreas(this.m_connectedAreas);
                this.m_connectionUpdateTime = Time.time;
            }
            return this.m_connectedAreas;
        }

        private void GetAllConnectedAreas(List<TrapController> areas)
        {
            Queue<TrapController> trapControllerQueue = new Queue<TrapController>();
            trapControllerQueue.Enqueue(this);
            foreach (TrapController allArea in TrapController.m_allAreas)
                allArea.m_tempChecked = false;
            this.m_tempChecked = true;
            while (trapControllerQueue.Count > 0)
            {
                TrapController trapController = trapControllerQueue.Dequeue();
                foreach (TrapController allArea in TrapController.m_allAreas)
                {
                    if (!allArea.m_tempChecked && allArea.IsEnabled() && allArea.IsInside(trapController.transform.position, 0.0f))
                    {
                        allArea.m_tempChecked = true;
                        trapControllerQueue.Enqueue(allArea);
                        areas.Add(allArea);
                    }
                }
            }
        }

        public static TrapController IsInRange(GameObject piece, float radius = 0.0f )
        {
            Vector3 point = piece.transform.position;

            List<TrapController> trapControllerList = new List<TrapController>();
       
            foreach (TrapController allArea in TrapController.m_allAreas)
            {
                if (allArea.IsEnabled() && allArea.IsInside(point, radius))
                {
                    trapControllerList.Add(allArea);
                }
            }
            if(trapControllerList.Count > 0)
            {
                TrapController trapController = trapControllerList.First<TrapController>();
                return trapController;
            }

            return null;
        }

    }
}
