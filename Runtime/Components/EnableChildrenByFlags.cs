using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyLibrary
{
    public class EnableChildrenByFlags : MonoBehaviour
    {
        public Conditions[] conditions
        {
            get => _conditions;
            set
            {
                _conditions = value;
                UpdateFlagListeners();
            }
        }

    #pragma warning disable CS0649
        [SerializeField] Conditions[] _conditions;
        [SerializeField] GameObject[] _specificChildren;
    #pragma warning restore CS0649

        bool _matched;

        [NonSerialized] HashSet<string> _curFlags = new HashSet<string>();

        void OnEnable() => Refresh();

        void OnTransformChildrenChanged() => EnableChildren();

        void OnDisable() => DeregisterListeners();

        public void Refresh()
        {
            if (isActiveAndEnabled) UpdateFlagListeners();
            ReviewFlags();
            EnableChildren();
        }

        void UpdateFlagListeners()
        {
            DeregisterListeners();

            foreach (var cond in _conditions)
                foreach (var flag in cond.flags)
                    _curFlags.Add(flag);
            
            foreach (var flag in _curFlags)
                FlagController.RegisterListener(flag, ReviewFlags);
        }

        void DeregisterListeners()
        {
            if (_curFlags.Count == 0) return;

            foreach (var flag in _curFlags)
                FlagController.DeregisterListener(flag, ReviewFlags);

            _curFlags.Clear();
        }

        void ReviewFlags()
        {
            _matched = true;

            foreach (var cond in _conditions)
            {
                var isAnd = cond.type == Conditions.Type.And;
                var localMatch = isAnd;

                foreach (var flag in cond.flags)
                {
                    localMatch = isAnd
                        ? localMatch && FlagController.Check(flag)
                        : localMatch || FlagController.Check(flag);
                    
                    if (isAnd != localMatch) break;
                }

                localMatch = cond.not ? !localMatch : localMatch;

                _matched = _matched && localMatch;

                if (!_matched) break;
            }
        }

        void EnableChildren()
        {
            if (_specificChildren.Length == 0)
                foreach (Transform child in transform)
                    child.gameObject.SetActive(_matched);
            else
                foreach (var child in _specificChildren)
                    child.SetActive(_matched);
        }

    #if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            Refresh();
        }
    #endif

        [Serializable]
        public struct Conditions
        {
            public bool not;
            public Type type;
            public string[] flags;

            public enum Type
            {
                And,
                Or
            }
        }
    }
}