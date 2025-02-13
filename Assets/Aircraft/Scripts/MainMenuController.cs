using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Aircraft
{
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Yüklenebilecek seviyelerin (sahne adlarının) listesi")]
        public List<string> levels;

        [Tooltip("Seviye seçmek için açılır menü")]
        public TMP_Dropdown levelDropdown;

        [Tooltip("Oyun zorluk seviyesini seçmek için açılır menü")]
        public TMP_Dropdown difficultyDropdown;

        private string selectedLevel;
        private GameDifficulty selectedDifficulty;

        // Başlangıçta seviyeleri ve zorluk seçeneklerini ayarla
        private void Start()
        {
            Debug.Assert(levels.Count > 0, "Hiç seviye yok");
            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(levels);
            selectedLevel = levels[0];

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(Enum.GetNames(typeof(GameDifficulty)).ToList());
            selectedDifficulty = GameDifficulty.Normal;
        }

        // Seçilen seviyeyi ayarla
        public void SetLevel(int levelIndex)
        {
            selectedLevel = levels[levelIndex];
        }

        // Seçilen zorluk seviyesini ayarla
        public void SetDifficulty(int difficultyIndex)
        {
            selectedDifficulty = (GameDifficulty)difficultyIndex;
        }

        // Başlat butonuna tıklanınca
        public void StartButtonClicked()
        {
            // Oyun zorluk seviyesini ayarla
            GameManager.Instance.GameDifficulty = selectedDifficulty;

            // Seçilen seviyeyi 'Hazırlanıyor' durumunda yükle
            GameManager.Instance.LoadLevel(selectedLevel, GameState.Preparing);
        }

        // Çıkış butonuna tıklanınca
        public void QuitButtonClicked()
        {
            Application.Quit();
        }
    }
}
