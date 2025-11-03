// SimpleRotate.cs
using UnityEngine;
public class YJ_Script_RouletteBody : MonoBehaviour
{
    [SerializeField]
    private float spinSpeed = 30f;
    private void Update()
    {
        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
    }
}