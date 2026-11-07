using UnityEngine;
namespace PSH
{
    // Observe navigation without loading or saving the developer's open scene.
    public class PSH_Script_SceneLoader : MonoBehaviour
    {
        public static PSH_Script_SceneLoader Instance { get; private set; }
        public string Destination;public int Requests;
        void Awake(){Instance=this;}
        public void LoadSceneAsyncByName(string name,bool showLoadingPanel=true){Destination=name;Requests++;}
    }
}
