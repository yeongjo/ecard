using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{

    public List<Player> m_playerList = new List<Player>();
    public Player myPlayer;
    public static GameManager _instance;
    public int turnIndex;
    int kingIndex = 1;
    int slaveIndex = 0;
    [SyncVar(hook = "OnBetMoneyChanged")]
    public int betMoney = 1;
    Slider betSlider;
    Text betText;

    bool bGameEnd;

    [SyncVar]
    bool bIsBetTurn;

    Button turnEndButton;

    public Transform gameEndObj;

    void OnEnable()
    {
        _instance = this;
        GameObject canvas = GameObject.Find("Canvas");
        turnEndButton = canvas.transform.GetChild(0).GetComponent<Button>();
        betSlider = canvas.transform.GetChild(1).GetComponent<Slider>();
        betSlider.onValueChanged.AddListener(OnBetMoney);
        betText = canvas.transform.GetChild(2).GetComponent<Text>();
        gameEndObj = canvas.transform.GetChild(3);
        turnEndButton.onClick.AddListener(OnTurnEnd);
    }

    void Start()
    {
        if (isServer)
            StartCoroutine(CustomStart());
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            m_playerList[0].m_myTurn = !m_playerList[0].m_myTurn;
        }
    }

    IEnumerator CustomStart()
    {
        yield return new WaitForSeconds(1.8f);
        StartCoroutine(GameStart());
    }

    void GameInit()
    {
        ChangeKing();
        m_playerList[kingIndex].SetUniqueCard(3);
        m_playerList[slaveIndex].SetUniqueCard(2);

        m_playerList[kingIndex].remainTurn = 1;
        m_playerList[slaveIndex].remainTurn = 2;

        bIsBetTurn = true;
        RpcButtonInteractable(false, kingIndex);
        RpcSliderInteractable(false, kingIndex);
        RpcButtonInteractable(true, slaveIndex);
        RpcSliderInteractable(true, slaveIndex);
    }

    void ChangeKing()
    {
        if (kingIndex == 0)
        {
            kingIndex = 1;
            slaveIndex = 0;
        }
        else
        {
            kingIndex = 0;
            slaveIndex = 1;
        }
    }

    public void OnTurnEnd()
    {
        if (bIsBetTurn || myPlayer.frontSlot)
        {
            turnEndButton.interactable = false;
            myPlayer.OnTurnEnd();
        }
    }

    [ClientRpc]
    void RpcButtonInteractable(bool t, int i)
    {
        if (m_playerList[i].isLocalPlayer)
        {
            turnEndButton.interactable = t;
            if(t)
            StartCoroutine(ShowBrocastImage(myTurnSprite));
        }
    }

    /// <summary>
    /// //////////////
    /// </summary>
    public SpriteRenderer m_spriteRenderer;
    public SpriteRenderer m_myTurnSpriteRenderer;
    public Sprite myTurnSprite;
    public Sprite loseSprite;
    public Sprite winSprite;
    public Sprite drawSprite;
    IEnumerator ShowBrocastImage(Sprite s)
    {
        if(s == myTurnSprite)
        {
            m_myTurnSpriteRenderer.sprite = s;
            yield return new WaitForSeconds(1);
            if (m_myTurnSpriteRenderer.sprite == s)
                m_myTurnSpriteRenderer.sprite = null;
        }
        else
        {
            m_spriteRenderer.sprite = s;
            yield return new WaitForSeconds(1);
            if (m_spriteRenderer.sprite == s)
                m_spriteRenderer.sprite = null;
        }

    }

    [ClientRpc]
    void RpcSetRoundSprite(int round)
    {
        ShowRoundImage(round);
    }

    public SpriteRenderer m_spriteRendererRound;
    public Sprite[] mRoundSprite;
    public int roundNum;
    IEnumerator ShowRoundImage(int round)
    {
        m_spriteRendererRound.sprite = mRoundSprite[round - 1];
        yield return new WaitForSeconds(1);
        if (m_spriteRendererRound.sprite == mRoundSprite[round - 1])
            m_spriteRendererRound.sprite = null;
    }

    ///////////////////

    [ClientRpc]
    void RpcSliderInteractable(bool b, int slave)
    {
        Debug.Log("RpcSliderInteractable "+ (m_playerList[slave] == myPlayer));
        if (m_playerList[slave] == myPlayer)
        betSlider.interactable = b;
    }

    public void OnBetMoney(float m)
    {
        betMoney = (int)m;
        betSlider.maxValue = myPlayer.money;
        betText.text = "Bet Money : " + betMoney;
        myPlayer.CmdChangeBetMoney(betMoney);
    }

    //hooked
    void OnBetMoneyChanged(int m)
    {
        Debug.Log("OnBetMoneyChanged");
        //if (m_playerList[slaveIndex] == myPlayer) return;
        betMoney = m;
        //betSlider.value = m;
        betText.text = "Bet Money : " + m;
    }

    [ClientRpc]
    void RpcSetTurn(int i, bool b)
    {
        m_playerList[i].SetMyTurn(b);
    }

    IEnumerator GameStart()
    {
        yield return new WaitForSeconds(.2f);
        GameInit();
        while (true)
        {
            if (m_playerList[slaveIndex].bIsTurnEnd)
            {
                m_playerList[slaveIndex].bIsTurnEnd = false;
                RpcSliderInteractable(false, slaveIndex);
                break;
            }
            yield return new WaitForSeconds(.1f);
        }
        RpcSetRoundSprite(++roundNum);
        bIsBetTurn = false;
        while (true)
        {
            yield return new WaitForSeconds(.1f);
            RpcButtonInteractable(true, turnIndex);
            m_playerList[turnIndex].m_myTurn = true;

            while (true)
            {
                if (m_playerList[turnIndex].bIsTurnEnd)
                {
                    m_playerList[turnIndex].bIsTurnEnd = false;
                    Debug.Log("턴끝 : " + turnIndex + " " + m_playerList[turnIndex].remainTurn);
                    if (!m_playerList[turnIndex].RemainTurnDecrease())
                    {
                        Debug.Log("up");
                        m_playerList[turnIndex].SetRemainTurn(2);
                        if (++turnIndex >= m_playerList.Count)
                        {
                            turnIndex = 0;
                        }
                    }
                    else
                    {
                        Debug.Log("down");
                        if (m_playerList[0].frontSlot && m_playerList[1].frontSlot)
                        {
                            m_playerList[0].FlipFrontCard();
                            m_playerList[1].FlipFrontCard();
                            yield return new WaitForSeconds(2);
                            int whowin;
                            if ((whowin = FlipFrontCard()) != -1)
                            {
                                //someone win game end
                                betMoney = 1;
                                m_playerList[0].RpcReset();
                                m_playerList[1].RpcReset();
                                yield return new WaitForSeconds(2);
                                if (!bGameEnd)
                                    StartCoroutine(GameStart());
                                else
                                    RpcGameEndBrocast(whowin);
                                yield break;
                            }
                        }

                        RpcSetRoundSprite(++roundNum);
                    }
                    break;
                }
                yield return new WaitForSeconds(.1f);
            }
            yield return new WaitForSeconds(.1f);
        }
    }

    [ClientRpc]
    void RpcGameEndBrocast(int winIndex)
    {
        gameEndObj.gameObject.SetActive(true);
        if (m_playerList[winIndex].isLocalPlayer)
            gameEndObj.GetChild(0).GetComponent<Text>().text = "WINNER";
        else
            gameEndObj.GetChild(0).GetComponent<Text>().text = "WASTED";
    }

    int FlipFrontCard()
    {
        int bRoundEnd = -1;
        if (!m_playerList[0].frontSlot || !m_playerList[1].frontSlot) return -1;
        Debug.Log(m_playerList[0].frontSlot.num + " " + m_playerList[1].frontSlot.num);
        m_playerList[0].RpcUseFrontCard();
        m_playerList[1].RpcUseFrontCard();
        if (m_playerList[0].frontSlot.num == 1)
        {
            if(m_playerList[1].frontSlot.num == 1)
            {
                Draw();
            }
            else if (m_playerList[1].frontSlot.num == 2)
            {
                Win(0);
                bRoundEnd = 0;
            }
            else if (m_playerList[1].frontSlot.num == 3)
            {
                Win(1);
                bRoundEnd = 1;
            }
        }
        else if(m_playerList[0].frontSlot.num == 2 )
        {
            if (m_playerList[1].frontSlot.num == 1)
            {
                Win(1);
                bRoundEnd = 1;
            }
            else if (m_playerList[1].frontSlot.num == 2)
            {
                Draw();
            }
            else if (m_playerList[1].frontSlot.num == 3)
            {
                Win(0);
                bRoundEnd = 0;
            }
        }
        else if (m_playerList[0].frontSlot.num == 3)
        {
            if (m_playerList[1].frontSlot.num == 1)
            {
                Win(0);
                bRoundEnd = 0;
            }
            else if (m_playerList[1].frontSlot.num == 2)
            {
                Win(1);
                bRoundEnd = 1;
            }
            else if (m_playerList[1].frontSlot.num == 3)
            {
                Draw();
            }
        }
        return bRoundEnd;
    }

    void Draw()
    {
        Debug.Log("Draw");
        RpcBrocastDraw();
        //StartCoroutine(GameStart());
    }

    public GameObject coinObj;
    [ClientRpc]
    void RpcSpawnCoinObj(int i, int count)
    {
        StartCoroutine(CoinAnim(m_playerList[i].isLocalPlayer, count));
    }
    IEnumerator CoinAnim(bool me, int count)
    {
        for(int i = 0; i < count; ++i)
        {
            if (me)
                Instantiate(coinObj);
            else
                Instantiate(coinObj, new Vector2(0, 0), Quaternion.Euler(0, 0, 180));
            yield return new WaitForSeconds(.08f);
        }
    }
    void Win(int i)
    {
        Debug.Log("Win "+ i +" " + betMoney);
        int money;
        if (i == slaveIndex)
            money = 3 * betMoney;
        else
            money = betMoney;
        if (i == 0)
        {
            m_playerList[1].money -= money;
            m_playerList[0].money += money;
            RpcSpawnCoinObj(1, money);
            RpcBrocastWin(0);
            if (m_playerList[1].money <= 0)
                bGameEnd = true;
        }
        else
        {
            m_playerList[0].money -= money;
            m_playerList[1].money += money;
            RpcSpawnCoinObj(0, money);
            RpcBrocastWin(1);
            if (m_playerList[0].money <= 0)
                bGameEnd = true;
        }
    }

    [ClientRpc]
    void RpcBrocastDraw()
    {
        StartCoroutine(ShowBrocastImage(drawSprite));
    }

    [ClientRpc]
    void RpcBrocastWin(int i)
    {
        if(m_playerList[i] == myPlayer)
        {
            StartCoroutine(ShowBrocastImage(winSprite));
        }
        else
        {
            StartCoroutine(ShowBrocastImage(loseSprite));
        }
    }

    public void AddPlayer(Player p)
    {
        if (p.isLocalPlayer) myPlayer = p;
        m_playerList.Add(p);
    }

    public void DeletePlayer(Player p)
    {
        m_playerList.Remove(p);
    }

    public Player FindEnemy(Player p)
    {
        for (int i = 0; i < m_playerList.Count; ++i)
        {
            if (m_playerList[i] != p)
                return m_playerList[i];
        }
        return p;
    }
}
