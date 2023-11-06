using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private Image captchaImage; 

    private static UpgradeManager instance;
    public static UpgradeManager Instance { get { return instance; } }
    private void Awake()
    {
        if (instance != this && instance != null) Destroy(gameObject);  
        else instance = this;
    }

    void Start()
    {
        GenerateUpgrades();
    }

    void Update()
    {
        
    }

    private void GenerateUpgrades()
    {
        StartCoroutine(nameof(GenerateUpgradesCoroutine));
    }
    private IEnumerator GenerateUpgradesCoroutine()
    {
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Captchas/");
        FileInfo[] info = dir.GetFiles("*.png");
        foreach (FileInfo f in info)
        {
            yield return new WaitForSeconds(0.1f);
            captchaImage.sprite = IMG2Sprite.LoadNewSprite(f.FullName);
            Debug.Log(f.FullName);
        }

        yield return null;
    }
}

public struct Upgrade
{
    public char Letter;
    public UpgradeType Type;
    public UpgradeType Prerequisite;
    public Upgrade(char letter, UpgradeType type)
    {
        Letter = letter;
        Type = type;
        Prerequisite = UpgradeType.None;
    }
}
public enum UpgradeType
{
    None,

}
