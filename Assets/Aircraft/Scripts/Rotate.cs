using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class RotateObject : MonoBehaviour
    {
        [Tooltip("Dönme hızı")]
        public Vector3 rotationSpeed;

        [Tooltip("Başlangıç pozisyonunu rastgele yapıp yapmamayı seç")]
        public bool randomizeStartPosition = false;

        private void Start()
        {
            // Başlangıç pozisyonunu rastgele yap
            if (randomizeStartPosition)
            {
                float randomAngle = UnityEngine.Random.Range(0f, 360f);
                transform.Rotate(rotationSpeed.normalized * randomAngle);
            }
        }

        void Update()
        {
            // Nesneyi belirli bir hızda döndür
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
