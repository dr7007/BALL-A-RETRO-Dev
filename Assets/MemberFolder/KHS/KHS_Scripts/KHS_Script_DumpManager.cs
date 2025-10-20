using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KHS_Script_DumpManager : MonoBehaviour
{
    private HashSet<Collider> triggeredBalls = new HashSet<Collider>();

    public static event Action<Collider> OnBallTrigger;
    public static event Action<Collision> OnBallCollision;

    private void OnTriggerEnter(Collider _collider)
    {
        //if(_collider.CompareTag("Ball"))
        //    OnBallTrigger?.Invoke(_collider);
        if (_collider.CompareTag("Ball") && !triggeredBalls.Contains(_collider))
        {
            triggeredBalls.Add(_collider);
            OnBallTrigger?.Invoke(_collider);
            StartCoroutine(ReleaseBall(_collider));
        }
    }
    private void OnCollisionEnter(Collision _collision)
    {
        if(_collision.gameObject.name =="Ball")
            OnBallCollision?.Invoke(_collision);
    }
    private IEnumerator ReleaseBall(Collider col)
    {
        yield return new WaitForSeconds(4.5f);
        triggeredBalls.Remove(col);
    }
}
