using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Card : NetworkBehaviour {

    [SyncVar(hook = "OnChangedCardNum")]
    public int num; //0 뒷면//1 시민 //2 도둑 //3 왕
    public Vector2 size = new Vector2(1.5f, 2f);
    public float defaultPosY = 4.2f;
    public float stayUpSize = .2f;
    //Image m_image;
    public SpriteRenderer m_sprite;
    [SyncVar]
    public bool bIsInHand = true;

    public bool bIsFliped;

    public Sprite[] otherCard = new Sprite[4]; //0 뒤 1앞 2도둑 3왕

    public Player m_player;
    //public NetworkIdentity networkId;

    void Start()
    {
        defaultPosY = transform.localPosition.y;
        //m_image = GetComponent<Image>();
        //m_sprite = GetComponent<SpriteRenderer>();
        //networkId = GetComponent<NetworkIdentity>();
        SetCardNum(num);
        if (!m_player.isLocalPlayer)
            bIsFliped = true;
    }
    public void SetCardNum(int t)
    {
        num = t;
        if (t == 2)
            m_sprite.sprite = otherCard[2];
        else if (t == 3)
            m_sprite.sprite = otherCard[3];
        if(isLocalPlayer)
        CmdSetCardNum(t);
    }
    [Command]
    void CmdSetCardNum(int t)
    {
        num = t;
    }

    void OnChangedCardNum(int t)
    {
        if (!m_sprite) m_sprite = GetComponent<SpriteRenderer>();
        if (t == 2)
            m_sprite.sprite = otherCard[2];
        else if(t==3)
            m_sprite.sprite = otherCard[3];

        if(!m_player.isLocalPlayer)
            bIsFliped = true;
    }

    public bool CheckMouseClicked(Vector2 mousePos, bool bClicked)
    {
        if (m_sprite)
        {
            m_sprite.color = Color.white;
            if (bIsFliped)
                m_sprite.sprite = otherCard[0];
            else
                m_sprite.sprite = otherCard[num];
        }
        //m_image.color = Color.white;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //float x = mousePos.x - Screen.width * .5f;
        if (transform.localPosition.x + size.x * .5f < mousePos.x || transform.localPosition.x - size.x * .5f > mousePos.x)
        {
            return false;
        }
        //float y = mousePos.y - Screen.height * .5f;
        if (transform.localPosition.y + size.y * .5f < mousePos.y || transform.localPosition.y - size.y * .5f > mousePos.y)
        {
            return false;
        }
        ///////////////
        if (bClicked)
            MouseClicked();
        else
            MouseEnter();

        if(isLocalPlayer)
        CmdMouseClicked(bClicked);

        return bClicked;
    }

    [Command]
    void CmdMouseClicked(bool bClicked)
    {
        RpcMouseClicked(bClicked);
    }
    [ClientRpc]
    void RpcMouseClicked(bool bClicked)
    {
        if (isLocalPlayer) return;
        if (bClicked)
            MouseClicked();
        else
            MouseEnter();
    }

    void MouseEnter()
    {
        if (!bIsInHand) return;
        transform.localPosition = Vector2.Lerp(transform.localPosition, new Vector2(transform.localPosition.x, stayUpSize+ defaultPosY), 20 * Time.deltaTime);
    }

    void MouseClicked()
    {
        //m_image.color = Color.grey;
        m_sprite.color = Color.grey;
    }

    public void ChangeColor(Color color)
    {
        if (!bIsInHand) return;
        m_sprite.color = color;
    }

    public void SetIsInHand(bool b)
    {
        if (bIsInHand == b) return;
        bIsInHand = b;
        if (isLocalPlayer)
            CmdSetIsInHand(b);
    }
    [Command]
    void CmdSetIsInHand(bool b)
    {
        RpcSetIsInHand(b);
    }
    [ClientRpc]
    void RpcSetIsInHand(bool b)
    {
        if (isLocalPlayer) return;
            bIsInHand = b;
    }

}
