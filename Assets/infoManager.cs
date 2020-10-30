using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class infoManager : MonoBehaviour {


    public GameObject infoObj;

    public void OnShowInfo()
    {
        infoObj.SetActive(!infoObj.activeSelf);
    }
}
