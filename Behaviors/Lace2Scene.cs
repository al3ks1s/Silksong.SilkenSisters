using SilkenSisters.SceneManagement;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class Lace2Scene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            getComponents();
            disableSceneObjects();
            moveSceneBounds();
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableSceneObjects()
        {
            SilkenSisters.Log.LogInfo($"Disabling unwanted LaceBossScene items");
            SceneObjectManager.findChildObject(gameObject, "Flower Effect Hornet").SetActive(false);
            //SceneObjectManager.findChildObject(gameObject, "Slam Particles").SetActive(false);
            SceneObjectManager.findChildObject(gameObject, "steam hazard").SetActive(false);
            SceneObjectManager.findChildObject(gameObject, "Silk Heart Memory Return").SetActive(false);
        }

        private void moveSceneBounds()
        {
            SilkenSisters.Log.LogInfo($"Moving lace arena objects");
            SceneObjectManager.findChildObject(gameObject, "Arena L").transform.position = new Vector3(72f, 104f, 0f);
            SceneObjectManager.findChildObject(gameObject, "Arena R").transform.position = new Vector3(97f, 104f, 0f);
            SceneObjectManager.findChildObject(gameObject, "Centre").transform.position = new Vector3(84.5f, 104f, 0f);
        }



    }
}
