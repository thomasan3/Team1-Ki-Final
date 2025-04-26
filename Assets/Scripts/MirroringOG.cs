// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Movement.Effects
{
    [DefaultExecutionOrder(300)]
    public class MirroringOG : MonoBehaviour
    {
        [System.Serializable]
        public class MirroredTransformPair
        {
            [HideInInspector] public string Name;

            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.OriginalTransform)]
            public Transform OriginalTransform;

            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.MirroredTransform)]
            public Transform MirroredTransform;

            public MirroredTransformPair(string name, Transform originalTransform, Transform mirrorTransform)
            {
                Name = name;
                OriginalTransform = originalTransform;
                MirroredTransform = mirrorTransform;
            }
        }

        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.TransformToCopy)]
        protected Transform _transformToCopy;
        public Transform OriginalTransform => _transformToCopy;

        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MyTransform)]
        protected Transform _myTransform;
        public Transform MirroredTransform => _myTransform;

        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairs)]
        protected MirroredTransformPair[] _mirroredTransformPairs;
        public MirroredTransformPair[] MirroredTransformPairs => _mirroredTransformPairs;

        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirrorScale)]
        protected bool _mirrorScale = false;
        public bool MirrorScale => _mirrorScale;

        private List<MirroredTransformPair> _mirrorTransformPairList = new List<MirroredTransformPair>();

        private class TransformSnapshot
        {
            public Vector3 ParentPosition;
            public Quaternion ParentRotation;
            public Vector3 ParentScale;
            public Dictionary<MirroredTransformPair, (Vector3 Position, Quaternion Rotation, Vector3 Scale)> ChildSnapshots;
            public float Timestamp;

            public TransformSnapshot(Vector3 parentPos, Quaternion parentRot, Vector3 parentScale,
                Dictionary<MirroredTransformPair, (Vector3, Quaternion, Vector3)> childSnapshots, float timestamp)
            {
                ParentPosition = parentPos;
                ParentRotation = parentRot;
                ParentScale = parentScale;
                ChildSnapshots = childSnapshots;
                Timestamp = timestamp;
            }
        }

        private Queue<TransformSnapshot> _snapshotQueue = new Queue<TransformSnapshot>();
        private float _delaySeconds = 2f;

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
                var mirroredChildTransform = MirroredTransform.transform.FindChildRecursive(originalChildTransform.name);
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

        private void LateUpdate()
        {
            // Record snapshot
            Vector3 parentPos = _transformToCopy.localPosition;
            Quaternion parentRot = _transformToCopy.localRotation;
            Vector3 parentScale = _transformToCopy.localScale;

            var childSnapshots = new Dictionary<MirroredTransformPair, (Vector3, Quaternion, Vector3)>();
            foreach (var pair in _mirroredTransformPairs)
            {
                childSnapshots[pair] = (pair.OriginalTransform.localPosition,
                                        pair.OriginalTransform.localRotation,
                                        pair.OriginalTransform.localScale);
            }

            _snapshotQueue.Enqueue(new TransformSnapshot(parentPos, parentRot, parentScale, childSnapshots, Time.time));

            // Apply snapshot after delay
            while (_snapshotQueue.Count > 0 && Time.time - _snapshotQueue.Peek().Timestamp >= _delaySeconds)
            {
                var snapshot = _snapshotQueue.Dequeue();

                _myTransform.localPosition = snapshot.ParentPosition;
                _myTransform.localRotation = snapshot.ParentRotation;
                if (_mirrorScale)
                {
                    _myTransform.localScale = snapshot.ParentScale;
                }

                foreach (var pair in _mirroredTransformPairs)
                {
                    if (snapshot.ChildSnapshots.TryGetValue(pair, out var childData))
                    {
                        pair.MirroredTransform.localPosition = childData.Position;
                        pair.MirroredTransform.localRotation = childData.Rotation;
                        if (_mirrorScale)
                        {
                            pair.MirroredTransform.localScale = childData.Scale;
                        }
                    }
                }
            }
        }
    }
}