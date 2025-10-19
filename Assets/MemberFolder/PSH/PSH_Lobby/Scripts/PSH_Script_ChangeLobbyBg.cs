using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace PSH
{
    public class ChangeLobbyBg : MonoBehaviour
    {
        [SerializeField] private Image lobbyBg;
        [SerializeField] private float _duration;
        private Coroutine _runningFadeCoroutine;
        
        public void Awake() 
        {
            if(lobbyBg == null)
            {
                this.enabled = false;
            }
        }

        public void Start()
        {
            FadeToBlack(_duration);
        }

        public void FadeToBlack(float _duration)
        {
            StartFade(Color.black, _duration);
        }

        public void FadeToWhite(float _duration)
        {
            StartFade(Color.white, _duration);
        }

        private void StartFade(Color _tagetColor, float _duration )
        {
            if(_runningFadeCoroutine !=null)
            {
                StopCoroutine(_runningFadeCoroutine);
            }
            _runningFadeCoroutine = StartCoroutine(FadeCoroutine(_tagetColor,_duration));
        }
        
        private IEnumerator FadeCoroutine(Color _tagetColor, float _duration)
        {
            Color startColor = lobbyBg.color;
            float elapsedTime = 0f;

            while(elapsedTime <_duration)
            {
                elapsedTime += Time.deltaTime;
                float prograss = Mathf.Clamp01(elapsedTime / _duration);
                lobbyBg.color = Color.Lerp(startColor,_tagetColor, prograss);
                yield return null;
            }
            lobbyBg.color = _tagetColor;
            _runningFadeCoroutine = null;
        }


    }
}
