using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aircraft
{
    public class HUDController : MonoBehaviour
    {
        [Tooltip("Yarıştaki sıralama (örneğin 1.)")]
        public TextMeshProUGUI placeText;

        [Tooltip("Bir sonraki kontrol noktasına ulaşmak için kalan saniyeler (örneğin Time 9.3)")]
        public TextMeshProUGUI timeText;

        [Tooltip("Mevcut tur (örneğin Lap 2)")]
        public TextMeshProUGUI lapText;

        [Tooltip("Bir sonraki kontrol noktasını gösteren ikon")]
        public Image checkpointIcon;

        [Tooltip("Bir sonraki kontrol noktasına doğru işaret eden ok")]
        public Image checkpointArrow;

        [Tooltip("Kontrol noktasına işaret eden ok yerine, ikonun ortalanmış olarak gösterileceği limit")]
        public float indicatorLimit = .7f;

        /// <summary>
        /// Bu HUD'nin gösterdiği ajan
        /// </summary>
        public AircraftAgent FollowAgent { get; set; }

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void Update()
        {
            if (FollowAgent != null)
            {
                UpdatePlaceText();
                UpdateTimeText();
                UpdateLapText();
                UpdateArrow();
            }
        }

        private void UpdatePlaceText()
        {
            string place = raceManager.GetAgentPlace(FollowAgent);
            placeText.text = place;
        }

        private void UpdateTimeText()
        {
            float time = raceManager.GetAgentTime(FollowAgent);
            timeText.text = "Time " + time.ToString("0.0");
        }

        private void UpdateLapText()
        {
            int lap = raceManager.GetAgentLap(FollowAgent);
            lapText.text = "Lap " + lap + "/" + raceManager.numLaps;
        }

        private void UpdateArrow()
        {
            // Görüş alanındaki kontrol noktasını bul
            Transform nextCheckpoint = raceManager.GetAgentNextCheckpoint(FollowAgent);
            Vector3 viewportPoint = raceManager.ActiveCamera.WorldToViewportPoint(nextCheckpoint.transform.position);
            bool behindCamera = viewportPoint.z < 0;
            viewportPoint.z = 0f;

            // Konum hesaplamaları yap
            Vector3 viewportCenter = new Vector3(.5f, .5f, 0f);
            Vector3 fromCenter = viewportPoint - viewportCenter;
            float halfLimit = indicatorLimit / 2f;
            bool showArrow = false;

            if (behindCamera)
            {
                // Merkezden uzaklık limitini belirle
                // (Nesne kameranın arkasında olduğunda viewport noktası ters döner)
                fromCenter = -fromCenter.normalized * halfLimit;
                showArrow = true;
            }
            else
            {
                if (fromCenter.magnitude > halfLimit)
                {
                    // Merkezden uzaklık limitini belirle
                    fromCenter = fromCenter.normalized * halfLimit;
                    showArrow = true;
                }
            }

            // Kontrol noktası ikonunu ve okunu güncelle
            checkpointArrow.gameObject.SetActive(showArrow);
            checkpointArrow.rectTransform.rotation = Quaternion.FromToRotation(Vector3.up, fromCenter);
            checkpointIcon.rectTransform.position = raceManager.ActiveCamera.ViewportToScreenPoint(fromCenter + viewportCenter);
        }
    }
}
