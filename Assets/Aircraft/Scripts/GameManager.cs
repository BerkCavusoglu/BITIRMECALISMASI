using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aircraft
{
    public enum GameState
    {
        Default,    // Varsayılan
        MainMenu,   // Ana Menü
        Preparing,  // Hazırlık
        Playing,    // Oynanıyor
        Paused,     // Duraklatıldı
        Gameover    // Oyun Sonu
    }

    public enum GameDifficulty
    {
        Normal,     // Normal
        Hard        // Zor
    }

    public delegate void OnStateChangeHandler();

    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Oyun durumu değiştiğinde çağrılır
        /// </summary>
        public event OnStateChangeHandler OnStateChange;

        private GameState gameState;

        /// <summary>
        /// Mevcut oyun durumu
        /// </summary>
        public GameState GameState
        {
            get
            {
                return gameState;
            }

            set
            {
                gameState = value;
                if (OnStateChange != null) OnStateChange();
            }
        }

        public GameDifficulty GameDifficulty { get; set; }

        /// <summary>
        /// Singleton GameManager örneği
        /// </summary>
        public static GameManager Instance
        {
            get; private set;
        }

        /// <summary>
        /// Singleton'ı yönetir ve tam ekran çözünürlüğünü ayarlar
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnApplicationQuit()
        {
            Instance = null;
        }

        /// <summary>
        /// Yeni bir seviye yükler ve oyun durumunu ayarlar
        /// </summary>
        /// <param name="levelName">Yüklenecek seviye adı</param>
        /// <param name="newState">Yeni oyun durumu</param>
        public void LoadLevel(string levelName, GameState newState)
        {
            StartCoroutine(LoadLevelAsync(levelName, newState));
        }

        private IEnumerator LoadLevelAsync(string levelName, GameState newState)
        {
            // Yeni seviyeyi yükle
            AsyncOperation operation = SceneManager.LoadSceneAsync(levelName);
            while (operation.isDone == false)
            {
                yield return null;
            }

            // Çözünürlüğü ayarla
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);

            // Oyun durumunu güncelle
            GameState = newState;
        }
    }
}

