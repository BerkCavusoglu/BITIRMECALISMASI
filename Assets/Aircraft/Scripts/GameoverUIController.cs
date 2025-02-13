using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Aircraft
{
    public class GameoverUIController : MonoBehaviour
    {
        [Tooltip("Bitirme sırasını göstermek için metin (örneğin 2. sırada)")]
        public TextMeshProUGUI placeText;

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.GameState == GameState.Gameover)
            {
                // Sıralamayı alır ve metni günceller
                string place = raceManager.GetAgentPlace(raceManager.FollowAgent);
                this.placeText.text = place + " Sıra";
            }
        }

        /// <summary>
        /// Ana Menü sahnesini yükler
        /// </summary>
        public void MainMenuButtonClicked()
        {
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
        }
    }
}
