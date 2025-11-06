using HutongGames.PlayMaker.Actions;
using SilkenSisters.SceneManagement;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

namespace SilkenSisters.Behaviors
{
    internal class Lace2 : MonoBehaviour
    {

        // Spawn pos : 78,7832 104,5677 0,004
        // Constraints left: 72,4, right: 96,52, bot: 104
        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            getComponents();
            disableParticleEffects();
            editPositionConstraint();
            rerouteState();
            fixActionsPositions();
            disableTitleCard();
            fixWallRangeAlert();
            setLaceFacing();
        }

        private void getComponents()
        {
            gameObject.transform.position = new Vector3(78.2832f, 104.5677f, 0.004f);
            SilkenSisters.Log.LogInfo($"Setting lace position at {gameObject.transform.position}");
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableParticleEffects()
        {
            SceneObjectManager.findChildObject(gameObject, "Pt DashPetal").SetActive(false);
            SceneObjectManager.findChildObject(gameObject, "Pt SkidPetal").SetActive(false);
            SceneObjectManager.findChildObject(gameObject, "Pt RisingPetal").SetActive(false);
            SceneObjectManager.findChildObject(gameObject, "Pt MovePetal").SetActive(false);
        }

        private void editPositionConstraint()
        {
            ConstrainPosition laceBossConstraint = (ConstrainPosition)gameObject.GetComponent(typeof(ConstrainPosition));
            laceBossConstraint.SetXMin(72.4f);
            laceBossConstraint.SetXMax(96.52f);
            laceBossConstraint.SetYMin(104f);
            laceBossConstraint.constrainX = true;
            laceBossConstraint.constrainY = true;
        }

        private void rerouteState()
        {
            SilkenSisters.Log.LogInfo("Rerouting states");
            _control.ChangeTransition("Init", "REFIGHT", "Start Battle Wait");
            _control.ChangeTransition("Start Battle Wait", "BATTLE START REFIGHT", "Refight Engarde");
            _control.ChangeTransition("Start Battle Wait", "BATTLE START FIRST", "Refight Engarde");

            // Lengthen the engarde state
            Wait wait_engarde = new Wait();
            wait_engarde.time = 2f;
            SilkenSisters.Log.LogInfo("Increase engarde time");
            _control.AddAction("Refight Engarde", wait_engarde);

        }

        private void fixActionsPositions()
        {
            // Change floor height
            SilkenSisters.Log.LogInfo("Fix floor heights");
            _control.GetAction<SetPosition2d>("ComboSlash 1", 0).y = 104.5677f;
            _control.GetAction<SetPosition2d>("Charge Antic", 2).y = 104.5677f;
            _control.GetAction<SetPosition2d>("Counter Antic", 1).y = 104.5677f;

            SilkenSisters.Log.LogInfo("Fixing Counter Teleport");
            _control.GetAction<SetPosition>("Counter TeleIn", 4).y = 110f;

            FloatClamp clamp_pos = new FloatClamp();
            clamp_pos.floatVariable = _control.FindFloatVariable("Tele X");
            clamp_pos.maxValue = 96f;
            clamp_pos.minValue = 73f;

            _control.InsertAction("Counter TeleIn", clamp_pos, 4);

            // -----
            _control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue = 73f;
            _control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue = 96f;

            _control.FindFloatVariable("Land Y").Value = 104.5677f;
            _control.FindFloatVariable("Arena Plat Bot Y").Value = 102f;
            _control.FindFloatVariable("Centre X").Value = 84f;

            // -----
            _control.GetAction<CheckXPosition>("Force R?", 2).compareTo = 73f;
            _control.GetAction<CheckXPosition>("Force L?", 1).compareTo = 96f;

            _control.FindFloatVariable("Bomb Max X").Value = 96f;
            _control.FindFloatVariable("Bomb Min X").Value = 72f;
            _control.FindFloatVariable("Bomb Max Y").Value = 115f;
            _control.FindFloatVariable("Bomb Min Y").Value = 105f;

        }

        private void disableTitleCard()
        {
            SilkenSisters.Log.LogInfo("Disabling title card");
            _control.DisableAction("Start Battle Refight", 4);
            _control.DisableAction("Start Battle", 4);
        }

        private void fixWallRangeAlert()
        {
            GameObject wallRange = SceneObjectManager.findChildObject(gameObject.transform.parent.gameObject, "Wall Range");
            wallRange.transform.SetPosition3D(84.0349f, 103.67f, 0f);

            BoxCollider2D[] boxes = wallRange.GetComponents<BoxCollider2D>();
            boxes[0].size = new Vector2(5f, 30f);
            boxes[0].offset = new Vector2(-9f, 0.4726f);

            boxes[1].size = new Vector2(5f, 35.1782f);
            boxes[1].offset = new Vector2(10f, 7.1234f);

            ClampPosition clamp_j_pos = new ClampPosition();
            clamp_j_pos.minX = 80f;
            clamp_j_pos.gameObject = SilkenSisters.plugin.laceBossFSMOwner;
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
        }

    }
}
