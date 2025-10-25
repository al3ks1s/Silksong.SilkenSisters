using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GenericVariableExtension;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

// Idea by AidaCamelia0516

// Challenge : Cradle_03 - Boss Scene/Intro Sequence

// Common health pool -> Sync fight phases
// 

// Three levels of fight :
// Lvl 1: Lace 1 Skipped -> Fight lace 1 and phantom at the same time       -> Cutscene shows hornet and lace instead of phantom, the moment parry is hit, phantom kicks lace out and take the bullet
// Lvl 2: Deep memory in organ chamber -> Lace 2 + Phantom                  -> This time lace takes the bullet, single final phase of dragoon dash
// Lvl 3: Taunt challenge in deep memory -> Lace 2 + Phantom on steroids    -> Fakeout cutscene, goes on normally until lace repels hornet + Hard phase

// Hidden interaction if phantom beaten alone                               -> Lace mourning over Phantom's remains, TeleOut after quick dialogue

// How to make Phantom stronger :
//      - Less idle time
//      - Faster Throws
//      - Less timeout between dives
//      - Multi dives - With patterns?
//      - Fog clone

// How to make Lace stronger:
//      - Less idle time
//      - Multiple cross slash like lost lace
//      - 

// __instance.GetComponent<tk2dSpriteAnimator>().Library.GetClipByName("Jump Antic").fps = 40;

namespace SilkenSisters
{

    // TODO - adjust the plugin guid as needed
    [BepInAutoPlugin(id: "io.github.al3ks1s.silkensisters")]
    [BepInDependency("org.silksong-modding.fsmutil")]
    public partial class SilkenSisters : BaseUnityPlugin
    {
        private static GameObject laceNPC;
        private static FsmOwnerDefault laceNPCFSMOwner;

        private static GameObject laceBoss;
        private static FsmOwnerDefault laceBossFSMOwner;
        private static GameObject laceBossScene;
        private static FsmOwnerDefault laceBossSceneFSMOwner;
        private static bool laceBoss2Active = false;


        private static GameObject phantomBoss;
        private static GameObject phantomBossScene;
        private static FsmOwnerDefault phantomBossSceneFSMOwner;
        private static bool phantomSpeedToggle = false;
        private static bool phantomDragoonToggle = false;


        private static GameObject hornet;
        private static FsmOwnerDefault hornetFSMOwner;

        private ConfigEntry<KeyCode> modifierKey;
        private ConfigEntry<KeyCode> actionKey;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Logger.LogInfo($"Plugin loaded and initialized {Application.streamingAssetsPath}");
            Log = base.Logger;

            modifierKey = Config.Bind(
                "Keybinds",
                "Modifier",
                KeyCode.LeftAlt,
                "Modifier"
            );

            StartCoroutine(WaitAndPatch());

            Harmony.CreateAndPatchAll(typeof(SilkenSisters));
        }


