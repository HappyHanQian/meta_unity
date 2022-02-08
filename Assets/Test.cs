using System.Collections;
using System.Collections.Generic;
using Assets.Script.ResManager;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button btn;

    public Button unLoad;

    public GameObject o;

    private bool load = true;
    // Start is called before the first frame update
    void Start()
    {
        btn.onClick.AddListener(OnClick);
        unLoad.onClick.AddListener(OnClickUnLoad);
    }

    private void OnClickUnLoad()
    {
        GameObject.Destroy(o);
        o = null;
        Resources.UnloadUnusedAssets();
    }

    private void OnClick()
    {
        if (load)
        {
            ResManager.Inst.LoadAsync<GameObject>("Cube.prefab", delegate (GameObject g)
            {
                o = GameObject.Instantiate(g);
            });
        }
        else
        {
            ResManager.Inst.StopLoad("Cube.prefab");
        }
        load = !load;
    }

}
