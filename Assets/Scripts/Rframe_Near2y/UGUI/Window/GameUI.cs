using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI : Window
{
    private GamePanel m_GamePanel = null;

    public override void OnAwake(params object[] paraList)
    {
        m_GamePanel = GameObject.GetComponent<GamePanel>();
        AddBtnClickListener(m_GamePanel.m_OverBtn, ClickOverBtn);
    }

    private void ClickOverBtn()
    {
        UIManager.Instance.CloseWindow(Name);
        UIManager.Instance.PopUpWindow(UIName.Menu);
    }
}