        private IEnumerator WaitAndPatch()
        {
            yield return new WaitForSeconds(2f); // Give game time to init Language
            Harmony.CreateAndPatchAll(typeof(Language_Get_Patch));
        }

        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GetAngleToTarget2D), "DoGetAngle")]
        private static void setAngleListenerS(GetAngleToTarget2D __instance)
        {
            SilkenSisters.Log.LogInfo($"DoGetAngle  Angle detected {__instance.storeAngle} to object {__instance.target.Name}");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClampAngle), "DoClamp")]
        private static void setClampAngleListener(ClampAngle __instance)
        {
            SilkenSisters.Log.LogInfo($"DoClamp     Clamped to angle {__instance.fsm.FindFloatVariable("Angle")} between {__instance.minValue}/{__instance.maxValue} : {__instance.angleVariable}");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClampAngleByScale), "DoClamp")]
        private static void setClampAngleScaleListener(ClampAngleByScale __instance)
        {
            SilkenSisters.Log.LogInfo($"DoClampScl  Clamped angle {__instance.fsm.FindFloatVariable("Angle")} between {__instance.positiveMinValue}/{__instance.positiveMaxValue} or {__instance.negativeMinValue}/{__instance.negativeMaxValue} : {__instance.angleVariable}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SetVelocityAsAngle), "DoSetVelocity")]
        private static void setVelocitySetListener(SetVelocityAsAngle __instance)
        {
            SilkenSisters.Log.LogInfo($"DoVelocity  Set to angle {__instance.fsm.FindFloatVariable("Angle")}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CheckAlertRange), "Apply")]
        private static void setRangeListener(CheckAlertRange __instance)
        {
            //SilkenSisters.Log.LogInfo($"AlertRng    Is in range {__instance.isCurrentlyInRange}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void setEventListener(FsmState __instance)
        {

            if (SilkenSisters.hornet != null)
            {
                if (__instance.Fsm.GameObjectName == SilkenSisters.hornet.gameObject.name)
                {
                    //SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, fsm: {__instance.Fsm.Name} Entering state {__instance.Name}, {__instance.Actions.Length} actions");
                }
            }

            if (__instance.Fsm.GameObject.name == "Phantom" && __instance.Name == "Idle")
            {
                SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, Entering state {__instance.Name}, {__instance.Actions.Length} actions");
                if (__instance.Actions.Length > 0)
                {
                    foreach (FsmStateAction action in __instance.Actions)
                    {
                        SilkenSisters.Log.LogInfo($"    Action for state {__instance.Name}: {action.GetType()}");
                    }
                }
            }

            if (__instance.Fsm.GameObject.name == "Corpse Lace2(Clone)")
            {
                SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, Entering state {__instance.Name}, {__instance.Actions.Length} actions");
                if (__instance.Actions.Length > 0)
                {
                    foreach (FsmStateAction action in __instance.Actions)
                    {
                        SilkenSisters.Log.LogInfo($"    Action for state {__instance.Name}: {action.GetType()}");
                    }
                }
            }

            if (__instance.Fsm.GameObject.name == "NPC")
            {
                SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, Entering state {__instance.Name}, {__instance.Actions.Length} actions");
                if (__instance.Actions.Length > 0)
                {
                    foreach (FsmStateAction action in __instance.Actions)
                    {
                        SilkenSisters.Log.LogInfo($"    Action for state {__instance.Name}: {action.GetType()}");
                    }
                }
            }

            // Enable Corpse Lace Hooking
            if (__instance.Fsm.GameObject.name == "Corpse Lace2(Clone)" && __instance.Name == "Start" && SilkenSisters.laceBoss2Active)
            {
                SilkenSisters.Log.LogInfo("Started setting corpse handler");
                GameObject laceCorpse = __instance.Fsm.GameObject;
                GameObject laceCorpseNPC = GameObject.Find($"{__instance.Fsm.GameObject.name}/NPC");
                SilkenSisters.Log.LogInfo($"{laceCorpseNPC}");

                SilkenSisters.Log.LogInfo("Fixing facing");
                PlayMakerFSM laceCorpseFSM = FsmUtil.GetFsmPreprocessed(laceCorpse, "Control");
                laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 0).compareTo = 72;
                laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 1).compareTo = 96;

                SilkenSisters.Log.LogInfo("Disabling interact action");
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

                SilkenSisters.Log.LogInfo("Editing NPC routes to skip dialogue");
                PlayMakerFSM laceCorpseNPCFSM = FsmUtil.GetFsmPreprocessed(laceCorpseNPC, "Control");
                SilkenSisters.Log.LogInfo("Editing Idle");
                laceCorpseNPCFSM.ChangeTransition("Idle", "INTERACT", "Drop Pause");
                SilkenSisters.Log.LogInfo("Drop Pause");
                laceCorpseNPCFSM.DisableAction("Drop Pause", 0);
                laceCorpseNPCFSM.ChangeTransition("Drop Down", "FINISHED", "End Pause");
                laceCorpseNPCFSM.ChangeTransition("Drop Down", "IS_HURT", "End Pause");
                SilkenSisters.Log.LogInfo("End Pause");
                laceCorpseNPCFSM.DisableAction("End Pause", 0);
                laceCorpseNPCFSM.GetAction<Wait>("End Pause", 1).time = 0.5f;

                SilkenSisters.Log.LogInfo("Disabling end actions");
                laceCorpseNPCFSM.DisableAction("End", 5);
                laceCorpseNPCFSM.DisableAction("End", 6);
                laceCorpseNPCFSM.DisableAction("End", 7);
                laceCorpseNPCFSM.DisableAction("End", 8);
                laceCorpseNPCFSM.DisableAction("End", 9);
                laceCorpseNPCFSM.DisableAction("End", 10);
                laceCorpseNPCFSM.DisableAction("End", 11);
                laceCorpseNPCFSM.DisableAction("End", 12);
                laceCorpseNPCFSM.DisableAction("End", 13);

                SilkenSisters.Log.LogInfo("Finished setting up corpse handler");
            }

            if (__instance.Fsm.GameObject.name == "Lace Boss2 New")
            {
                if (__instance.Name == "Dstab Angle" || __instance.Name == "Tele In" || __instance.Name == "Downstab")
                {
                    SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, Entering state {__instance.Name} {__instance.Transitions.FirstOrDefault().EventName}");
                    if (__instance.Actions.Length > 0)
                    {
                        foreach (FsmStateAction action in __instance.Actions)
                        {
                            // SilkenSisters.Log.LogInfo($"    Action for state {__instance.Name}: {action.GetType()}");

                            if (action.GetType() == typeof(SetPosition2d))
                            {
                                SilkenSisters.Log.LogInfo($"        Teleport to heigth {((SetPosition2d)action).y}");
                            }
                        }
                    }
                }
            }
        }

        private void setupPhantom()
        {
            if (SilkenSisters.phantomBossScene != null && SilkenSisters.phantomBoss != null)
            {

                PlayMakerFSM phantomSceneFSM = FsmUtil.GetFsmPreprocessed(SilkenSisters.phantomBossScene, "Control");
                PlayMakerFSM phantomBossFSM = FsmUtil.GetFsmPreprocessed(SilkenSisters.phantomBoss, "Control");
                
                // Disable phantom's arena detectors
                ((PlayMakerUnity2DProxy)SilkenSisters.phantomBossScene.GetComponent(typeof(PlayMakerUnity2DProxy))).enabled = false;
                ((BoxCollider2D)SilkenSisters.phantomBossScene.GetComponent(typeof(BoxCollider2D))).enabled = false;

                // Trigger lace jump
                SendEventByName lace_jump_event = new SendEventByName();
                lace_jump_event.sendEvent = "ENTER";
                lace_jump_event.delay = 0;
                FsmEventTarget target = new FsmEventTarget();
                target.gameObject = SilkenSisters.laceNPCFSMOwner;
                target.target = FsmEventTarget.EventTarget.GameObject;
                lace_jump_event.eventTarget = target;

                phantomSceneFSM.AddAction("Organ Hit", lace_jump_event);

                // FG Column - enable LaceBoss Object
                ActivateGameObject activate_lace_boss = new ActivateGameObject();
                activate_lace_boss.activate = true;
                activate_lace_boss.gameObject = SilkenSisters.laceBossFSMOwner;
                activate_lace_boss.recursive = false;


                phantomBossFSM.AddAction("Appear", activate_lace_boss);

                // Trigger lace boss
                SendEventByName lace_boss_start = new SendEventByName();
                lace_boss_start.sendEvent = "BATTLE START FIRST";
                lace_boss_start.delay = 0;
                FsmEventTarget target_boss = new FsmEventTarget();
                target_boss.gameObject = SilkenSisters.laceBossFSMOwner;
                target_boss.target = FsmEventTarget.EventTarget.GameObject;
                lace_boss_start.eventTarget = target_boss;

                phantomBossFSM.AddAction("To Idle", lace_boss_start);

                phantomSceneFSM.GetAction<DisplayBossTitle>("Start Battle", 3).bossTitle = "SILKEN_SISTERS";

                // Skip cutscene
                phantomBossFSM.GetAction<Wait>("Time Freeze", 4).time = 0.001f;
                phantomBossFSM.GetAction<ScaleTime>("Time Freeze", 5).timeScale = 1f;

                phantomBossFSM.DisableAction("Parry Ready", 0);
                phantomBossFSM.DisableAction("Parry Ready", 1);
                phantomBossFSM.GetAction<Wait>("Parry Ready", 4).time = 0.001f;
                phantomBossFSM.GetAction<Wait>("Parry Ready", 4).finishEvent = FsmEvent.GetFsmEvent("PARRY");

                phantomBossFSM.ChangeTransition("Death Explode", "FINISHED", "End Recover");
                phantomBossFSM.DisableAction("End Recover", 3);

                SilkenSisters.phantomBoss.transform.SetPositionX(77.04f);

                Logger.LogInfo($"Finished setting up phantom");
            }
        }

        private void spawnChallengeSequence()
        {

            Scene cur_scene = SceneManager.GetActiveScene();
            Logger.LogInfo($"Current scene {SceneManager.GetActiveScene().name}");
            Logger.LogInfo("Loading Cradle scene");

            PlayerData __instance = PlayerData.instance;

            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "scenes_scenes_scenes", "cradle_03.bundle"));
            AsyncOperation op = SceneManager.LoadSceneAsync("Cradle_03", LoadSceneMode.Additive);

            op.completed += (AsyncOperation obj) =>
            {
                Scene scene = SceneManager.GetSceneByName("Cradle_03");

                Logger.LogInfo($"Scene {scene.name} successfully loaded");

                GameObject go = GameObject.Find("Boss Scene/Intro Sequence");
                Logger.LogInfo($"Found sequence? {go}");
                GameObject challengeDialog = Instantiate(go);
                Logger.LogInfo($"Copying challenge dialog, new id: {challengeDialog.name}");
                Logger.LogInfo($"Moving ${challengeDialog.name} to {cur_scene.name}");
                SceneManager.MoveGameObjectToScene(challengeDialog, cur_scene);
                // Challenge region 84.375 106.8835 3.64 - 84,2341 112,4307 4,9999
                // Challenge dialog 83,9299 105,8935 2,504

                GameObject challengeRegion = GameObject.Find($"{challengeDialog.name}/Challenge Region");

                challengeDialog.transform.position = new Vector3(84.45f, 105.8935f, 2.504f);
                GameObject.Find($"{challengeDialog.name}/Challenge Region").transform.SetPositionX(84.2341f); // 0,1451 0,99
                GameObject.Find($"{challengeDialog.name}/Challenge Region").transform.SetPositionY(107.0495f);
                GameObject.Find($"{challengeDialog.name}/Challenge Region").transform.SetPositionZ(4.9999f);
                Logger.LogInfo($"Setting dialog position at {challengeDialog.transform.position}");

                Logger.LogInfo($"Disabling Cradle specific things");
                GameObject.Find($"{challengeDialog.name}/Challenge Glows/Cradle__0013_loom_strut_based (2)").SetActive(false);
                GameObject.Find($"{challengeDialog.name}/Challenge Glows/Cradle__0013_loom_strut_based (3)").SetActive(false);

                PlayMakerFSM challengeDialogFSM = challengeDialog.GetFsmPreprocessed("First Challenge");
                PlayMakerFSM challengeDialogRegionFSM = challengeRegion.GetFsmPreprocessed("Challenge");

                Logger.LogInfo("Disabling Silk's intro");
                challengeDialogFSM.GetTransition("Idle", "CHALLENGE START").FsmEvent = FsmEvent.GetFsmEvent("QUICK START");

                Logger.LogInfo("Attributing the dialog disable to challenge region");
                ActivateGameObject disableChallengeObject = new ActivateGameObject();
                FsmOwnerDefault disableOwner = new FsmOwnerDefault();
                disableOwner.ownerOption = OwnerDefaultOption.SpecifyGameObject;
                disableOwner.gameObject = challengeDialog;
                disableChallengeObject.activate = false;
                disableChallengeObject.gameObject = disableOwner;
                Logger.LogInfo($"Adding action {disableChallengeObject} to {challengeDialogRegionFSM}");
                challengeDialogRegionFSM.AddAction("Challenge Complete", disableChallengeObject);

                // Trigger phantom boss scene
                Logger.LogInfo($"Setting battle trigger");
                SendEventByName battle_begin_event = new SendEventByName();
                battle_begin_event.sendEvent = "ENTER";
                battle_begin_event.delay = 0;
                FsmEventTarget target = new FsmEventTarget();
                target.gameObject = SilkenSisters.phantomBossSceneFSMOwner;
                target.target = FsmEventTarget.EventTarget.GameObject;
                battle_begin_event.eventTarget = target;

                challengeDialogRegionFSM.AddAction("Challenge Complete", battle_begin_event);
                challengeDialogRegionFSM.GetAction<GetXDistance>("Straight Back?", 1).gameObject.ownerOption = OwnerDefaultOption.UseOwner;

                PlayMakerFSM HornetSpecialSFM = SilkenSisters.hornet.GetComponents<PlayMakerFSM>().First(f => f.FsmName == "Silk Specials");
                Logger.LogInfo($"{HornetSpecialSFM.FsmName}");
                challengeDialogRegionFSM.DisableAction("Hornet Voice", 0);
                challengeDialogRegionFSM.AddAction("Hornet Voice", HornetSpecialSFM.GetStateAction("Standard", 0));

                Logger.LogInfo($"Unloading {scene.name} scene");
                SceneManager.UnloadScene(scene.name);
                Logger.LogInfo($"Unloading bundle {bundle.name}");
                bundle.Unload(false);
            };
        }

        private void toggleLaceFSM()
        {
            if (SilkenSisters.laceBoss != null)
            {
                PlayMakerFSM pfsm = SilkenSisters.laceBoss.GetComponents<PlayMakerFSM>().First(pfsm => pfsm.FsmName == "Control");
                pfsm.SetState("Idle");
                pfsm.fsm.manualUpdate = !pfsm.fsm.manualUpdate;
            }
        }

        private void spawnLaceNpc()
        {

            Logger.LogInfo($"Spawning lace on the organ bench");

            Scene cur_scene = SceneManager.GetActiveScene();
            Logger.LogInfo($"Current scene {SceneManager.GetActiveScene().name}");
            Logger.LogInfo("Loading coral scene");

            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "scenes_scenes_scenes", "coral_19.bundle"));
            AsyncOperation op = SceneManager.LoadSceneAsync("Coral_19", LoadSceneMode.Additive);

            op.completed += (AsyncOperation obj) =>
            {

                Scene scene = SceneManager.GetSceneByName("Coral_19");

                Logger.LogInfo($"Scene {scene.name} successfully loaded");

                GameObject go = GameObject.Find("Lace NPC Blasted Bridge");
                GameObject lace = Instantiate(go);

                Logger.LogInfo($"Copying lace npc, new id: {lace.name}");
                Logger.LogInfo($"Moving {lace.name} to {cur_scene.name}");
                SceneManager.MoveGameObjectToScene(lace, cur_scene);

                Logger.LogInfo($"Disabling lace npc range detection");
                GameObject.Find($"{lace.name}/Start Range").SetActive(false);

                SilkenSisters.laceNPC = lace;
                SilkenSisters.laceNPCFSMOwner = new FsmOwnerDefault();
                SilkenSisters.laceNPCFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                SilkenSisters.laceNPCFSMOwner.GameObject = SilkenSisters.laceNPC;

                GameObject hornet = GameObject.Find("Hero_Hornet(Clone)");
                Logger.LogInfo($"Hornet positon at {hornet.transform.position}");

                lace.transform.position = new Vector3(81.9569f, 106.1943f, 2.7021f);
                lace.transform.SetScaleX(-0.9f);
                lace.transform.SetScaleY(0.9f);
                lace.transform.SetScaleZ(0.9f);
                Logger.LogInfo($"Setting lace position at {lace.transform.position}");


                PlayMakerFSM laceFSM = (PlayMakerFSM)lace.GetComponent(typeof(PlayMakerFSM));
                Logger.LogInfo($"FSM: {laceFSM.name}");
                Logger.LogInfo($"FSM states {laceFSM.FsmStates.Length}");

                //transitions["Dormant"]["ENTER"].ToState = states["Sit Antic"].Name;
                //transitions["Dormant"]["ENTER"].ToFsmState = states["Sit Antic"];

                laceFSM.ChangeTransition("Take Control", "LAND", "Sit Up");
                laceFSM.ChangeTransition("Take Control", "LAND", "Sit Up");
                laceFSM.GetTransition("Take Control", "LAND").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");
                laceFSM.DisableAction("Take Control", 3);
                
                Wait w2 = new Wait();
                w2.time = 2f;
                laceFSM.DisableAction("Sit Up", 4);
                laceFSM.AddAction("Sit Up", w2);

                SetPosition laceTargetPos = laceFSM.GetAction<SetPosition>("Sit Up", 3);

                laceTargetPos.vector = new Vector3(81.9569f, 106.6942f, 2.7021f);
                laceTargetPos.x = 81.9569f;
                laceTargetPos.y = 106.6942f;
                laceTargetPos.z = 2.7021f;

                laceFSM.ChangeTransition("Sit Up", "FINISHED", "Jump Antic");
                
                Logger.LogInfo("Setting actions to give back hornet control");
                SendMessage message_control_idle = new SendMessage();
                FunctionCall fc_control_idle = new FunctionCall();
                fc_control_idle.FunctionName = "StartControlToIdle";
                
                message_control_idle.functionCall = fc_control_idle;
                message_control_idle.gameObject = SilkenSisters.hornetFSMOwner;
                message_control_idle.options = SendMessageOptions.DontRequireReceiver;

                SendMessage message_control_regain = new SendMessage();
                FunctionCall fc_control_regain = new FunctionCall();
                fc_control_regain.FunctionName = "RegainControl";
                message_control_regain.functionCall = fc_control_regain;
                message_control_regain.gameObject = SilkenSisters.hornetFSMOwner;
                message_control_regain.options = SendMessageOptions.DontRequireReceiver;

                laceFSM.AddAction("Jump Away", message_control_regain);
                laceFSM.AddAction("Jump Away", message_control_idle);

                Logger.LogInfo($"Unloading {scene.name} scene");
                SceneManager.UnloadScene(scene.name);
                Logger.LogInfo($"Unloading bundle {bundle.name}");
                bundle.Unload(false);

            };
        }

        private void spawnLaceBoss2()
        {
            // Spawn pos : 78,7832 104,5677 0,004
            // Constraints left: 72,4, right: 96,52, bot: 104
            Scene cur_scene = SceneManager.GetActiveScene();
            Logger.LogInfo($"Current scene {SceneManager.GetActiveScene().name}");
            Logger.LogInfo("Loading song tower scene");

            // Needed to get her loaded in the scene
            bool has_beaten_lace = PlayerData.instance.defeatedLaceTower;
            Logger.LogInfo($"Has beaten lace? {has_beaten_lace}, saving for later");
            PlayerData.instance.defeatedLaceTower = false;

            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "scenes_scenes_scenes", "song_tower_01.bundle"));
            AssetBundle laceboss_bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "localpoolprefabs_assets_laceboss.bundle"));
            AsyncOperation op = SceneManager.LoadSceneAsync("Song_Tower_01", LoadSceneMode.Additive);

            op.completed += (AsyncOperation obj) =>
            {
                Scene scene = SceneManager.GetSceneByName("Song_Tower_01");

                Logger.LogInfo($"Scene {scene.name} loaded");


                GameObject go = null;
                foreach (GameObject sgo in scene.GetRootGameObjects())
                {
                    if (sgo.name == "Boss Scene")
                    {
                        go = sgo;
                    }
                }

                //GameObject go = GameObject.Find("Boss Scene");
                Logger.LogInfo($"Trying to copy {go.name}");
                GameObject laceBossScene = Instantiate(go);

                Logger.LogInfo($"Moving {laceBossScene.name} to {cur_scene.name}");
                SceneManager.MoveGameObjectToScene(laceBossScene, cur_scene);

                Logger.LogInfo($"Trying to find Lace Boss from scene {laceBossScene.gameObject.name}");
                GameObject lace = GameObject.Find($"{laceBossScene.gameObject.name}/Lace Boss2 New");
                Logger.LogInfo($"Lace object: {lace}");

                Logger.LogInfo($"Disabling unwanted LaceBossScene items");
                GameObject.Find($"{laceBossScene.gameObject.name}/Flower Effect Hornet").SetActive(false);
                GameObject.Find($"{laceBossScene.gameObject.name}/Slam Particles").SetActive(false);
                GameObject.Find($"{laceBossScene.gameObject.name}/steam hazard").SetActive(false);
                GameObject.Find($"{laceBossScene.gameObject.name}/Silk Heart Memory Return").SetActive(false);

                GameObject.Find($"{laceBossScene.gameObject.name}/{lace.gameObject.name}/Pt DashPetal").SetActive(false);
                GameObject.Find($"{laceBossScene.gameObject.name}/{lace.gameObject.name}/Pt SkidPetal").SetActive(false);
                GameObject.Find($"{laceBossScene.gameObject.name}/{lace.gameObject.name}/Pt RisingPetal").SetActive(false);
                GameObject.Find($"{laceBossScene.gameObject.name}/{lace.gameObject.name}/Pt MovePetal").SetActive(false);

                Logger.LogInfo($"Moving lace arena objects");
                GameObject.Find($"{laceBossScene.gameObject.name}/Arena L").transform.position = new Vector3(72f, 104f, 0f);
                GameObject.Find($"{laceBossScene.gameObject.name}/Arena R").transform.position = new Vector3(97f, 104f, 0f);
                GameObject.Find($"{laceBossScene.gameObject.name}/Centre").transform.position = new Vector3(84.5f, 104f, 0f);

                SilkenSisters.laceBoss = lace;
                SilkenSisters.laceBossFSMOwner = new FsmOwnerDefault();
                SilkenSisters.laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                SilkenSisters.laceBossFSMOwner.GameObject = SilkenSisters.laceBoss;

                // Disabling the check so that we don't need to track it further
                Logger.LogInfo($"Disabling defeated check");
                DeactivateIfPlayerdataTrue comp = (DeactivateIfPlayerdataTrue)lace.GetComponent(typeof(DeactivateIfPlayerdataTrue));
                comp.enabled = false;
                PlayerData.instance.defeatedLaceTower = has_beaten_lace; // Putting back the value

                ConstrainPosition laceBossConstraint = (ConstrainPosition)lace.GetComponent(typeof(ConstrainPosition));
                laceBossConstraint.SetXMin(72.4f);
                laceBossConstraint.SetXMax(96.52f);
                laceBossConstraint.SetYMin(104f);
                laceBossConstraint.constrainX = true;
                laceBossConstraint.constrainY = true;

                // Finite state machine edition
                PlayMakerFSM laceBossFSM = FsmUtil.GetFsmPreprocessed(laceBoss, "Control");
                Logger.LogInfo(laceBossFSM.FsmName);

                // Reroute states
                Logger.LogInfo("Rerouting states");
                laceBossFSM.ChangeTransition("Init", "REFIGHT", "Start Battle Wait");
                laceBossFSM.ChangeTransition("Start Battle Wait", "BATTLE START REFIGHT", "Refight Engarde");
                laceBossFSM.ChangeTransition("Start Battle Wait", "BATTLE START FIRST", "Refight Engarde");

                // Lengthen the engarde state
                Wait wait_engarde = new Wait();
                wait_engarde.time = 1f;
                Logger.LogInfo("Increase engarde time");
                laceBossFSM.AddAction("Refight Engarde", wait_engarde);
                

                // Change floor height
                Logger.LogInfo("Fix floor heights");
                laceBossFSM.GetAction<SetPosition2d>("ComboSlash 1", 0).y = 104.5677f;
                laceBossFSM.GetAction<SetPosition2d>("Charge Antic", 2).y = 104.5677f;
                laceBossFSM.GetAction<SetPosition2d>("Counter Antic", 1).y = 104.5677f;

                Logger.LogInfo("Fixing Counter Teleport Height");
                laceBossFSM.GetAction<SetPosition>("Counter TeleIn", 4).y = 110f;

                // Disable lace's title card
                Logger.LogInfo("Disabling title card");
                laceBossFSM.DisableAction("Start Battle Refight", 4);
                laceBossFSM.DisableAction("Start Battle", 4);

                laceBossFSM.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue = 73f;
                laceBossFSM.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue = 96f;

                laceBossFSM.FindFloatVariable("Land Y").Value = 104.5677f;
                laceBossFSM.FindFloatVariable("Arena Plat Bot Y").Value = 102f;

                //lace.transform.position = hornet.transform.position + new Vector3(3, 0, 0);
                lace.transform.position = new Vector3(78.3832f, 104.5677f, 0.004f);
                lace.transform.SetScaleX(1);
                Logger.LogInfo($"Setting lace position at {lace.transform.position}");

                SilkenSisters.laceBoss2Active = true;
                lace.SetActive(false);

                Logger.LogInfo($"Unloading {scene.name} scene");
                SceneManager.UnloadScene(scene.name);
                Logger.LogInfo($"Unloading bundle {bundle.name}");
                bundle.Unload(false);

            };
        }

        private void reset()
        {

            Logger.LogInfo("Reseting variables");

            SilkenSisters.laceNPC = null;
            SilkenSisters.laceNPCFSMOwner = null;

            SilkenSisters.laceBoss = null;
            SilkenSisters.laceBossFSMOwner = null;

            SilkenSisters.phantomBoss = null;
            SilkenSisters.phantomBossScene = null;
            SilkenSisters.phantomBossSceneFSMOwner = null;
        }

        private void Update()
        {
            if (SilkenSisters.phantomBossScene == null && SceneManager.GetActiveScene().name == "Organ_01")
            {
                SilkenSisters.phantomBossScene = GameObject.Find("Boss Scene");
                if (SilkenSisters.phantomBossScene != null)
                {
                    SilkenSisters.phantomBossSceneFSMOwner = new FsmOwnerDefault();
                    SilkenSisters.phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                    SilkenSisters.phantomBossSceneFSMOwner.GameObject = SilkenSisters.phantomBossScene;
                }
            }

            if (SilkenSisters.laceBoss != null && SceneManager.GetActiveScene().name == "Organ_01")
            {
                PlayMakerFSM laceBossFSM = SilkenSisters.laceBoss.GetComponents<PlayMakerFSM>().First(fsm => fsm.FsmName == "Control");
                //Logger.LogInfo($"Dstab angle : {laceBossFSM.FindFloatVariable("Angle").Value}, Min angle {laceBossFSM.FindFloatVariable("Angle Max").Value}, Max angle {laceBossFSM.FindFloatVariable("Angle Max").Value}");
            }

            if (SilkenSisters.phantomBoss == null && SceneManager.GetActiveScene().name == "Organ_01")
            {
                SilkenSisters.phantomBoss = GameObject.Find("Boss Scene/Phantom");
            }

            if (SilkenSisters.hornet == null)
            {
                SilkenSisters.hornet = GameObject.Find("Hero_Hornet(Clone)");
                if (SilkenSisters.hornet != null)
                {
                    SilkenSisters.hornetFSMOwner = new FsmOwnerDefault();
                    SilkenSisters.hornetFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                    SilkenSisters.hornetFSMOwner.GameObject = SilkenSisters.hornet;
                }
            }

            // ------------------------------------------------------------------

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.O))
            {
                spawnLaceBoss2();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.U))
            {
                spawnLaceNpc();
                spawnLaceBoss2();
                spawnChallengeSequence();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.P))
            {
                setupPhantom();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.H))
            {
                SilkenSisters.hornet.transform.position = new Vector3(114f, 105f, 0.004f);
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.L))
            {
                reset();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                SilkenSisters.laceBoss.SetActive(true);
                ((PlayMakerFSM)SilkenSisters.laceBoss.GetComponent(typeof(PlayMakerFSM))).SendEvent("BATTLE START FIRST");
                SilkenSisters.laceBoss.GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                SilkenSisters.phantomBoss.GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad2))
            {
                toggleLaceFSM();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.K))
            {
                PlayMakerFSM.BroadcastEvent("ENTER");
                ((PlayMakerFSM)SilkenSisters.phantomBossScene.GetComponent(typeof(PlayMakerFSM))).SendEvent("ENTER");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.G))
            {
                PlayMakerFSM phantomBossFSM = FsmUtil.GetFsmPreprocessed(SilkenSisters.phantomBoss, "Control");
                Logger.LogInfo("Switching dragoon time");
                if (phantomDragoonToggle)
                {
                    Logger.LogInfo("Slow dragoon");
                    phantomBossFSM.GetAction<Wait>("Dragoon Away", 3).time = 0.25f;
                    phantomBossFSM.FindFloatVariable("Dragoon Drop Time").Value = 0.9f;

                }
                else
                {
                    Logger.LogInfo("Fast dragoon");
                    phantomBossFSM.GetAction<Wait>("Dragoon Away", 3).time = 0.15f;
                    phantomBossFSM.FindFloatVariable("Dragoon Drop Time").Value = 0.4f;
                }
                phantomDragoonToggle = !phantomDragoonToggle;
            }
        }
    }

    // Title cards
    [HarmonyPatch(typeof(Language), "Get")]
    [HarmonyPatch(new[] { typeof(string), typeof(string) })]
    public static class Language_Get_Patch
    {
        private static void Postfix(string key, string sheetTitle, ref string __result)
        {
            if (key == "SILKEN_SISTERS_SUPER") __result = "Silken Sisters";
            if (key == "SILKEN_SISTERS_MAIN") __result = "Phantom & Lace";
            if (key == "SILKEN_SISTERS_SUB") __result = "";
        }
    }

}
