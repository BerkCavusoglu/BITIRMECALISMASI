using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {
        [Tooltip("Yarışın takip edeceği yol")]
        public CinemachineSmoothPath racePath;

        [Tooltip("Kontrol noktaları için kullanılacak prefab")]
        public GameObject checkpointPrefab;

        [Tooltip("Başlangıç/bitiş kontrol noktası için kullanılacak prefab")]
        public GameObject finishCheckpointPrefab;

        [Tooltip("Eğer doğruysa, eğitim modu etkinleştirilir")]
        public bool trainingMode;

        public List<AircraftAgent> AircraftAgents { get; private set; }
        public List<GameObject> Checkpoints { get; private set; }

        private void Awake()
        {
            // Çocuk nesneleri arasından AircraftAgent'ları al
            AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();
            Debug.Assert(AircraftAgents.Count > 0, "Hiç AircraftAgent bulunamadı");
        }

        private void Start()
        {
            // Yarış yolunun ayarlandığından emin ol
            Debug.Assert(racePath != null, "Yarış Yolu ayarlanmamış");
            Checkpoints = new List<GameObject>();
            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
            for (int i = 0; i < numCheckpoints; i++)
            {
                // Kontrol noktalarını yerleştir
                GameObject checkpoint;
                if (i == numCheckpoints - 1) checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                else checkpoint = Instantiate<GameObject>(checkpointPrefab);

                // Kontrol noktalarını doğru yere yerleştir
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                // Kontrol noktalarını listeye ekle
                Checkpoints.Add(checkpoint);
            }
        }

        public void ResetAgentPosition(AircraftAgent agent, bool randomize = false)
        {
            if (randomize)
            {
                // Eğer rastgele pozisyon isteniyorsa, kontrol noktalarından birini rastgele seç
                agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
            }

            int previousCheckpointIndex = agent.NextCheckpointIndex - 1;
            if (previousCheckpointIndex == -1) previousCheckpointIndex = Checkpoints.Count - 1;

            // Yarış yolunun birimlerini kullanarak start pozisyonunu al
            float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

           
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);
            Quaternion orientation = racePath.EvaluateOrientation(startPosition);

            
            Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f)
                * UnityEngine.Random.Range(9f, 10f);

            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;
        }
    }
}
