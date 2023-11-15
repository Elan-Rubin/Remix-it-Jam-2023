using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroAnimation : MonoBehaviour
{
    [SerializeField] private List<TMP_FontAsset> fontAssets;
    TMP_FontAsset lastChosen;
    TextMeshProUGUI tmp;
    private string baseText = "IM NOT A ROBOT";

    [SerializeField] private Toggle startToggle;


    void Start()
    {
        StartCoroutine(nameof(LateStart));
    }

    void Update()
    {
        
    }
    private IEnumerator LateStart()
    {
        yield return null;
        tmp = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        StartCoroutine(nameof(ChangeFonts));
    }
    private IEnumerator ChangeFonts()
    {
        lastChosen = fontAssets[Random.Range(0, fontAssets.Count)];
        var sb = new StringBuilder();
        foreach(var c in baseText)
        {
            var chosenFont = Random.value > 0.5 ? fontAssets[Random.Range(0, fontAssets.Count)] : lastChosen;
            lastChosen = chosenFont;
            sb.Append($"<font=\"{chosenFont.name}\">");
            sb.Append(c);
            sb.Append("</font>");
        }
        tmp.text = sb.ToString();
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(nameof(ChangeFonts));
    }

    public void ContinueIntro()
    {
        StartCoroutine(nameof(ContinueCoroutine));
        startToggle.transform.GetChild(0).GetChild(0).DOPunchScale(Vector2.one * 0.2f, 0.1f);
        startToggle.interactable = false;
    }
    private IEnumerator ContinueCoroutine()
    {
        yield return new WaitForSeconds(1f);

        GetComponent<CanvasGroup>().DOFade(0, 1f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            SnakeManager.Instance.StartAnimation();
        });
    }
}
