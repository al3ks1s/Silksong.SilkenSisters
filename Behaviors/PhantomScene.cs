using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SilkenSisters.SceneManagement;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace SilkenSisters.Behaviors
{
    internal class PhantomScene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            //register();
            getComponents();
            //waitForLace();
            disableAreaDetection();
            editFSMEvents();
            editBossTitle();
        }


        private void register()
        {
            SilkenSisters.Log.LogInfo($"Trying to register phantom");
            SilkenSisters.plugin.phantomBossScene = gameObject;
            SilkenSisters.Log.LogInfo($"{SilkenSisters.plugin.phantomBossScene}");

            SilkenSisters.Log.LogInfo($"Registering FSMOwner");
            SilkenSisters.plugin.phantomBossSceneFSMOwner = new FsmOwnerDefault();
            SilkenSisters.plugin.phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            SilkenSisters.plugin.phantomBossSceneFSMOwner.GameObject = SilkenSisters.plugin.phantomBossScene;
        }

        private void waitForLace()
        {
            while (SilkenSisters.plugin.laceBossFSMOwner == null || SilkenSisters.plugin.lace2BossInstance == null || SilkenSisters.plugin.laceNPCFSMOwner == null)
            {
                SilkenSisters.Log.LogInfo("Lace objects not ready, waiting");
                Task.Delay(100);
            }
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableAreaDetection()
        {
            ((PlayMakerUnity2DProxy)GetComponent(typeof(PlayMakerUnity2DProxy))).enabled = false;
            ((BoxCollider2D)GetComponent(typeof(BoxCollider2D))).enabled = false;
        }

        private void editFSMEvents()
        {
            SilkenSisters.Log.LogInfo($"Trigger lace jump");
            SendEventByName lace_jump_event = new SendEventByName();
            lace_jump_event.sendEvent = "ENTER";
            lace_jump_event.delay = 0;

            FsmEventTarget target = new FsmEventTarget();
            target.gameObject = SilkenSisters.plugin.laceNPCFSMOwner;
            target.target = FsmEventTarget.EventTarget.GameObject;
            
            lace_jump_event.eventTarget = target;

            _control.AddAction("Organ Hit", lace_jump_event);
        }

        private void editBossTitle()
        {
            SilkenSisters.Log.LogInfo($"Change boss title");
            _control.GetAction<DisplayBossTitle>("Start Battle", 3).bossTitle = "SILKEN_SISTERS";
        }

    }
}
