using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITextHandler : MonoBehaviour
{
    public int _Number;
    public bool _IsMoving = true;

    private Coroutine _openingMovementCoroutine;

    private RectTransform _rectTransform;
    private TextMeshProUGUI _text;

    private float _firstXPos;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _text = GetComponent<TextMeshProUGUI>();
        _firstXPos = _rectTransform.anchoredPosition.x;
    }

    private void OnEnable()
    {
        Localization._LanguageChangedEvent += SetText;
        SetText();
        if (_IsMoving)
            OpeningMovement();
    }
    private void OnDisable()
    {
        Localization._LanguageChangedEvent -= SetText;
    }
    public void SetText()
    {
        if (Localization._Instance._UI.Count >= _Number && Localization._Instance._UI[_Number - 1] != null)

            GetComponent<TextMeshProUGUI>().text = Localization._Instance._UI[_Number - 1];
    }
    private void OpeningMovement()
    {
        if (_openingMovementCoroutine != null)
            StopCoroutine(_openingMovementCoroutine);
        _openingMovementCoroutine = StartCoroutine(OpeningMovementCoroutine());
    }
    private IEnumerator OpeningMovementCoroutine()
    {
        if (Time.realtimeSinceStartup < 1f)
            yield return null;
        _rectTransform.anchoredPosition += -Vector2.right * 75f;
        _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, 0f);

        float startTime = Time.realtimeSinceStartup;
        while (startTime + 0.75f > Time.realtimeSinceStartup)
        {
            _rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(_rectTransform.anchoredPosition.x, _firstXPos, Time.unscaledDeltaTime * 7f), _rectTransform.anchoredPosition.y);
            _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, Mathf.Lerp(_text.color.a, 1f, Time.unscaledDeltaTime * 3.25f));
            yield return null;
        }

        _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, 1f);
        _rectTransform.anchoredPosition = new Vector2(_firstXPos, _rectTransform.anchoredPosition.y);
    }
}