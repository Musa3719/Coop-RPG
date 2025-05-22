using UnityEngine;
using TMPro;
public class SpeechText : MonoBehaviour
{
    public bool _IsLookingTowardsCamera;

    private TextMeshProUGUI _textComponent;
    private int _lastTextSize;
    private void Awake()
    {
        _textComponent = GetComponent<TextMeshProUGUI>();
    }
    void Update()
    {
        while (!_textComponent.isTextOverflowing && _textComponent.fontSize < 7 && _lastTextSize > _textComponent.text.Length)
        {
            _textComponent.fontSize += 0.25f;
            _textComponent.ForceMeshUpdate();
        }
        while (_textComponent.isTextOverflowing && _textComponent.fontSize > 3)
        {
            _textComponent.fontSize -= 0.25f;
            _textComponent.ForceMeshUpdate();
        }
        _lastTextSize = _textComponent.text.Length;

    }
    private void LateUpdate()
    {
        if (_IsLookingTowardsCamera)
        {
            transform.parent.LookAt(GameManager._Instance._MainCamera.transform);
        }
    }
}
