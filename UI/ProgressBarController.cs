using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Tweens;
using System.Collections;

public class ProgressBarController : MonoBehaviour
{
    public enum ProgressTextType
    {
        Percent,
        Value,
        ValueMax
    }

    [SerializeField] UIProgressBar _bar;
    [SerializeField] float _duration = 5f;
    [SerializeField] TweenEasing _easing = TweenEasing.InOutQuint;
    [SerializeField] Text _text;
    [SerializeField] ProgressTextType _progressTextType = ProgressTextType.Percent;
    [SerializeField] int _maxValue = 0;

    int _currentValue;
    bool _isTweening = false;
    //These values are to be assigned on upgrade if SetProgress was called before last tweening is done
    bool _isWaitingForTweening;
    int _newCurrentValue;
    int _newMaxValue;

    // Tween controls
    [NonSerialized]
    private readonly TweenRunner<FloatTween> m_FloatTweenRunner;

    // Called by Unity prior to deserialization, 
    // should not be called by users
    ProgressBarController()
    {
        if (m_FloatTweenRunner == null)
            m_FloatTweenRunner = new TweenRunner<FloatTween>();

        m_FloatTweenRunner.Init(this);
    }

    void OnEnable()
    {
        if (_bar == null)
            return;

        if(_maxValue != 0)
            SetProgress(_currentValue, _maxValue);
    }

    void SetFillAmount(float amount)
    {
        if (_bar == null)
            return;

        _bar.fillAmount = amount;

        if (_text != null)
        {
            if (_progressTextType == ProgressTextType.Percent)
            {
                _text.text = Mathf.RoundToInt(((float)_currentValue / _maxValue) * 100f).ToString() + " %";
            }
            else if (_progressTextType == ProgressTextType.Value)
            {
                _text.text = Mathf.RoundToInt(_currentValue).ToString();
            }
            else if (_progressTextType == ProgressTextType.ValueMax)
            {
                _text.text = Mathf.RoundToInt(_currentValue).ToString() + " / " + _maxValue;
            }
        }
    }

    void OnTweenFinished()
    {
        if (_bar == null)
            return;

        _isTweening = false;

        if(_isWaitingForTweening)
        {
            SetProgress(_newCurrentValue, _newMaxValue);
            _newCurrentValue = _newMaxValue = 0;
            _isWaitingForTweening = false;
        }
    }

    void StartTween(float targetFloat, float duration)
    {
        if (_bar == null)
            return;

        var floatTween = new FloatTween { duration = duration, startFloat = _bar.fillAmount, targetFloat = targetFloat };
        floatTween.AddOnChangedCallback(SetFillAmount);
        floatTween.AddOnFinishCallback(OnTweenFinished);
        floatTween.ignoreTimeScale = true;
        floatTween.easing = _easing;
        m_FloatTweenRunner.StartTween(floatTween);
        _isTweening = true;
    }

    public void SetProgress(int currentValue, int maxValue)
    {
        if (_bar == null)
            return;

        if(_isTweening)
        {
            _newCurrentValue = currentValue;
            _newMaxValue = maxValue;
            _isWaitingForTweening = true;
            return;
        }

        _currentValue = currentValue;
        _maxValue = maxValue;

        if (gameObject.activeInHierarchy)
        {
            if (_bar.fillAmount == 0)
                StartTween((float)_currentValue / _maxValue, _duration);
            else
                StartTween((float)_currentValue / _maxValue, _bar.fillAmount * _duration); 
        }
    }
}