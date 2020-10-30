using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{

    [SyncVar]
    public bool m_myTurn;
    public GameObject cardObjForSpawn;
    public List<Card> m_Cards = new List<Card>();
    public Card[] everyCardCanUse = new Card[5];
    Card handCard;
    int handCardIndex;
    public Card frontSlot;
    public Rect frontRect;
    [SyncVar]
    Vector2 worldMousePos;
    [SyncVar]
    bool bClicked;
    float handLine;

    [SyncVar]
    public bool bIsKing;

    public float distanceCard = 1.57f;
    [SyncVar]
    public int playerId;
    static int static_id;

    public int remainTurn = 2;

    [SyncVar]
    public bool bIsTurnEnd;

    [SyncVar(hook ="OnChangeMoney")]
    public int money = 10;

    public TextMesh moneyTextMesh;

    public NetworkIdentity networkId;
    public GameObject manager;

    public void Start()
    {
        playerId = ++static_id;
        if (isLocalPlayer && isServer)
            CmdSpawnManager();

        for (int i = 0; i < 5; ++i)
        {
            GameObject t = Instantiate(cardObjForSpawn);
            t.transform.SetParent(transform);
            Card c;
            (c = t.GetComponent<Card>()).m_player = this;
            m_Cards.Add(c);
        }

        //if (isLocalPlayer)
        //    CmdSpawnCard();

        everyCardCanUse = m_Cards.ToArray();
        //SetUniqueCard(2);


        networkId = GetComponent<NetworkIdentity>();
        StartCoroutine(CustomStart());
    }

    [Command]
    void CmdSpawnManager()
    {
        GameObject a = Instantiate(manager);
        NetworkServer.Spawn(a);
    }

    //[Command]
    //void CmdSpawnCard()
    //{
    //    for (int i = 0; i < 5; ++i)
    //    {
    //        GameObject t = Instantiate(cardObjForSpawn);
    //        t.transform.SetParent(transform);
    //        NetworkServer.SpawnWithClientAuthority(t, this.gameObject);
    //        t.GetComponent<Card>().parentId = playerId;
    //        m_Cards.Add(t.GetComponent<Card>());
    //    }
    //}
    //[ClientRpc]
    //void CmdSpawnCard()
    //{
    //}

        bool bStarted;
    IEnumerator CustomStart()
    {
        yield return new WaitForSeconds(1);

        GameManager._instance.AddPlayer(this);
        handLine = m_Cards[0].transform.localPosition.y + m_Cards[0].size.y * .5f;

        if (!isLocalPlayer)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 180);
        }
        //if(isLocalPlayer)
        //for (int i = 0; i < m_Cards.Count; ++i)
        //{
        //    CmdSetAuthority(m_Cards[i].networkId);
        //}
        bStarted = true;
    }


    [Command]
    void CmdSetAuthority(NetworkIdentity id)
    {
        id.AssignClientAuthority(connectionToClient);
    }

    void Update()
    {
        if (!bStarted) return;
        if (isLocalPlayer)
        {
            worldMousePos = GetWorldPosFormMousePos();
            bClicked = Input.GetMouseButton(0);
            CmdMouse(worldMousePos, bClicked);
        }
        SortCards();
        ClickCard();
        DragHandCard();
        CheckReleaseHandCard();

    }

    [ClientRpc]
    public void RpcReset()
    {
        m_Cards.Clear();
        sortIndex = 0;
        for (int i = 0; i < everyCardCanUse.Length; ++i)
        {
            m_Cards.Add(everyCardCanUse[i]);
            everyCardCanUse[i].m_sprite.color = Color.white;
            everyCardCanUse[i].m_sprite.sortingOrder = 0;
            if (!isLocalPlayer)
                everyCardCanUse[i].bIsFliped = true;
            else
                everyCardCanUse[i].bIsFliped = false;
        }
    }

    [ClientRpc]
    public void RpcUseFrontCard()
    {
        frontSlot.m_sprite.color = Color.grey;
    }

    [Command]
    void CmdMouse(Vector2 pos, bool bClick)
    {
        worldMousePos = pos;
        bClicked = bClick;
    }

    void SortCards()
    {
        float delta = (m_Cards.Count + 1) * .5f - 1;
        for (int i = 0; i < m_Cards.Count; ++i)
        {
            m_Cards[i].transform.localPosition = Vector3.Lerp(m_Cards[i].transform.localPosition, new Vector2((i - delta) * distanceCard, m_Cards[i].defaultPosY), 20 * Time.deltaTime);
        }
    }

    public void ClickCard()
    {
        //if (!isLocalPlayer) return;
        for (int i = 0; i < everyCardCanUse.Length; ++i)
        {
            if (everyCardCanUse[i].CheckMouseClicked(GetMousePos(), isLocalPlayer&& Input.GetMouseButtonDown(0)))
                PickCard2Hand(i);
        }
        //Vector2 ray = Camera.main.ScreenToWorldPoint(mousePos);
        //Debug.DrawRay(ray,  Vector2.up);
        //RaycastHit2D hit = Physics2D.Raycast(ray, Vector2.zero);
        //if (hit.collider)
        //{
        //    Debug.Log(hit.collider.name);
        //    for (int i = 0; i < m_Cards.Count; ++i)
        //    {
        //        if (hit.collider.gameObject == m_Cards[i].gameObject)
        //            if (m_Cards[i].CheckMouseClicked(GetMousePos(), Input.GetMouseButtonDown(0)))
        //                PickCard2Hand(m_Cards[i]);
        //    }
        //}
    }

    void PickCard2Hand(int i)
    {
        if (handCard) return;
        everyCardCanUse[i].SetIsInHand(false);
        handCard = everyCardCanUse[i];
        handCardIndex = i;
        handCard.m_sprite.sortingOrder = ++sortIndex;
        m_Cards.Remove(everyCardCanUse[i]);

        CmdPickCard2Hand(i);
    }
    [Command]
    void CmdPickCard2Hand(int i)
    {
        RpcPickCard2Hand(i);
    }
    [ClientRpc]
    void RpcPickCard2Hand(int i)
    {
        if (isLocalPlayer) return;
        everyCardCanUse[i].SetIsInHand(false);
        handCard = everyCardCanUse[i];
        handCardIndex = i;
        handCard.m_sprite.sortingOrder = ++sortIndex;
        m_Cards.Remove(everyCardCanUse[i]);
    }
    [Command]
    public void CmdChangeBetMoney(int money)
    {
        GameManager._instance.betMoney = money;
    }

    int sortIndex;

    void CheckReleaseHandCard()
    {
        if (bClicked || !handCard || !isLocalPlayer) return;
        if (handCard != frontSlot)
            handCard.transform.localPosition = worldMousePos;
        else
            handCard.transform.localPosition = frontRect.center;
        handCard.ChangeColor(Color.white);
        CmdCheckReleaseHandCard(handCard.transform.localPosition, handCardIndex);
        handCard = null;
        handCardIndex = -1;

    }
    [Command]
    void CmdCheckReleaseHandCard(Vector2 pos, int index)
    {
        RpcCheckReleaseHandCard(pos, index);
    }
    [ClientRpc]
    void RpcCheckReleaseHandCard(Vector2 pos, int index)
    {
        everyCardCanUse[index].transform.localPosition = pos;
        everyCardCanUse[index].ChangeColor(Color.white);
        handCard = null;
        handCardIndex = -1;
    }

    bool CheckCard(Card c)
    {
        bool t = false;
        for (int i = 0; i < m_Cards.Count; ++i)
        {
            if (m_Cards[i] == c)
                t = true;
        }
        return t;
    }

    void DragHandCard()
    {
        if (!handCard) return;
        Vector2 mouse = worldMousePos;
        //Debug.Log("mouse : " + mouse + frontRect.yMin + " " + -frontRect.yMax + " " + frontRect.xMin + " " + frontRect.xMax);
        if (frontRect.yMin < mouse.y || frontRect.yMax > mouse.y || frontRect.xMin > mouse.x || frontRect.xMax < mouse.x)
        {
            handCard.transform.localPosition = Vector2.Lerp(handCard.transform.localPosition, mouse, 20 * Time.deltaTime);
            RemoveFrontSlot();
        }
        else
        {
            //Debug.Log("hoho");
            AddToFrontSlot(handCardIndex);
            //Debug.Log("frontRect.center: " + frontRect.center);
            handCard.transform.localPosition = Vector2.Lerp(handCard.transform.localPosition, frontRect.center, 30 * Time.deltaTime);
        }

        if (!isLocalPlayer) return;
        if (mouse.y < handLine)
        {
            if (!CheckCard(handCard))
            {
                m_Cards.Add(handCard);
                handCard.SetIsInHand(true);
                CmdAddHandCard();
            }
        }
        else
        {
            if (CheckCard(handCard))
            {
                m_Cards.Remove(handCard);
                handCard.SetIsInHand(false);
                CmdRemoveHandCard();
            }
        }
    }
    [Command]
    void CmdAddHandCard()
    {
        RpcAddHandCard();
    }
    [ClientRpc]
    void RpcAddHandCard()
    {
        if (isLocalPlayer || !handCard) return;
        m_Cards.Add(handCard);
        handCard.SetIsInHand(true);
    }
    [Command]
    void CmdRemoveHandCard()
    {
        RpcRemoveHandCard();
    }
    [ClientRpc]
    void RpcRemoveHandCard()
    {
        if (isLocalPlayer) return;
        m_Cards.Remove(handCard);
        handCard.SetIsInHand(false);
    }


    void AddToFrontSlot(int i)
    {
        frontSlot = everyCardCanUse[i];
        if (isLocalPlayer)
            CmdAddToFrontSlot(i);
    }
    [Command]
    void CmdAddToFrontSlot(int i)
    {
        RpcAddToFrontSlot(i);
    }
    [ClientRpc]
    void RpcAddToFrontSlot(int i)
    {
        if (isLocalPlayer) return;
        frontSlot = everyCardCanUse[i];
    }

    void RemoveFrontSlot()
    {
        frontSlot = null;
        if (isLocalPlayer)
            CmdRemoveFrontSlot();
    }
    [Command]
    void CmdRemoveFrontSlot()
    {
        RpcRemoveFrontSlot();
    }
    [ClientRpc]
    void RpcRemoveFrontSlot()
    {
        if (isLocalPlayer) return;
        frontSlot = null;
    }

    Vector2 GetWorldPosFormMousePos()
    {
        return Camera.main.ScreenToWorldPoint(GetMousePos());
    }

    //로컬플레이어랑 아닌거랑 나눠줌
    Vector2 GetMousePos()
    {
        //if (isLocalPlayer)
        return Input.mousePosition;
        //else
        //    return new Vector2(Screen.width - mousePos.x, Screen.height - mousePos.y);
    }

    public void SetUniqueCard(int t)
    {
        int ran = Random.Range(0, 4);
        for (int i = 0; i < 5; ++i)
        {
            if(i==ran)
                everyCardCanUse[ran].SetCardNum(t);
            else
                everyCardCanUse[i].SetCardNum(1);
        }
        RpcSetUniqueCard(t, ran);
    }
    [ClientRpc]
    public void RpcSetUniqueCard(int t, int ran)
    {
        if (isServer) return;
        for (int i = 0; i < 5; ++i)
        {
            if (i == ran)
                everyCardCanUse[ran].SetCardNum(t);
            else
                everyCardCanUse[i].SetCardNum(1);
        }
    }

    public void SetIsTurnEnd(bool b)
    {
        bIsTurnEnd = b;
        if (isLocalPlayer)
            CmdSetIsTurnEnd(b);
    }
    [Command]
    public void CmdSetIsTurnEnd(bool b)
    {
        Debug.Log("HOHO");
        bIsTurnEnd = b;
    }

    public bool RemainTurnDecrease()
    {
        return SetRemainTurn(--remainTurn);
    }

    public bool SetRemainTurn(int t)
    {
        remainTurn = t;
        if (t == 0) return false;
        return true;
    }

    public void SetMyTurn(bool t)
    {
        m_myTurn = t;
        if (isLocalPlayer)
            CmdSetMyTurn(t);
    }
    [Command]
    void CmdSetMyTurn(bool t)
    {
        m_myTurn = t;
    }

    public void OnTurnEnd()
    {
        bIsTurnEnd = true;
        SetMyTurn(false);
        CmdOnTurnEnd();
    }
    [Command]
    void CmdOnTurnEnd()
    {
        bIsTurnEnd = true;
    }

    public void FlipFrontCard()
    {
        RpcFlipFrontCard();
    }
    [ClientRpc]
    void RpcFlipFrontCard()
    {
            frontSlot.bIsFliped = false;
    }

    //hooked
    void OnChangeMoney(int m)
    {
        int delta = m - money;
        money = m;
        if(delta > 0)
        moneyTextMesh.text = "<color=lime>+" + delta+ "</color>\n"+money + " coin";
        else
            moneyTextMesh.text = "<color=red>" + delta + "</color>\n" + money + " coin";
    }
}
