using Unity.MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {
        [Header("Hareket Parametreleri")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Patlama Bilgileri")]
        [Tooltip("Patlama durumunda kaybolacak uçak modeli")]
        public GameObject meshObject;

        [Tooltip("Patlama efektinin oyun objesi")]
        public GameObject explosionEffect;

        [Header("Eğitim")]
        [Tooltip("Eğitimde zaman aşımına uğramadan önceki adım sayısı")]
        public int stepTimeout = 300;

        public int NextCheckpointIndex { get; set; }

        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;

        private float nextStepTimeout;

        private bool frozen = false;

        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;

        public override void Initialize()
        {
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();

            MaxStep = area.trainingMode ? 5000 : 0;
        }

        public override void OnActionReceived(float[] vectorAction)
        {
            pitchChange = vectorAction[0]; // yukarı ya da hiç
            if (pitchChange == 2) pitchChange = -1f; // aşağı
            yawChange = vectorAction[1]; // sağa dön ya da hiç
            if (yawChange == 2) yawChange = -1f; // sola dön

            boost = vectorAction[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            if (frozen) return;

            ProcessMovement();

            if (area.trainingMode)
            {
                // Her adımda küçük negatif ödül
                AddReward(-1f / MaxStep);

                // Eğer eğitimdeysek, zamanın bitmediğinden emin ol
                if (StepCount > nextStepTimeout)
                {
                    AddReward(-.5f);
                    EndEpisode();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                if (localCheckpointDir.magnitude < Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f))
                {
                    GotCheckpoint();
                }
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Uçak hızını gözlemle (1 vector3 = 3 değer)
            sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity));

            // Sonraki kontrol noktası nerede? (1 vector3 = 3 değer)
            sensor.AddObservation(VectorToNextCheckpoint());

            // Sonraki kontrol noktasının yönü (1 vector3 = 3 değer)
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
            sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));

            // Toplam Gözlemler = 3 + 3 + 3 = 9
        }

        /// <summary>
        /// Yeni bir bölüm başladığında çağrılır
        /// </summary>
        public override void OnEpisodeBegin()
        {
            // Hız, pozisyon ve oryantasyonu sıfırla
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this, randomize: area.trainingMode);

            // Eğitimdeysek, adım zaman aşımını güncelle
            if (area.trainingMode) nextStepTimeout = StepCount + stepTimeout;
        }

        /// <summary>
        /// Bu projede, Heuristic yalnızca AircraftPlayer üzerinde kullanılacaktır
        /// </summary>
        /// <returns>Boş dizi</returns>
        public override void Heuristic(float[] actionsOut)
        {
            Debug.LogError("Heuristic() " + gameObject.name + " üzerinde çağrıldı. Lütfen yalnızca AircraftPlayer'ın Davranış Türü: Heuristic Only olarak ayarlandığından emin olun.");
        }

        /// <summary>
        /// Ajanın hareket etmesini ve eylem almasını engeller
        /// </summary>
        public void FreezeAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw eğitimde desteklenmiyor");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        /// <summary>
        /// Ajanın hareketine ve eylemlerine devam etmesini sağlar
        /// </summary>
        public void ThawAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw eğitimde desteklenmiyor");
            frozen = false;
            rigidbody.WakeUp();
        }

        /// <summary>
        /// Ajan doğru kontrol noktasından geçtiğinde çağrılır
        /// </summary>
        private void GotCheckpoint()
        {
            // Sonraki kontrol noktasına geçildi, güncelle
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if (area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = StepCount + stepTimeout;
            }
        }

        /// <summary>
        /// Ajanın geçmesi gereken bir sonraki kontrol noktasına giden vektörü alır
        /// </summary>
        /// <returns>Yerel alandaki bir vektör</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        /// <summary>
        /// Hareketi hesapla ve uygula
        /// </summary>
        private void ProcessMovement()
        {
            // Boost hesapla
            float boostModifier = boost ? boostMultiplier : 1f;

            // İleri itiş uygulama
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);

            // Mevcut dönüşü al
            Vector3 curRot = transform.rotation.eulerAngles;

            // Roll açısını hesapla (-180 ile 180 arasında)
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if (yawChange == 0f)
            {
                // Dönmüyorsa; yumuşakça merkeze doğru dön
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                // Dönüyorsa; dönüş yönünün tersine doğru dön
                rollChange = -yawChange;
            }

            // Yumuşak delta değerlerini hesapla
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            // Yeni pitch, yaw ve roll hesapla. Pitch ve roll'u sınırlı tut.
            float pitch = curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 180f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            float roll = curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;
            if (roll > 180f) roll -= 360f;
            roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

            // Yeni rotayı ayarla
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        /// <summary>
        /// Bir tetikleyiciye girildiğinde tepki ver
        /// </summary>
        /// <param name="other">Girilen collider</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.CompareTag("checkpoint") &&
                other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        /// <summary>
        /// Çarpışmalara tepki ver
        /// </summary>
        /// <param name="collision">Çarpışma bilgisi</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                // Başka bir ajanla çarpmadık
                if (area.trainingMode)
                {
                    AddReward(-1f);
                    EndEpisode();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }

        /// <summary>
        /// Uçağı en son tamamlanan kontrol noktasına sıfırlar
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator ExplosionReset()
        {
            FreezeAgent();

            // Uçak modelini devre dışı bırak, patlama efektini etkinleştir
            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            // Patlamayı devre dışı bırak, uçak modelini tekrar etkinleştir
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }
    }
}
