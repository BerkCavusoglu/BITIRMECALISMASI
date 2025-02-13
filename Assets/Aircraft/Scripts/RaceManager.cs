using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class RaceManager : MonoBehaviour
    {
        [Tooltip("Yarış için tur sayısı")]
        public int numLaps = 2;

        [Tooltip("Kontrol noktasına ulaşıldığında verilecek bonus saniyeler")]
        public float checkpointBonusTime = 15f;

        [Serializable]
        public struct DifficultyModel
        {
            public GameDifficulty difficulty;
            public NNModel model;
        }

        public List<DifficultyModel> difficultyModels;

        public AircraftAgent FollowAgent { get; private set; }

        public Camera ActiveCamera { get; private set; }

        private CinemachineVirtualCamera virtualCamera;
        private CountdownUIController countdownUI;
        private PauseMenuController pauseMenu;
        private HUDController hud;
        private GameoverUIController gameoverUI;
        private AircraftArea aircraftArea;
        private AircraftPlayer aircraftPlayer;
        private List<AircraftAgent> sortedAircraftAgents;


        private float lastResumeTime = 0f;
        private float previouslyElapsedTime = 0f;

        private float lastPlaceUpdate = 0f;
        private Dictionary<AircraftAgent, AircraftStatus> aircraftStatuses;
        private class AircraftStatus
        {
            public int checkpointIndex = 0;
            public int lap = 0;
            public int place = 0;
            public float timeRemaining = 0f;
        }

        public float RaceTime
        {
            get
            {
                if (GameManager.Instance.GameState == GameState.Playing)
                {
                    return previouslyElapsedTime + Time.time - lastResumeTime;
                }
                else if (GameManager.Instance.GameState == GameState.Paused)
                {
                    return previouslyElapsedTime;
                }
                else
                {
                    return 0f;
                }
            }
        }


        public Transform GetAgentNextCheckpoint(AircraftAgent agent)
        {
            return aircraftArea.Checkpoints[aircraftStatuses[agent].checkpointIndex].transform;
        }


        public int GetAgentLap(AircraftAgent agent)
        {
            return aircraftStatuses[agent].lap;
        }


        public string GetAgentPlace(AircraftAgent agent)
        {
            int place = aircraftStatuses[agent].place;
            if (place <= 0)
            {
                return string.Empty;
            }

            if (place >= 11 && place <= 13) return place.ToString() + "th";

            switch (place % 10)
            {
                case 1:
                    return place.ToString() + "st";
                case 2:
                    return place.ToString() + "nd";
                case 3:
                    return place.ToString() + "rd";
                default:
                    return place.ToString() + "th";
            }
        }

        public float GetAgentTime(AircraftAgent agent)
        {
            return aircraftStatuses[agent].timeRemaining;
        }

        private void Awake()
        {
            hud = FindObjectOfType<HUDController>();
            countdownUI = FindObjectOfType<CountdownUIController>();
            pauseMenu = FindObjectOfType<PauseMenuController>();
            gameoverUI = FindObjectOfType<GameoverUIController>();
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            aircraftArea = FindObjectOfType<AircraftArea>();
            ActiveCamera = FindObjectOfType<Camera>();
        }
        private void Start()
        {
            GameManager.Instance.OnStateChange += OnStateChange;


            FollowAgent = aircraftArea.AircraftAgents[0];
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                agent.FreezeAgent();
                if (agent.GetType() == typeof(AircraftPlayer))
                {

                    FollowAgent = agent;
                    aircraftPlayer = (AircraftPlayer)agent;
                    aircraftPlayer.pauseInput.performed += PauseInputPerformed;
                }
                else
                {

                    agent.SetModel(GameManager.Instance.GameDifficulty.ToString(),
                        difficultyModels.Find(x => x.difficulty == GameManager.Instance.GameDifficulty).model);
                }
            }


            Debug.Assert(virtualCamera != null, "Sanal Kamera belirtilmedi");
            virtualCamera.Follow = FollowAgent.transform;
            virtualCamera.LookAt = FollowAgent.transform;
            hud.FollowAgent = FollowAgent;


            hud.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive(false);
            countdownUI.gameObject.SetActive(false);
            gameoverUI.gameObject.SetActive(false);


            StartCoroutine(StartRace());
        }

        private IEnumerator StartRace()
        {
            // Countdown'u göster
            countdownUI.gameObject.SetActive(true);
            yield return countdownUI.StartCountdown();

            // Ajanın durumunu izlemeyi başlat
            aircraftStatuses = new Dictionary<AircraftAgent, AircraftStatus>();
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                AircraftStatus status = new AircraftStatus();
                status.lap = 1;
                status.timeRemaining = checkpointBonusTime;
                aircraftStatuses.Add(agent, status);
            }


            GameManager.Instance.GameState = GameState.Playing;
        }

        private void PauseInputPerformed(InputAction.CallbackContext obj)
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                GameManager.Instance.GameState = GameState.Paused;
                pauseMenu.gameObject.SetActive(true);
            }
        }

        private void OnStateChange()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                // Oyun süresini başlat/devam ettir, HUD'yi göster, ajanları çöz
                lastResumeTime = Time.time;
                hud.gameObject.SetActive(true);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.ThawAgent();
            }
            else if (GameManager.Instance.GameState == GameState.Paused)
            {
                // Oyunu durdurup ajanları dondur
                previouslyElapsedTime += Time.time - lastResumeTime;
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.FreezeAgent();
            }
            else if (GameManager.Instance.GameState == GameState.Gameover)
            {
                // Oyunu durdur ajanları ve HUD'u dondur
                previouslyElapsedTime += Time.time - lastResumeTime;
                hud.gameObject.SetActive(false);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.FreezeAgent();

                // Oyun bitti ekranını göster
                gameoverUI.gameObject.SetActive(true);
            }
            else
            {
                // Time'ı sıfırla
                lastResumeTime = 0f;
                previouslyElapsedTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.GameState == GameState.Playing)
            {
                // Place List'i yarım saniyede bir güncelle
                if (lastPlaceUpdate + .5f < Time.fixedTime)
                {
                    lastPlaceUpdate = Time.fixedTime;

                    if (sortedAircraftAgents == null)
                    {
                        // Sıralama için ajan listesinin bir kopyasını al
                        sortedAircraftAgents = new List<AircraftAgent>(aircraftArea.AircraftAgents);
                    }

                    sortedAircraftAgents.Sort((a, b) => PlaceComparer(a, b));
                    for (int i = 0; i < sortedAircraftAgents.Count; i++)
                    {
                        aircraftStatuses[sortedAircraftAgents[i]].place = i + 1;
                    }
                }

                foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
                {
                    AircraftStatus status = aircraftStatuses[agent];

                    if (status.checkpointIndex != agent.NextCheckpointIndex)
                    {
                        status.checkpointIndex = agent.NextCheckpointIndex;
                        status.timeRemaining = checkpointBonusTime;

                        if (status.checkpointIndex == 0)
                        {
                            status.lap++;
                            if (agent == FollowAgent && status.lap > numLaps)
                            {
                                GameManager.Instance.GameState = GameState.Gameover;
                            }
                        }
                    }
                    status.timeRemaining = Mathf.Max(0f, status.timeRemaining - Time.fixedDeltaTime);
                    if (status.timeRemaining == 0f)
                    {
                        aircraftArea.ResetAgentPosition(agent);
                        status.timeRemaining = checkpointBonusTime;
                    }
                }
            }
        }

        private int PlaceComparer(AircraftAgent a, AircraftAgent b)
        {
            AircraftStatus statusA = aircraftStatuses[a];
            AircraftStatus statusB = aircraftStatuses[b];
            int checkpointA = statusA.checkpointIndex + (statusA.lap - 1) * aircraftArea.Checkpoints.Count;
            int checkpointB = statusB.checkpointIndex + (statusB.lap - 1) * aircraftArea.Checkpoints.Count;
            if (checkpointA == checkpointB)
            {

                Vector3 nextCheckpointPosition = GetAgentNextCheckpoint(a).position;
                int compare = Vector3.Distance(a.transform.position, nextCheckpointPosition)
                    .CompareTo(Vector3.Distance(b.transform.position, nextCheckpointPosition));
                return compare;
            }
            else
            {
                int compare = -1 * checkpointA.CompareTo(checkpointB);
                return compare;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChange -= OnStateChange;
            if (aircraftPlayer != null) aircraftPlayer.pauseInput.performed -= PauseInputPerformed;
        }
    }
}