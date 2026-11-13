using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapForageDemo.UI.Gameplay.Map
{
    public sealed class UIAreaMap : MonoBehaviour
    {
        [SerializeField] private Button _previous;
        [SerializeField] private Button _next;
        [SerializeField] private Text _areaName;
        [SerializeField] private RawImage _mapImage;
        [SerializeField] private RectTransform _markers;
        [SerializeField] private RectTransform _playerPin;
        [SerializeField] private RectTransform _objectPinTemplate;

        public event Action<int> OnBrowse;
        private readonly List<RectTransform> _pins = new();

        private void OnEnable()
        {
            _previous.onClick.AddListener(BrowsePrevious);
            _next.onClick.AddListener(BrowseNext);
        }

        private void OnDisable()
        {
            _previous.onClick.RemoveListener(BrowsePrevious);
            _next.onClick.RemoveListener(BrowseNext);
        }

        public void ShowArea(string displayName, Texture2D image, IReadOnlyList<Vector2> positions)
        {
            _areaName.text = displayName;
            _mapImage.texture = image;
            while (_pins.Count < positions.Count)
                _pins.Add(Instantiate(_objectPinTemplate, _markers, false));

            for (int index = 0; index < _pins.Count; index++)
            {
                _pins[index].gameObject.SetActive(false);
                if (index < positions.Count)
                    PlacePin(_pins[index], positions[index]);
            }
        }

        public void SetObjectPinVisible(int index, bool visible) => _pins[index].gameObject.SetActive(visible);

        public void SetPlayerPin(bool visible, Vector2 position)
        {
            _playerPin.gameObject.SetActive(visible);
            if (visible)
                PlacePin(_playerPin, position);
        }

        private void BrowsePrevious() => OnBrowse?.Invoke(-1);
        private void BrowseNext() => OnBrowse?.Invoke(1);

        private static void PlacePin(RectTransform pin, Vector2 normalized)
        {
            pin.anchorMin = normalized;
            pin.anchorMax = normalized;
            pin.anchoredPosition = Vector2.zero;
        }
    }
}
