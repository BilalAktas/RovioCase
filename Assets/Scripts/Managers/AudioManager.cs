using UnityEngine;

namespace Core
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private float _minInterval = 0.05f;
        [SerializeField] private float _maxPitch = 1.5f;
        [SerializeField] private float _rampSpeed = 4f; 
        [SerializeField] private float _decaySpeed = 2f; 

        private float _lastPlayTime;
        private float _currentIntensity;
    
        [SerializeField] private AudioSource _collectSource;

        private void Start()
        {
            EventBus.Subscribe<OnCollectCubeEvent>(PlayCollectSound);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnCollectCubeEvent>(PlayCollectSound);
        }

        public void PlayCollectSound(OnCollectCubeEvent data)
        {
            if (Time.time - _lastPlayTime < _minInterval)
                return;
            
            _currentIntensity += _rampSpeed * Time.deltaTime;
            _currentIntensity = Mathf.Clamp01(_currentIntensity);

            if (!_collectSource.isPlaying)
                _collectSource.Play();

            _lastPlayTime = Time.time;
        }

        private void Update()
        {
            _currentIntensity = Mathf.MoveTowards(_currentIntensity, 0f, _decaySpeed * Time.deltaTime);
            var targetPitch = Mathf.Lerp(1f, _maxPitch, _currentIntensity);
            _collectSource.pitch = targetPitch;
        }
    }
}
