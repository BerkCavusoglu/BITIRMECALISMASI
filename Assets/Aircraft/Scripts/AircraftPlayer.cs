using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class AircraftPlayer : AircraftAgent
    {
        [Header("Girdi Bağlantıları")]
        public InputAction pitchInput;  // İleri-geri hareket girişi
        public InputAction yawInput;    // Sağ-sol hareket girişi
        public InputAction boostInput;  // Hızlanma girişi
        public InputAction pauseInput;  // Duraklatma girişi

        public override void Initialize()
        {
            base.Initialize();
            pitchInput.Enable();  // İleri-geri hareket girişini etkinleştir
            yawInput.Enable();    // Sağ-sol hareket girişini etkinleştir
            boostInput.Enable();  // Hızlanma girişini etkinleştir
            pauseInput.Enable();  // Duraklatma girişini etkinleştir
        }

        public override void Heuristic(float[] actionsOut)
        {
            // İleri-geri hareket değerini al
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());

            // Sağ-sol hareket değerini al
            float yawValue = Mathf.Round(yawInput.ReadValue<float>());

            // Hızlanma değerini al
            float boostValue = Mathf.Round(boostInput.ReadValue<float>());

            // İleri-geri hareket değerinin -1 olması durumunda 2'ye ayarla
            if (pitchValue == -1f) pitchValue = 2f;

            // Sağ-sol hareket değerinin -1 olması durumunda 2'ye ayarla
            if (yawValue == -1f) yawValue = 2f;

            // Eylemleri çıkış dizisine ata
            actionsOut[0] = pitchValue;
            actionsOut[1] = yawValue;
            actionsOut[2] = boostValue;
        }

        private void OnDestroy()
        {
            // Girdi bağlantılarını devre dışı bırak
            pitchInput.Disable();
            yawInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
        }
    }
}
