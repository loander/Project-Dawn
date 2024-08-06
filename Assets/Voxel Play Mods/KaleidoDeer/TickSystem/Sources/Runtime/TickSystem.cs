using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace VoxelPlay
{
    public partial class VoxelPlayEnvironment : MonoBehaviour
    {
        [Tooltip("The speed at which ticks occur in seconds.")]
        public float TickRate = 0.05f;

        float TimePassed = 0;

        public event VoxelPlayEvent OnTick;

        void Update()
        {
            TimePassed += Time.deltaTime;
            if (TimePassed >= TickRate)
            {
                OnTick?.Invoke();
                TimePassed = 0.0f;
            }
        }
    }
}
