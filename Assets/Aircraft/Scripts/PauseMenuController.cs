using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class PauseMenuController : MonoBehaviour
    {
        // Başlangıçta GameManager'ın durum değişiklikleri
        private void Start()
        {
            GameManager.Instance.OnStateChange += HandleStateChange;
        }

        // Oyun durumuna göre menü görünürlüğünü ayarlayan fonksiyon
        private void HandleStateChange()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                // Oyun oynanıyorsa pause menüsünü gizle
                gameObject.SetActive(false);
            }
        }

        // Devam butonuna tıklanması durumu
        public void OnResumeButtonClicked()
        {
            // Oyun durumunu oynanıyor olarak değiştir
            GameManager.Instance.GameState = GameState.Playing;
        }

        // Ana menüye dönüş butonuna tıklanması durumu
        public void OnMainMenuButtonClicked()
        {
            // Ana menüyü yükle
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
        }

        // Nesne yok edilirken abone olunan olayları temizle
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChange -= HandleStateChange;
        }
    }
}
