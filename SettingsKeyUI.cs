using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsKeyUI : MonoBehaviour
{
    public TextMeshProUGUI Text;
    public TextMeshProUGUI KeyText;
    public Button KeyButton;
    public int KeyIndex;
    public string PreviousKeyCode;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIndex(int num)
    {
        KeyIndex = num;
    }

    public void ChangeKey()
    {
        if(!UIManager.Instance.isListeningForKey)
        {
            UIManager.Instance.isListeningForKey = true;
            UIManager.Instance.ListenKey = GetComponent<SettingsKeyUI>();
        }
    }
}