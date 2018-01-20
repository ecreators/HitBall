﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.scripts
{
    public class AttackerButtonScript : MonoBehaviour
    {
        public const string TAG = "Attacker";

        public GameObject BallPrefab;
        public Text       TitleTextObject;
        public Text       ForceTextObject;
        public Text       CostTextObject;
        public Collider2D SpawnArea;
        public Image      IconImage;

        public Sprite         Icon;
        public string         Titel;
        public double         Costs;
        public double         Force;
        public float          Speed = 15;
        public float          Mass  = 1;
        public AnimationCurve CostCurveLevel100;
        public AnimationCurve ForceCurveLevel100;

        private BallScript       ballScript;
        private bool             spawnBall;
        private double           spawnBallForce;
        private double           baseCosts;
        private double           baseForce;
        private bool             triggerClickAgain;
        private double           lastTime;
        private HashSet<PowerUp> powerUpEffect;
        private GameObject       ballsSpawnParent;

        void Start()
        {
            baseCosts        = Costs;
            baseForce        = Force;
            powerUpEffect    = new HashSet<PowerUp>();
            ballsSpawnParent = GameObject.Find("balls");
            UpdateText();
        }

        void Update()
        {
            if ((triggerClickAgain || IsPowerUpMaximum) && Time.time - lastTime >= MouseHelper.DEFAULT_CLICK_REPEAT_DURATION_SECONDS)
            {
                lastTime = Time.time;
                OnClick();
            }

            if (spawnBall && ballScript == null && BallPrefab != null)
            {
                var bo = SpawnHelper.SpawnPrefab(BallPrefab, SpawnArea.transform.position, SpawnArea.bounds.size, Quaternion.identity);
                if (bo != null)
                {
                    SpawnHelper.SetParentInHierarchy(bo, ballsSpawnParent);

                    Debug.Log("Spawn Ball");
                    ballScript          = bo.GetComponent<BallScript>();
                    ballScript.HitForce = spawnBallForce;
                    ballScript.Speed    = Speed;
                    ballScript.Mass     = Mass;

                    spawnBall = false;
                }
            }
        }

        private bool IsPowerUpMaximum => powerUpEffect.Contains(PowerUp.MAXIMUM);

        public void OnClick()
        {
            var upgrade = Upgrade();
            if (IsPowerUpMaximum && !upgrade)
            {
                powerUpEffect = null;
            }

            if (upgrade && !spawnBall)
            {
                UpdateText();
            }

            triggerClickAgain = MouseHelper.IsMouseLeftDown;
        }

        private bool Upgrade()
        {
            var costs    = Hit.FromFullLife(Costs);
            var player   = PlayerScript.Player;
            var hasMoney = player.Money.GreaterThanOrEqual(costs);
            if (hasMoney)
            {
                player.Money -= costs;

                if (!IsTouchAttacker && IsSpawnRequired)
                {
                    // spawn in next update
                    spawnBall      = true;
                    spawnBallForce = Force;
                }

                UpgradeForce();

                UpgradeCosts();
            }

            return hasMoney;
        }

        private bool IsSpawnRequired
        {
            get { return ballScript == null; }
        }

        public bool IsTouchAttacker
        {
            get { return BallPrefab == null; }
        }

        private void UpgradeCosts()
        {
            var level       = PlayerScript.Player.Level;
            var playerLevel = Math.Min(100, level);

            // f zwischen 0 und 1+
            var f           = CostCurveLevel100.Evaluate(playerLevel);
            var costFactor  = f;
            var forceFactor = ForceCurveLevel100.Evaluate(playerLevel);
            Debug.Log(string.Format("Cost xf: {0:0.000}, Force xf: {1:0.000}", costFactor, forceFactor));

            Costs += baseCosts * costFactor  * level;
            Force += baseForce * forceFactor * level;

            UpdateText();
        }

        private void UpgradeForce()
        {
            if (ballScript != null)
            {
                ballScript.HitForce = Force;
            }
        }

        private void UpdateText()
        {
            TitleTextObject.text = Titel;
            ForceTextObject.text = Hit.FromFullLife(Force).ToString();
            CostTextObject.text  = Hit.FromFullLife(Costs).ToString();
            IconImage.sprite     = Icon;
        }

        public void UpgradeToMaximum()
        {
            powerUpEffect.Add(PowerUp.MAXIMUM);
        }

        private enum PowerUp
        {
            /// <summary>
            /// Löst die maximale Kraft aus,
            /// die aktuell bezahlt werden kann
            /// </summary>
            MAXIMUM
        }
    }
}