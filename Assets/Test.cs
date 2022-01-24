using System.Collections;
using System.Collections.Generic;
using Assets.Script.ResManager;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button btn;

    public Button unLoad;

    public GameObject o;
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
        var g = ResManager.Inst.Load<GameObject>("Cube.prefab");
        o = GameObject.Instantiate(g);
    }

}
