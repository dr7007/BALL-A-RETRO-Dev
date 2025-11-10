using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Collections;

public class KHS_Script_UIImgFunc : MonoBehaviour
{
    [Header("Round 이미지 연출 관련")]
    [Tooltip("Round 이미지 오브젝트 연결")]
    [SerializeField]
    private GameObject[] roundImgGos;
    private Material[] images;

    void Start()
    {
        List<GameObject> childObjects = new List<GameObject>();
        List<Material> ImagesMaterials = new List<Material>();

        foreach (Transform child in transform)
        {
            childObjects.Add(child.gameObject);
            ImagesMaterials.Add(child.GetComponent<Image>().material);
            child.gameObject.SetActive(false);
        }

        images = ImagesMaterials.ToArray();
        roundImgGos = childObjects.ToArray();
        
        foreach(Material mat in images)
        {
            mat.SetFloat("_Progress", 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartRoundFunc(int _round)
    {
        StartCoroutine(StartRoundCoroutine(_round));
    }

    private IEnumerator StartRoundCoroutine(int _round)
    {
        roundImgGos[_round].SetActive(true);
        Material mat = images[_round];
        float progress = 0f;
        if (progress < 1f)
        {
            progress += Time.unscaledDeltaTime;
            mat.SetFloat("_Progress", progress);
            yield return null;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(StartCloseCoroutine(_round));
        }
    }
    private IEnumerator StartCloseCoroutine(int _round)
    {
        roundImgGos[_round].SetActive(true);
        Material mat = images[_round];
        float progress = 1f;
        if (progress > 0f)
        {
            progress -= Time.unscaledDeltaTime;
            mat.SetFloat("_Progress", progress);
            yield return null;
        }
        else
        {
            roundImgGos[_round].SetActive(false);
        }
    }
}
