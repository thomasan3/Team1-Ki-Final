// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Mirrors an object by copying its local transformation values.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class MirroringOG : MonoBehaviour
    {
        /// <summary>
        /// Contains information about a mirrored transform pair.
        /// </summary>
        [System.Serializable]
        public class MirroredTransformPair
        {
            /// <summary>
            /// The name of the mirrored transform pair.
            /// </summary>
            [HideInInspector] public string Name;

            /// <summary>
            /// The original transform.
            /// </summary>
            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.OriginalTransform)]
            public Transform OriginalTransform;

            /// <summary>
            /// The mirrored transform.
            /// </summary>
            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.MirroredTransform)]
            public Transform MirroredTransform;

            /// <summary>
            /// MirroredTransformPair constructor.
            /// </summary>
            /// <param name="name">Mirrored pair name.</param>
            /// <param name="originalTransform">Original transform.</param>
            /// <param name="mirrorTransform">Mirrored transform.</param>
            public MirroredTransformPair(string name, Transform originalTransform, Transform mirrorTransform)
            {
                Name = name;
                OriginalTransform = originalTransform;
                MirroredTransform = mirrorTransform;
            }

            /// <summary>
            /// Mirror the character based on the original transform, and apply
            /// scale optionally.
            /// </summary>
            /// <param name="mirrorScale">Whether to mirror scale or not.</param>
            public void ApplyMirroring(bool mirrorScale)
            {
                MirroredTransform.localPosition = OriginalTransform.localPosition;
                MirroredTransform.localRotation = OriginalTransform.localRotation;
                if (mirrorScale)
                {
                    MirroredTransform.localScale = OriginalTransform.localScale;
                }
            }
        }

        /// <summary>
        /// The transform which transform values are being mirrored from.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.TransformToCopy)]
        protected Transform _transformToCopy;
        /// <inheritdoc cref="_transformToCopy" />
        public Transform OriginalTransform => _transformToCopy;

        /// <summary>
        /// The target transform which transform values are being mirrored to.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MyTransform)]
        protected Transform _myTransform;
        /// <inheritdoc cref="_myTransform" />
        public Transform MirroredTransform => _myTransform;

        /// <summary>
        /// The array of mirrored transform pairs.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairs)]
        protected MirroredTransformPair[] _mirroredTransformPairs;
        /// <inheritdoc cref="_mirroredTransformPairs" />
        public MirroredTransformPair[] MirroredTransformPairs => _mirroredTransformPairs;

        /// <summary>
        /// Mirror scale.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirrorScale)]
        protected bool _mirrorScale = false;
        /// <inheritdoc cref="_mirrorScale" />
        public bool MirrorScale => _mirrorScale;

        private List<MirroredTransformPair> _mirrorTransformPairList = new List<MirroredTransformPair>();

        private void Awake()
        {
            Assert.IsNotNull(_transformToCopy);
            if (_myTransform == null)
            {
                _myTransform = transform;
            }
            foreach (var mirroredTransformPair in _mirroredTransformPairs)
            {
                Assert.IsNotNull(mirroredTransformPair.OriginalTransform);
                Assert.IsNotNull(mirroredTransformPair.MirroredTransform);
            }
        }

        /// <summary>
        /// Sets up mirrored transform pairs.
        /// </summary>
        /// <param name="originalTransform">Original transform to mirror from.</param>
        /// <param name="mirroredTransform">Mirrored transform to mirror to.</param>
        public void SetUpMirroredTransformPairs(Transform originalTransform, Transform mirroredTransform)
        {
            _transformToCopy = originalTransform;
            _myTransform = mirroredTransform;
            Assert.IsNotNull(_transformToCopy);
            Assert.IsNotNull(_myTransform);
            _mirrorTransformPairList.Clear();
            var originalChildTransforms = _transformToCopy.GetComponentsInChildren<Transform>(true);
            foreach (var originalChildTransform in originalChildTransforms)
            {
                var mirroredChildTransform =
                    MirroredTransform.transform.FindChildRecursive(originalChildTransform.name);
                if (mirroredChildTransform != null)
                {
                    var newMirroredPair = new MirroredTransformPair(originalChildTransform.name,
                        originalChildTransform, mirroredChildTransform);
                    _mirrorTransformPairList.Add(newMirroredPair);
                }
                else
                {
                    Debug.LogError($"Missing a mirrored transform for: {originalChildTransform.name}");
                }
            }
            _mirroredTransformPairs = _mirrorTransformPairList.ToArray();
        }

        private class TransformSnapshot
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public float Timestamp;

            public TransformSnapshot(Vector3 position, Quaternion rotation, Vector3 scale, float timestamp)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Timestamp = timestamp;
            }
        }

        private Queue<TransformSnapshot> _snapshotQueue = new Queue<TransformSnapshot>();
        private float _delaySeconds = 2f;

        private void LateUpdate()
        {
            // Record the current transform of the original
            _snapshotQueue.Enqueue(new TransformSnapshot(
                _transformToCopy.localPosition,
                _transformToCopy.localRotation,
                _transformToCopy.localScale,
                Time.time
            ));

            // Check if we have a snapshot older than 2 seconds
            while (_snapshotQueue.Count > 0 && Time.time - _snapshotQueue.Peek().Timestamp >= _delaySeconds)
            {
                var snapshot = _snapshotQueue.Dequeue();

                // Apply the delayed transform to the mirrored object
                _myTransform.localPosition = snapshot.Position;
                _myTransform.localRotation = snapshot.Rotation;
                if (_mirrorScale)
                {
                    _myTransform.localScale = snapshot.Scale;
                }

                // Also apply for all pairs
                foreach (var transformPair in _mirroredTransformPairs)
                {
                    transformPair.MirroredTransform.localPosition = transformPair.OriginalTransform.localPosition;
                    transformPair.MirroredTransform.localRotation = transformPair.OriginalTransform.localRotation;
                    if (_mirrorScale)
                    {
                        transformPair.MirroredTransform.localScale = transformPair.OriginalTransform.localScale;
                    }
                }
            }
        }

    }
}
