using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{

    internal class Lace2 : MonoBehaviour
    {

        // Spawn pos : 78,7832 104,5677 0,004
        // Constraints left: 72,4, right: 96,52, bot: 104
        private PlayMakerFSM _control;
        private HealthManager _healthManager;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try { 
                SilkenSisters.Log.LogMessage($"[Lace2.Setup] Started setting up Lace");
                getComponents();
                disableParticleEffects();
                editPositionConstraint();
                rerouteState();
                fixActionsPositions();
                disableTitleCard();
                fixWallRangeAlert(); 
                disableLaceMusic();
                setLaceFacing();
                PrepareCorpse();
                prepareSync();
                SilkenSisters.Log.LogMessage($"Damage scaling: {_healthManager.damageScaling.Level1Mult} {_healthManager.damageScaling.Level2Mult} {_healthManager.damageScaling.Level3Mult} {_healthManager.damageScaling.Level4Mult} {_healthManager.damageScaling.Level5Mult} ");
                SilkenSisters.Log.LogMessage($"[Lace2.Setup] Finished setting up Lace");

                gameObject.SetActive(false);

            } catch (Exception e) {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            gameObject.transform.position = new Vector3(78.2832f, 104.5677f, 0.004f);
            SilkenSisters.Log.LogInfo($"[Lace2.getComponents] position:{gameObject.transform.position}");
            _control = gameObject.GetFsmPreprocessed("Control");
            _healthManager = gameObject.GetComponent<HealthManager>();
            _healthManager.damageScaling = SilkenSisters.plugin.phantomBossScene.FindChild("Phantom").GetComponent<HealthManager>().damageScaling;
        }

        private void disableParticleEffects()
        {
            gameObject.FindChild("Pt DashPetal").SetActive(false);
            gameObject.FindChild("Pt SkidPetal").SetActive(false);
            gameObject.FindChild("Pt RisingPetal").SetActive(false);
            gameObject.FindChild("Pt MovePetal").SetActive(false);

        }

        private void editPositionConstraint()
        {
            ConstrainPosition laceBossConstraint = (ConstrainPosition)gameObject.GetComponent(typeof(ConstrainPosition));
            laceBossConstraint.SetXMin(72.4f);
            laceBossConstraint.SetXMax(96.52f);
            laceBossConstraint.SetYMin(104f);
            laceBossConstraint.constrainX = true;
            laceBossConstraint.constrainY = true;

            SilkenSisters.Log.LogInfo($"[Lace2.editPositionConstraint] Constraints: " +
                $"MinX:{laceBossConstraint.xMin}" +
                $"MaxX:{laceBossConstraint.xMax}" +
                $"MinY:{laceBossConstraint.yMin}"
            );
        }

        private void rerouteState()
        {
            _control.ChangeTransition("Init", "REFIGHT", "Start Battle Wait");
            _control.ChangeTransition("Start Battle Wait", "BATTLE START REFIGHT", "Refight Engarde");
            _control.ChangeTransition("Start Battle Wait", "BATTLE START FIRST", "Refight Engarde");

            // Lengthen the engarde state
            Wait wait_engarde = new Wait();
            wait_engarde.time = 2f;
            //_control.AddAction("Refight Engarde", wait_engarde);

            SilkenSisters.Log.LogInfo($"[Lace2.rerouteState] \n" +
                $"              Init:REFIGHT -> {_control.GetTransition("Init", "REFIGHT").ToState} \n" +
                $"              Start Battle Wait:BATTLE START REFIGHT -> {_control.GetTransition("Start Battle Wait", "BATTLE START REFIGHT").ToState}\n" +
                $"              Start Battle Wait:BATTLE START FIRST -> {_control.GetTransition("Start Battle Wait", "BATTLE START FIRST").ToState}");

        }

        private void fixActionsPositions()
        {
            // Change floor height
            SilkenSisters.Log.LogMessage("Fix floor heights");
            _control.GetAction<SetPosition2d>("ComboSlash 1", 0).y = 104.5677f;
            _control.GetAction<SetPosition2d>("Charge Antic", 2).y = 104.5677f;
            _control.GetAction<SetPosition2d>("Counter Antic", 1).y = 104.5677f;

            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] Floor heights:\n" +
                $"              ComboSlash 1: {_control.GetAction<SetPosition2d>("ComboSlash 1", 0).y}\n" +
                $"              Charge Antic: {_control.GetAction<SetPosition2d>("Charge Antic", 2).y}\n" +
                $"              Counter Antic {_control.GetAction<SetPosition2d>("Counter Antic", 1).y}"
            );

            _control.GetAction<SetPosition>("Counter TeleIn", 4).y = 110f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] TeleHeight: {_control.GetAction<SetPosition>("Counter TeleIn", 4).y}");

            FloatClamp clamp_pos = new FloatClamp();
            clamp_pos.floatVariable = _control.FindFloatVariable("Tele X");
            clamp_pos.maxValue = 96f;
            clamp_pos.minValue = 73f;

            _control.InsertAction("Counter TeleIn", clamp_pos, 4);
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] TeleXClamp: min:{_control.GetAction<FloatClamp>("Counter TeleIn", 4).minValue}, max:{_control.GetAction<FloatClamp>("Counter TeleIn", 4).maxValue}");


            _control.FindFloatVariable("Tele Out Floor").Value = 103f;
            _control.GetAction<FloatClamp>("Tele In", 6).minValue = 73f;
            _control.GetAction<FloatClamp>("Tele In", 6).maxValue = 96f;
            _control.GetAction<SetPosition2d>("Tele In", 7).y = 104.5677f;

            // -----
            _control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue = 73f;
            _control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue = 96f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] CrossSlash Pos: min:{_control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue}, max:{_control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue}");

            _control.FindFloatVariable("Land Y").Value = 104.5677f;
            _control.FindFloatVariable("Arena Plat Bot Y").Value = 102f;
            _control.FindFloatVariable("Centre X").Value = 84f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] Float vars: " +
                $"Land Y: {_control.FindFloatVariable("Land Y").Value} " +
                $"Arena Plat Bot Y: {_control.FindFloatVariable("Arena Plat Bot Y").Value} " +
                $"Centre X: {_control.FindFloatVariable("Centre X").Value}"
            );
            

            // -----
            _control.GetAction<CheckXPosition>("Force R?", 2).compareTo = 73f;
            _control.GetAction<CheckXPosition>("Force L?", 1).compareTo = 96f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] Lace Dstab bounds: Left:{_control.GetAction<CheckXPosition>("Force R?", 2).compareTo}, Right:{_control.GetAction<CheckXPosition>("Force L?", 1).compareTo}");

            _control.GetAction<FloatTestToBool>("CrossSlash Aim", 10);

            // ----- Bombs, unused attack for now
            _control.FindFloatVariable("Bomb Max X").Value = 96f;
            _control.FindFloatVariable("Bomb Min X").Value = 72f;
            _control.FindFloatVariable("Bomb Max Y").Value = 115f;
            _control.FindFloatVariable("Bomb Min Y").Value = 105f;

        }

        private void disableTitleCard()
        {
            SilkenSisters.Log.LogMessage("[Lace2.disableTitleCard] Disabling title card");
            _control.DisableAction("Start Battle Refight", 4);
            _control.DisableAction("Start Battle", 4);

            SilkenSisters.Log.LogInfo($"[Lace2.disableTitleCard] " +
                $"(Start Battle Refight):{_control.GetStateAction("Start Battle Refight", 4).active}, " +
                $"(Start Battle):{_control.GetStateAction("Start Battle", 4).active}");

        }

        private void disableLaceMusic()
        {
            _control.DisableAction("Start Battle Refight", 1);
            _control.DisableAction("Start Battle Refight", 2);
        }

        private void fixWallRangeAlert()
        {
            GameObject wallRange = gameObject.transform.parent.gameObject.FindChild("Wall Range");
            wallRange.transform.SetPosition3D(84.0349f, 103.67f, 0f);
            SilkenSisters.Log.LogInfo($"[Lace2.fixWallRangeAlert] position:{wallRange.transform.position}");

            BoxCollider2D[] boxes = wallRange.GetComponents<BoxCollider2D>();
            boxes[0].size = new Vector2(5f, 30f);
            boxes[0].offset = new Vector2(-9f, 0.4726f);

            boxes[1].size = new Vector2(5f, 35.1782f);
            boxes[1].offset = new Vector2(10f, 7.1234f);


            SilkenSisters.Log.LogInfo($"[Lace2.fixWallRangeAlert] alertLeft: Size:{boxes[0].size}, Size:{boxes[0].offset}");
            SilkenSisters.Log.LogInfo($"[Lace2.fixWallRangeAlert] alertRight: Size:{boxes[1].size}, Size:{boxes[1].offset}");
        }

        private void setLaceFacing()
        {
            FaceObjectV2 faceRight = new FaceObjectV2();
            faceRight.spriteFacesRight = true;
            faceRight.playNewAnimation = false;
            faceRight.newAnimationClip = "";
            faceRight.resetFrame = false;
            faceRight.everyFrame = false;
            faceRight.pauseBetweenTurns = 0.5f;
            faceRight.objectA = SilkenSisters.plugin.laceBossFSMOwner;
            faceRight.objectB = SilkenSisters.hornet;

            _control.InsertAction("Init", faceRight, 4);
            _control.DisableAction("Refight Engarde", 0);

            SilkenSisters.Log.LogInfo($"[Lace2.setLaceFacing] Facing Action:{_control.GetStateAction("Init", 4).GetType()}");
            SilkenSisters.Log.LogInfo($"[Lace2.Refight Engarde] Base facing active?:{_control.GetStateAction("Refight Engarde", 0).active}");
        }


        private void PrepareCorpse()
        {
            SilkenSisters.Log.LogDebug("Started setting corpse handler");
            GameObject laceCorpse = gameObject.FindChild("Corpse Lace2(Clone)");
            GameObject laceCorpseNPC = laceCorpse.FindChild("NPC");
            SilkenSisters.Log.LogDebug($"{laceCorpseNPC}");

            SilkenSisters.Log.LogDebug("Fixing facing");
            PlayMakerFSM laceCorpseFSM = FsmUtil.GetFsmPreprocessed(laceCorpse, "Control");
            laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 0).compareTo = 72;
            laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 1).compareTo = 96;

            SilkenSisters.Log.LogDebug("Disabling interact action");
            laceCorpseFSM.DisableAction("NPC Ready", 0);
            SendEventByName lace_death_event = new SendEventByName();
            lace_death_event.sendEvent = "INTERACT";
            lace_death_event.delay = 0f;
            FsmOwnerDefault owner = new FsmOwnerDefault();
            owner.gameObject = laceCorpseNPC;
            owner.ownerOption = OwnerDefaultOption.SpecifyGameObject;

            FsmEventTarget target = new FsmEventTarget();
            target.gameObject = owner;
            target.target = FsmEventTarget.EventTarget.GameObject;
            lace_death_event.eventTarget = target;
            laceCorpseFSM.AddAction("NPC Ready", lace_death_event);

            SilkenSisters.Log.LogDebug("Editing NPC routes to skip dialogue");
            PlayMakerFSM laceCorpseNPCFSM = FsmUtil.GetFsmPreprocessed(laceCorpseNPC, "Control");
            SilkenSisters.Log.LogDebug("Editing Idle");
            laceCorpseNPCFSM.ChangeTransition("Idle", "INTERACT", "Drop Pause");
            SilkenSisters.Log.LogDebug("Drop Pause");
            laceCorpseNPCFSM.DisableAction("Drop Pause", 0);
            laceCorpseNPCFSM.ChangeTransition("Drop Down", "FINISHED", "End Pause");
            laceCorpseNPCFSM.ChangeTransition("Drop Down", "IS_HURT", "End Pause");
            SilkenSisters.Log.LogDebug("End Pause");
            laceCorpseNPCFSM.DisableAction("End Pause", 0);
            laceCorpseNPCFSM.GetAction<Wait>("End Pause", 1).time = 0.5f;

            SilkenSisters.Log.LogDebug("Disabling end actions");
            laceCorpseNPCFSM.DisableAction("End", 5);
            laceCorpseNPCFSM.DisableAction("End", 6);
            laceCorpseNPCFSM.DisableAction("End", 7);
            laceCorpseNPCFSM.DisableAction("End", 8);
            laceCorpseNPCFSM.DisableAction("End", 9);
            laceCorpseNPCFSM.DisableAction("End", 10);
            laceCorpseNPCFSM.DisableAction("End", 11);
            laceCorpseNPCFSM.DisableAction("End", 12);
            laceCorpseNPCFSM.DisableAction("End", 13);

            SilkenSisters.Log.LogDebug("Disabling audio cutting");
            laceCorpseFSM.DisableAction("Start", 0);
            laceCorpseNPCFSM.DisableAction("Talk 1 Start", 3);
            laceCorpseNPCFSM.DisableAction("End", 0);

            SilkenSisters.Log.LogDebug("Finished setting up corpse handler");
        }



        // Sync fight edits
        private void prepareSync()
        {
            if (SilkenSisters.syncedFight.Value && SilkenSisters.debugBuild) {

                _control.enabled = false;
                
                NukeCoreAI();
                SyncTransitions();

                _control.enabled = true;

                SilkenSisters.Log.LogMessage($"[Lace.prepareSync] Finished doing the sync stuff");
            }
        }

        private void NukeCoreAI()
        {
            SilkenSisters.Log.LogMessage($"[Lace.prepareSync] Nuking a few states");

            _control.RemoveState("Close");
            _control.RemoveState("Far");
            _control.RemoveState("Distance Check");
            _control.RemoveState("CrossSlash?");
            _control.RemoveState("P3 Check");
            _control.RemoveState("P2 Check");
            
            _control.RemoveTransition("Idle", "ATTACK");
            _control.RemoveTransition("Idle", "TOOK DAMAGE");

            _control.ChangeTransition("Tele End", "FINISHED", "Idle");
            _control.ChangeTransition("Counter End", "FINISHED", "Idle");
            _control.ChangeTransition("Evade Move", "FINISHED", "Idle");
            _control.ChangeTransition("Start Battle Refight", "FINISHED", "Idle");
            _control.ChangeTransition("Land", "FINISHED", "Idle");
            
            _control.DisableAction("Idle", 2);
            _control.DisableAction("Idle", 7);
            _control.DisableAction("Idle", 9);
            _control.AddAction("Idle", new Wait { time = 0.01f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") });


            //*/
        }


        private void SyncTransitions()
        {
            _control.AddState("SyncWait");
            _control.AddTransition("Idle", "FINISHED", "SyncWait");

            _control.AddTransition("SyncWait", "HOP CHARGE", "Hop To Charge");
            _control.AddTransition("SyncWait", "HOP JSLASH", "Hop To J Slash");
            _control.AddTransition("SyncWait", "HOP COMBO", "Hop To Combo");
            _control.AddTransition("SyncWait", "COMBO", "ComboSlash 1");
            _control.AddTransition("SyncWait", "JSLASH", "J Slash Antic");
            _control.AddTransition("SyncWait", "COUNTER", "Counter Antic");
            _control.AddTransition("SyncWait", "TO P2", "Hop To P2");
            _control.AddTransition("SyncWait", "TO P3", "Hop To P3");
            _control.AddTransition("SyncWait", "CROSS SLASH", "CrossSlash Aim");

            _control.AddAction(
                "SyncWait", 
                new SendEventByName { 
                    eventTarget = new FsmEventTarget { 
                        gameObject = SilkenSisters.plugin.phantomBossSceneFSMOwner, 
                        fsmName = "Silken Sisters Sync Control", 
                        target = FsmEventTarget.EventTarget.GameObjectFSM 
                    }, 
                    sendEvent = "LACE READY", 
                    delay = 0f,
                    everyFrame = true
                }
            );

        }

    }

    internal class Lace2Scene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            SilkenSisters.Log.LogMessage($"[Lace2Scene.Setup] Started setting Lace scene up");
            getComponents();
            disableSceneObjects();
            moveSceneBounds();
            SilkenSisters.Log.LogMessage($"[Lace2Scene.Setup] Finished setting Lace scene up");
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableSceneObjects()
        {
            SilkenSisters.Log.LogMessage($"[Lace2Scene.disableSceneObjects] Disabling unwanted LaceBossScene items");
            gameObject.FindChild("Flower Effect Hornet").SetActive(false);
            gameObject.FindChild("steam hazard").SetActive(false);
            gameObject.FindChild("Silk Heart Memory Return").SetActive(false);
        }

        private void moveSceneBounds()
        {
            SilkenSisters.Log.LogMessage($"[Lace2Scene.moveSceneBounds] Moving lace arena objects");
            gameObject.FindChild("Arena L").transform.position = new Vector3(72f, 104f, 0f);
            gameObject.FindChild("Arena R").transform.position = new Vector3(97f, 104f, 0f);
            gameObject.FindChild("Centre").transform.position = new Vector3(84.5f, 104f, 0f);
        }
    }

    internal class SilkenController : MonoBehaviour
    {
        private PlayMakerFSM _control;
        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {

        }


    }

}
