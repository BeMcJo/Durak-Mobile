using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player
{
    public string playerName;
    public GameObject playerGO;
    public List<Card> hand;
    public int connectionId;
    public bool ack;
    public bool winner;
}

public class FieldPair
{
    public Card attack;
    public Card defend;
}

public class GameManager : MonoBehaviour
{
    public static int WIDTH = 1280, HEIGHT = 800;
    [SerializeField]
    public static GameManager gm;

    public bool isServer;

    public GameObject mainCanvas,
               optionsCanvas,
               gameCanvas,
               myCardsContainer,
               cardField,
               attackField,
               defendField,
               otherPlayersContainer,
               playerActionsContainer,
               attackBtn,
               defendBtn,
               transferBtn,
               endBattleBtn,
               trumpCard,
               endGameCanvas,
               multiplayersCanvas;

    public GameObject actionButtonPrefab,
                      cardPrefab,
                      otherPlayerPrefab,
                      cardHolderPrefab;

    Text deckCountText,
         playerTurnText,
         handCountText;
    public int w, h;
    public List<List<Card>> playerHands;
    public List<Card> selected, 
                      graveyard;
    public List<FieldPair> defendCards;
    public List<Player> playerOrder,
                        winners;
    public int defender, originalTurn, durak, myTurn;
    public int initialDrawAmount = 6;
    // Phase 0: Start turn - Decide what card(s) to use to attack other player
    // Phase 1: Battle phase - After playing card(s), attacked player decides to defend or lose round.
    //          Other players can participate in attacking during this phase
    // Phase 2: Cleanup Phase - After the battle is complete, players draw until they have 6 cards
    //          If no more cards to draw and player has no cards in hand, player is not a loser.
    //          If only 1 player remains, that player is loser. Game finishes and ends.
    //          If not, move to next available player to start turn
    public int phase;
    public bool inGame;
    public bool hasDefended;
    public bool hasSuccessfullyDefended;
    public bool gameEnded;

    public Sprite[] cardSprites;
    public string playerName;

    public List<FieldPair> field;
    public Card trump;
    public Deck deck;


    private void Start()
    {
        //Debug.Log(Screen.width + " " + Screen.height);
        
        if (!gm)
        {
            gm = this;
            cardSprites = Resources.LoadAll<Sprite>("Sprites/playing cards");
            w = Screen.width;
            h = Screen.height;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        LoadMainScene();
    }

    private void Update()
    {
        if(Screen.orientation != ScreenOrientation.Landscape)

            Screen.orientation = ScreenOrientation.Landscape;
        int width = Screen.width, height = Screen.height;
        if(w != width && h != height)
        {
            //Debug.Log("CHANGED DIMENSIONS");
            w = width;
            h = height;
        }
        if (myCardsContainer && false)
        {
            HorizontalLayoutGroup hlg = myCardsContainer.transform.GetComponent<HorizontalLayoutGroup>();
            int cardCt = myCardsContainer.transform.childCount;
            if (cardCt <= 6)
                hlg.spacing = 0;
            else if (cardCt <= 8)
                hlg.spacing = -20;
            else if (cardCt <= 10)
                hlg.spacing = -40;
            //else if (cardCt <= 10)
            //    hlg.spacing = -5;
            //else if (cardCt <= 12)
            //    hlg.spacing = -25;
            else
                hlg.spacing = -50;

        }
    }

    private void OnLevelWasLoaded(int level)
    {
        if (this != gm)
            return;
        ClearGame();
        switch (level)
        {
            // Main
            case 0:
                LoadMainScene();
                break;
            // Multiplayer
            case 1:
                LoadMultiplayerScene();
                break;
            // Server
            case 2:
                LoadServerScene();
                break;
            // Client
            case 3:
                LoadClientScene();
                break;
            // Game (Multiplayer)
            case 4:
                LoadMultiplayerGameScene();
                break;
        }
    }

    public int NextAvailablePlayer(int player)
    {
        //Debug.Log("NEST PLAYTER");
        int nextPlayer = player;
        for (int i = 1; i < playerOrder.Count; i++)
        {
            nextPlayer = (player + i) % playerOrder.Count;
            if (!playerOrder[nextPlayer].winner)
                break;
        }
        //Debug.Log(player + " " + nextPlayer);
        return nextPlayer;
    }

    public void AddCardsTo(int player, List<Card> cards)
    {
        foreach (Card c in cards)
        {
            playerOrder[player].hand.Add(c);
            if (player == myTurn)
            {
                GameObject cardHolder = Instantiate(cardHolderPrefab);
                cardHolder.transform.SetParent(myCardsContainer.transform);
                cardHolder.tag = "Hand";
                cardHolder.transform.localScale = new Vector3(1, 1, 1);
                GameObject cardUI = CreateCardUI(c);//Instantiate(cardPrefab);
                //cardUI.transform.GetChild(0).GetComponent<Text>().text = Card.CARDS[c];
                cardUI.transform.SetParent(cardHolder.transform);
                cardUI.transform.position = cardHolder.transform.position;
                cardUI.transform.localScale = new Vector3(1, 1, 1);
                //cardUI.transform.GetComponent<CardUI>().card = c;
            }
            //else
            //{
            //otherPlayerUI.SetParent(otherPlayersContainer.transform);
            //}
        }
        UpdateOtherPlayerUI(player);
    }

    public int GetDurak()
    {
        if (playerOrder.Count - winners.Count != 1)
            return -1;
        for (int i = 0; i < playerOrder.Count; i++)
        {
            if (!winners.Contains(playerOrder[i]))
                return i;
        }
        return -1;
    }

    public bool HasFinishedGame(int player)
    {
        if (deck.Count() != 0 || playerOrder[player].hand.Count > 0)
            return false;
        Player p = playerOrder[player];
        p.winner = true;
        if (!winners.Contains(p))
            winners.Add(playerOrder[player]);
        //playerOrder.Remove(playerOrder[player]);
        //if (playerOrder.Count - winners.Count == 1)

        return (gameEnded = ((durak = GetDurak()) != -1));
        //if (player == originalTurn)
        //    originalTurn = originalTurn + 1 % playerOrder.Count;

    }

    bool DisplayEndGameCanvas()
    {
       // Debug.Log("HEY" + endGameCanvas.transform.Find("Transitioner").localPosition);
        endGameCanvas.transform.Find("Transitioner").localPosition -= new Vector3(0, 10, 0);

        //Debug.Log("asdasdHEY" + (endGameCanvas.transform.Find("Transitioner").localPosition.y > 0));
        return endGameCanvas.transform.Find("Transitioner").localPosition.y > 0;
    }

    IEnumerator EndGame()
    {
        // Debug.Log("?");
        string fool = "You";
        if (durak != myTurn)
            fool = playerOrder[durak].playerName;
        endGameCanvas.transform.Find("Transitioner").Find("DurakTxt").GetComponent<Text>().text = "The Durak\n" + fool;
        yield return new WaitWhile(DisplayEndGameCanvas);
    }

    public void CleanUpPhase()
    {
        //Debug.Log("cleanup");
        //if(phase != 1)

        // Check if game ended after defender ends battle phase
        if (gameEnded)
        {
            Debug.Log("durak" + durak);
            Debug.Log("WE HAVE A LOSEAH " + durak + " >>" + playerOrder[durak].playerName);
            StartCoroutine("EndGame");
            //gameEnded = true;
            return;
        }
        phase = 2;
        bool successfulDefend = true;
        // Check if any attack cards left undefended
        foreach (FieldPair fp in field)
        {
            if (fp.defend == null || !hasDefended)
            {
                successfulDefend = false;
                break;
            }
        }

        // Return any cards defender used to defend unsuccessfully
        for (int i = 0; i < defendField.transform.childCount; i++)
        {
            Transform cardHolder = defendField.transform.GetChild(i);
            if (cardHolder.childCount == 1)
            {
                CardUI cui = cardHolder.GetChild(0).GetComponent<CardUI>();
                foreach (FieldPair fp in defendCards)
                {
                    if (cui.card.Equals(fp.defend))
                    {
                        Debug.Log("found " + cui.card);
                        cui.ReturnToHand();
                        break;
                    }
                }
            }
        }
        defendCards.Clear();
        // Add all cards on field to loser's hand
        List<Card> addToHand = new List<Card>();
        for (int i = 0; i < attackField.transform.childCount; i++)
        {
            //Debug.Log("ATK CARD");
            if (!successfulDefend)
            {
                CardUI cui = attackField.transform.GetChild(i).GetComponent<CardUI>();
                addToHand.Add(cui.card);
            }
            Destroy(attackField.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < defendField.transform.childCount; i++)
        {
            //Debug.Log("DEF");
            Transform cardHolder = defendField.transform.GetChild(i);
            if (cardHolder.childCount == 1)
            {
                CardUI cui = cardHolder.GetChild(0).GetComponent<CardUI>();
                if (!successfulDefend)
                {
                    addToHand.Add(cui.card);
                }
                Destroy(cui.gameObject);
            }
            Destroy(cardHolder.gameObject);
        }

        int turnStarter;
        // If defender failed to defend, turn starts on player after defender
        if (!successfulDefend)
        {
            AddCardsTo(defender, addToHand);
            turnStarter = NextAvailablePlayer(defender);
            //turn = turn + 1;
        }
        // else defender stats turn
        else
        {
            turnStarter = defender;
        }

        // Sanity check, make sure all cards successfully defended sent to graveyard is not duplicates
        if (successfulDefend)
        {
            foreach (FieldPair fp in field)
            {
                if (graveyard.Contains(fp.attack))
                {
                    Debug.LogError("ALREADY SEEN CARD");
                }
                else
                    graveyard.Add(fp.attack);
                if (fp.defend != null)
                    if (graveyard.Contains(fp.defend))
                    {
                        Debug.LogError("ALREADY SEEN CARD");
                    }
                    else
                        graveyard.Add(fp.defend);

            }
        }
        field.Clear();
        // original turn player draws until 6 cards or no more
        // rest in the respective order draws as well if necessary
        // check for players with no more cards and remove from game if so
        // move to start turn
        for (int i = 0; i < playerOrder.Count; i++)
        {
            int player = (originalTurn + i) % playerOrder.Count;

            if (deck.Count() == 0)
            {
                Debug.Log("NO MORE CARDS TO DRAW");
            }
            else
            {
               // Debug.Log("HAS CARDs" + player);
                AddCardsTo(player, deck.Draw(initialDrawAmount - playerOrder[player].hand.Count));
            }
            if (!playerOrder[player].winner && HasFinishedGame(player))
            {
                Debug.Log("WE HAVE A LOSEAH " + durak + " >>" + playerOrder[durak].playerName);
                StartCoroutine("EndGame");
                return;
            }
        }
        //Debug.Log("No loser yet, NEXT TURN");
        // If turn starter has no cards after draw phase, find next player to start turn
        if (playerOrder[turnStarter].winner)
            turnStarter = NextAvailablePlayer(turnStarter);
        UpdateGameUI();
        StartTurn(turnStarter);
        //UpdateActionButtons(myTurn);
    }

    public bool EndBattlePhase()
    {
        //Debug.Log("End battle");
        if (phase != 1)
            return false;
        CleanUpPhase();
        return true;
    }

    public bool Transfer(int player, List<Card> toTransfer)
    {
      //  Debug.Log("transfer?");
        if (!CanTransfer(player, toTransfer))
            return false;
        Debug.Log("could");
        AddCardsToAttackField(player, toTransfer);

        defender = NextAvailablePlayer(player);
        UpdateGameUI();
        UpdateOtherPlayerUI(player);
        UpdateActionButtons();
        return true;
    }

    public bool CanTransfer(int player, List<Card> toTransfer)
    {
        if (toTransfer.Count == 0 || field.Count == 0)
            return false;
     //   Debug.Log("legit ct");
        for (int i = 1; i < toTransfer.Count; i++)
            if (!toTransfer[i].EqualValue(toTransfer[0]))
                return false;
       // Debug.Log("legit cards");
        //Debug.Log(player + " " + defender + " " + hasDefended + " " + phase + " " + Card.CARDS[field[0].attack] + " " + Card.CARDS[toTransfer[0]]);
        //Debug.Log(myTurn + " " + NextAvailablePlayer(defender) + " "  + playerOrder[NextAvailablePlayer(defender)].hand.Count + " " + (field.Count + toTransfer.Count));
        return player == defender &&
               !hasDefended &&
               phase == 1 &&
               field[0].attack.EqualValue(toTransfer[0]) &&
               field.Count + toTransfer.Count <= initialDrawAmount &&
               playerOrder[NextAvailablePlayer(defender)].hand.Count >= field.Count + toTransfer.Count;
    }

    public bool Defend(int player, Card def, Card atk)
    {
        foreach (FieldPair fp in field)
        {
            if (fp.attack.Equals(atk))
                return Defend(player, def, fp);
        }
        return false;
    }

    public bool Defend(int player, Card def, FieldPair fieldCard)
    {
        if (player != defender || !CanDefend(fieldCard, def))
            return false;
        fieldCard.defend = def;
        hasDefended = true;
        if (myTurn == defender)
        {
            for (int i = 0; i < defendField.transform.childCount; i++)
            {
                Transform cardHolder = defendField.transform.GetChild(i);
                if (cardHolder.childCount == 1)
                {
                    CardUI cui = cardHolder.GetChild(0).GetComponent<CardUI>();
                    if (cui.card.Equals(def))
                    {
                        cui.played = true;
                        break;
                    }
                }
            }
            for (int i = 0; i < myCardsContainer.transform.childCount; i++)
            {
                if (myCardsContainer.transform.GetChild(i).childCount == 0)
                    Destroy(myCardsContainer.transform.GetChild(i).gameObject);
            }
            //playerOrder[player].hand.Remove(def);
        }
        else
        {
            for (int i = 0; i < attackField.transform.childCount; i++)
            {
                //Transform cardHolder = defendField.transform.GetChild(i)
                CardUI cui = attackField.transform.GetChild(i).GetComponent<CardUI>();
                if (cui.card.Equals(fieldCard.attack))
                {
                    GameObject defCardUI = CreateCardUI(def);//Instantiate(cardPrefab);
                    //defCardUI.transform.GetComponent<CardUI>().card = def;
                    defCardUI.transform.GetComponent<CardUI>().played = true;
                    defCardUI.transform.SetParent(defendField.transform.GetChild(i));
                    defCardUI.transform.localScale = new Vector3(1, 1, 1);
                    //defCardUI.transform.GetChild(0).GetComponent<Text>().text = Card.CARDS[def];
                    defCardUI.transform.position = defCardUI.transform.parent.position;
                    break;
                }
            }
           // playerOrder[player].hand.Remove(playerOrder[player].hand[0]);
        }
        if (player == myTurn)
            playerOrder[player].hand.Remove(def);
        else
        {
            foreach (Card cc in playerOrder[player].hand)
            {
                if (cc.Equals(def))
                {
                    playerOrder[player].hand.Remove(cc);
                    break;
                }
            }
        }
        if (HasFinishedGame(player))
        {
            Debug.Log("WOO WIN" + playerOrder[player].playerName);
            StartCoroutine("EndGame");
        }
        //if (player == myTurn)
        //else
        UpdateGameUI();
        UpdateOtherPlayerUI(player);
        UpdateActionButtons();
        return true;
    }

    public bool CanDefend(FieldPair fp, Card def)
    {
        if (phase != 1 || fp.defend != null)
            return false;
        Card atk = fp.attack;
        if (atk.Equals(trump))
            return false;
        if (def.Equals(trump))
            return true;
        if (atk.cardValue == trump.cardValue)
        {
            return false;
        }
        if (atk.cardSuit == trump.cardSuit)
        {
            if (def.EqualValue(trump))
                return true;
            if (def.cardSuit == trump.cardSuit)
            {
                return def.Difference(atk) > 0;
            }
            return false;
        }
        if (def.cardSuit == trump.cardSuit || def.cardValue == trump.cardValue)
            return true;
        return atk.cardSuit == def.cardSuit && def.Difference(atk) > 0;
    }

    public bool ValidAttack(List<Card> cards)
    {
        if (cards.Count == 0)
            return false;
        List<int> fieldValues = new List<int>();
        int undefended = 0;
        List<int> attackValues = new List<int>();

        foreach (FieldPair fp in field)
        {
            if (!fieldValues.Contains(fp.attack.cardValue))
                fieldValues.Add(fp.attack.cardValue);
            if (fp.defend != null && !fieldValues.Contains(fp.defend.cardValue))
            {
                fieldValues.Add(fp.defend.cardValue);
            }
            if(fp.defend == null)
            {
                undefended++;
            }
        }
        foreach (Card c in cards)
        {
            if (!attackValues.Contains(c.cardValue))
                attackValues.Add(c.cardValue);
        }
        if (field.Count == 0 && attackValues.Count == 1 && cards.Count <= playerOrder[defender].hand.Count)
            return true;
        ///Debug.Log("not begining");
        foreach (int value in attackValues)
        {
            if (!fieldValues.Contains(value))
                return false;
        }
        //Debug.Log("valid cards");
        return cards.Count + undefended <= playerOrder[defender].hand.Count &&
               field.Count + cards.Count <= initialDrawAmount;
    }

    public bool Attack(int player, List<Card> cards)
    {
        if (!ValidAttack(cards))
            return false;
        if (phase == 0)
        {
            if (player == defender)
                return false;
            phase = 1;
        }
        else if (phase != 1)
            return false;


        AddCardsToAttackField(player, cards);
        /*foreach (Card c in cards)
        {
            FieldPair fp = new FieldPair();
            fp.attack = c;
            field.Add(fp);
            if(player != myTurn)
            {
                GameObject cardObj = Instantiate(cardPrefab);
                CardUI cui = cardObj.transform.GetComponent<CardUI>();
                cui.card = c;
                cui.played = true;
                cardObj.transform.SetParent(attackField.transform);
                cardObj.transform.GetChild(0).GetComponent<Text>().text = Card.CARDS[c];
            }
            GameObject cardHolder = Instantiate(cardHolderPrefab);
            cardHolder.transform.SetParent(defendField.transform);
            cardHolder.tag = "DefendField";
            if (player == myTurn)
                playerOrder[player].hand.Remove(c);
            else
                playerOrder[player].hand.Remove(playerOrder[player].hand[0]);
        }*/
        UpdateOtherPlayerUI(player);
        UpdateActionButtons();
        UpdateGameUI();
        if (HasFinishedGame(player))
        {
            Debug.Log("WOO WIN" + playerOrder[player].playerName);
            StartCoroutine("EndGame");
        }
        return true;
    }

    private void AddCardsToAttackField(int player, List<Card> cards)
    {
        //Debug.Log("ADDING CARDS");
        // If I am attacking, remove selected cards from my hand to the attack field
        if (player == myTurn)
        {
            foreach (Card c in cards)
            {
                for (int i = 0; i < myCardsContainer.transform.childCount; i++)
                {

                    Debug.Log(i + " " +myCardsContainer.transform.childCount);
                    Transform cardHolder = myCardsContainer.transform.GetChild(i);
                    if (cardHolder.childCount > 0)
                    {
                        CardUI cUI = cardHolder.GetChild(0).GetComponent<CardUI>();
                        if (c.Equals(cUI.card))
                        {
                            //     Debug.Log("PLAYED " + cUI.card.ToString());
                            cUI.transform.SetParent(attackField.transform);
                            cUI.played = true;
                            Destroy(cardHolder.gameObject);
                            //i--;
                            break;
                        }
                    }
                }
            }
        }
        //foreach(Card c in playerOrder[player].hand)
       // {
        //    Debug.Log(Card.CARDS[c]);
        //}
        // Add a card holder for each card added to the attack field
        foreach (Card c in cards)
        {
            //Debug.Log("spawn" + Card.CARDS[c]);
            FieldPair fp = new FieldPair();
            fp.attack = c;
            field.Add(fp);
            // If player adding cards to field is not me, create a new card UI
            if (player != myTurn)
            {

                GameObject cardObj = CreateCardUI(c);//Instantiate(cardPrefab);
                CardUI cui = cardObj.transform.GetComponent<CardUI>();
                //cui.card = c;
                cui.played = true;
                cardObj.transform.SetParent(attackField.transform);
                cardObj.transform.localScale = new Vector3(1, 1, 1);
                //cardObj.transform.GetChild(0).GetComponent<Text>().text = Card.CARDS[c];
            }
            GameObject cardHolder = Instantiate(cardHolderPrefab);
            cardHolder.transform.SetParent(defendField.transform);
            cardHolder.tag = "DefendField";
            cardHolder.transform.localScale = new Vector3(1, 1, 1);
            if (player == myTurn)
                playerOrder[player].hand.Remove(c);
            else
            {
                foreach(Card cc in playerOrder[player].hand)
                {
                    if (cc.Equals(c))
                    {
                        playerOrder[player].hand.Remove(cc);
                        break;
                    }
                }
            }
            
        }

    }

    public void StartTurn(int player)
    {
      //  Debug.Log("START TURN" + player);
        phase = 0;
        originalTurn = player;
        defender = NextAvailablePlayer(originalTurn);
        hasDefended = false;
        hasSuccessfullyDefended = true;
        //field.Clear();
        //field = new List<FieldPair>();
        UpdateGameUI();
        UpdateActionButtons();
    }

    public void StartGame(int playerCount)
    {
        //Debug.Log("Start game");
        deck = new Deck();
        deck.Shuffle();
        graveyard = new List<Card>();
        /*playerHands = new List<List<Card>>();
        for (int i = 0; i < playerCount; i++)
        {
            playerHands.Add(deck.Draw(initialDrawAmount));
        }*/
        playerOrder = new List<Player>();
        trump = deck.Draw(1)[0];

        originalTurn = 0;
        defender = 1;
        deck.AddToBottom(trump);
        if (!isServer)
        {
            trump = null;
            deck = null;
        }
        durak = -1;
        field = new List<FieldPair>();
        winners = new List<Player>();
        selected = new List<Card>();
        defendCards = new List<FieldPair>();
        myTurn = 0;
        gameEnded = false;
        //defendField = new List<Card>();
    }

    public void ClearGame()
    {
        if(selected != null)
            selected.Clear();
        if(defendCards != null)
            defendCards.Clear();
        if(field != null)
            field.Clear();
        deck = null;
        if(winners != null)
            winners.Clear();
        trump = null;
        if(playerOrder != null)
            playerOrder.Clear();
    }

    public void Setup()
    {

        SetupUI();
    }

    public GameObject CreateCardUI(Card c)
    {
        GameObject cardUI = Instantiate(cardPrefab);
        cardUI.transform.Find("CardSprite").GetComponent<Image>().sprite = GetCardSprite(c);
        //cardUI.transform.localScale = new Vector3(1, 1, 1);
        //cardUI.transform.GetChild(0).GetComponent<Text>().text = Card.CARDS[c];
        cardUI.transform.GetComponent<CardUI>().card = c;
        return cardUI;
    }

    public Sprite GetCardSprite(Card c)
    {
        int index = 0;
        if(c.cardValue >= 1 && c.cardValue <= 13)
        {
            index = c.cardSuit * 13 + (c.cardValue % 13);
        }
        else
        {
            index = 4 * 13 + (c.cardSuit % 4); 
        }
        return cardSprites[index];
    }

    public void SetupUI()
    {

        foreach (Card c in playerOrder[myTurn].hand)
        {
            GameObject cardHolder = Instantiate(cardHolderPrefab);
            cardHolder.transform.SetParent(myCardsContainer.transform);
            cardHolder.tag = "Hand";
            cardHolder.transform.localScale = new Vector3(1, 1, 1);
            GameObject cardUI = CreateCardUI(c);
            cardUI.transform.SetParent(cardHolder.transform);
            cardUI.transform.position = cardHolder.transform.position;
            cardUI.transform.localScale = new Vector3(1, 1, 1);
        }
        for (int i = 1; i < playerOrder.Count; i++)
        {
            int p = (myTurn + i) % playerOrder.Count;
            //Debug.Log(p);
            Player plyr = playerOrder[p];
            GameObject otherPlayerUI = Instantiate(otherPlayerPrefab);
            otherPlayerUI.transform.Find("NameTxt").GetComponent<Text>().text = plyr.playerName;// + "(" + plyr.connectionId + ")";
            otherPlayerUI.transform.Find("PlayerHand").Find("HandCardsCountTxt").GetComponent<Text>().text = "x" + plyr.hand.Count;
            otherPlayerUI.transform.SetParent(otherPlayersContainer.transform);
            otherPlayerUI.transform.localScale = new Vector3(1, 1, 1);
        }
        //handCountText.text = "Hand Count: " + playerOrder[myTurn].hand.Count;
        //deckCountText.text = "Deck Count: " + deck.Count();
        //playerTurnText.text = "Player Turn\n" + playerOrder[turn].connectionId + "\nAttacking\n" + NextAvailablePlayer(turn);
        UpdateGameUI();
        GameObject trumpCardUI = CreateCardUI(trump);
        trumpCardUI.transform.position = trumpCard.transform.position;
        trumpCardUI.transform.SetParent(gameCanvas.transform);
        trumpCardUI.transform.GetComponent<CardUI>().played = true;
        trumpCardUI.transform.localScale = new Vector3(0.75f, 0.75f, 1);
        //trumpCardUI.transform.localRotation = new Quaternion(20, 40, -15, 0);
        //trumpCardUI.transform.localPosition = new Vector3(-523.5f, 17, trumpCardUI.transform.position.z);
        UpdateActionButtons();
    }

    private void UpdateGameUI()
    {
        Debug.Log("update game ui" + originalTurn + " " + defender + " " + deck.Count());
        handCountText.text = "x" + playerOrder[myTurn].hand.Count;
        deckCountText.text = "x" + deck.Count();
        playerTurnText.text = "Defender\n" + playerOrder[defender].playerName + " " + playerOrder[defender].connectionId;// + "\nPlayer Defending\n" + defender;
        if (defender == myTurn)
            playerTurnText.text = "Defender\nYou";

    }

    private void UpdateOtherPlayerUI(int player)
    {
        if (player == myTurn)
            return;
        //Debug.Log("player uis" + playerOrder[player].hand.Count);
        int pos = (myTurn + player - 1) % (playerOrder.Count - 1);
        Player p = playerOrder[player];
        //otherPlayersContainer.transform.GetChild(pos).Find("Text").GetComponent<Text>().text =
        //    p.playerName + "(" + p.connectionId + ")\nHand:" + p.hand.Count;
        otherPlayersContainer.transform.GetChild(pos).Find("PlayerHand").Find("HandCardsCountTxt").GetComponent<Text>().text = "x" + p.hand.Count;
    }

    public void UpdateActionButtons()
    {
        //Debug.Log("UPDATE ACTION BUTTONS PHASE" + phase + " defender" + defender + " turnstart " + originalTurn + " myturn" + myTurn + " defe" + defender);
        attackBtn.gameObject.SetActive(((phase == 0 && originalTurn == myTurn) || (phase == 1 && myTurn != defender)));
        attackBtn.transform.GetComponent<Button>().interactable = ValidAttack(selected);
        defendBtn.gameObject.SetActive(phase == 1 && myTurn == defender);
        defendBtn.transform.GetComponent<Button>().interactable = defendCards.Count > 0;
        transferBtn.gameObject.SetActive(phase == 1 && myTurn == defender && !hasDefended);
        transferBtn.transform.GetComponent<Button>().interactable = defendCards.Count == 0 && CanTransfer(myTurn, selected);
        endBattleBtn.gameObject.SetActive(phase == 1 && myTurn == defender);
        
    }

    public void LoadMultiplayerGameScene()
    {
        //optionsCanvas = GameObject.Find("OptionsCanvas");
        //optionsCanvas.transform.Find("LeaveBtn").GetComponent<Button>().onClick.AddListener(GoToMainScene);
        /*if (isServer)
        {
            optionsCanvas.transform.Find("LeaveBtn").GetComponent<Button>().onClick.AddListener(Server.server.LeaveGame);
        }
        else
        {
            optionsCanvas.transform.Find("LeaveBtn").GetComponent<Button>().onClick.AddListener(Client.client.LeaveGame);
        }*/

        gameCanvas = GameObject.Find("GameCanvas");
        myCardsContainer = gameCanvas.transform.Find("HandBackground").Find("Hand").Find("CardsContainer").gameObject;
        cardField = gameCanvas.transform.Find("Field").gameObject;
        attackField = cardField.transform.Find("AttackField").gameObject;
        defendField = cardField.transform.Find("DefendField").gameObject;
        otherPlayersContainer = gameCanvas.transform.Find("OtherPlayers").Find("PlayersContainer").gameObject;
        playerActionsContainer = gameCanvas.transform.Find("PlayerActions").gameObject;

        endGameCanvas = GameObject.Find("EndGameCanvas");
        endGameCanvas.transform.Find("Transitioner").Find("ActionBtn").GetComponent<Button>().onClick.AddListener(GoToMainScene);
        //endGameCanvas.SetActive(false);

        attackBtn = Instantiate(actionButtonPrefab) as GameObject;
        attackBtn.transform.GetComponent<Button>().onClick.AddListener(CommitAttack);
        attackBtn.transform.GetChild(0).GetComponent<Text>().text = "Attack";
        attackBtn.transform.SetParent(playerActionsContainer.transform);
        attackBtn.transform.localScale = new Vector3(1, 1, 1);

        defendBtn = Instantiate(actionButtonPrefab) as GameObject;
        defendBtn.transform.GetComponent<Button>().onClick.AddListener(CommitDefend);
        defendBtn.transform.GetChild(0).GetComponent<Text>().text = "Defend";
        defendBtn.transform.SetParent(playerActionsContainer.transform);
        defendBtn.transform.localScale = new Vector3(1, 1, 1);

        transferBtn = Instantiate(actionButtonPrefab) as GameObject;
        transferBtn.transform.GetComponent<Button>().onClick.AddListener(CommitTransfer);
        transferBtn.transform.GetChild(0).GetComponent<Text>().text = "Transfer";
        transferBtn.transform.SetParent(playerActionsContainer.transform);
        transferBtn.transform.localScale = new Vector3(1, 1, 1);

        endBattleBtn = Instantiate(actionButtonPrefab) as GameObject;
        endBattleBtn.transform.GetComponent<Button>().onClick.AddListener(CommitEndBattle);
        endBattleBtn.transform.GetChild(0).GetComponent<Text>().text = "End Battle";
        endBattleBtn.transform.SetParent(playerActionsContainer.transform);
        endBattleBtn.transform.localScale = new Vector3(1, 1, 1);

        if (isServer)
            gameCanvas.transform.Find("LeaveGameBtn").GetComponent<Button>().onClick.AddListener(Server.server.LeaveGame);
        else
            gameCanvas.transform.Find("LeaveGameBtn").GetComponent<Button>().onClick.AddListener(Client.client.LeaveGame);
        playerTurnText = gameCanvas.transform.Find("PlayerTurnTxt").GetComponent<Text>();
        deckCountText = gameCanvas.transform.Find("DeckCount").Find("DeckCountTxt").GetComponent<Text>();
        handCountText = gameCanvas.transform.Find("PlayerHand").Find("HandCardsCountTxt").GetComponent<Text>();
        trumpCard = gameCanvas.transform.Find("TrumpCard").gameObject;
    }

    public void CommitEndBattle()
    {
       // Debug.Log("commit end battle");
        if (EndBattlePhase())
        {
            if (isServer)
            {
                Server.server.CommitEndBattle();
            }
            else
            {
                Client.client.CommitEndBattle();
            }
        }
        else
        {
            Debug.Log("CANT END, battle phase didnt even begin!");
        }
    }

    public void CommitTransfer()
    {
       // Debug.Log("commit transfer");
        if (selected.Count == 0)
        {
            Debug.Log("didnt select any cards");
            return;
        }
        int value = -1;
        foreach (Card c in selected)
        {
            if (value == -1)
                value = c.cardValue;
            else if (c.cardValue != value)
            {
                Debug.Log("must select cards with similar vlaue");
                return;
            }
        }
        if (Transfer(myTurn, selected))
        {
            //Debug.Log("Successful xfer");
            if (isServer)
            {
                Server.server.CommitTransfer();
            }
            else
            {
                Client.client.CommitTransfer();
            }

        }
        else
        {
           // Debug.Log("failed to xfer");
            return;
        }

        foreach (FieldPair fp in defendCards)
        {
            for (int i = 0; i < defendField.transform.childCount; i++)
            {
                Transform cardHolder = defendField.transform.GetChild(i);
                if (cardHolder.childCount == 1)
                {
                    CardUI cui = cardHolder.GetChild(0).GetComponent<CardUI>();
                    if (cui.card.Equals(fp.defend))
                    {
                        cui.ReturnToHand();
                        break;
                    }
                }
            }
        }
        selected.Clear();
        UpdateActionButtons();
        UpdateGameUI();
    }

    public void CommitDefend()
    {
      //  Debug.Log("Commit defend");
        if (defendCards.Count == 0)
        {
            Debug.Log("CANT DEFEND WITH NO CARDS");
            return;
        }
        foreach (FieldPair fp in defendCards)
        {
            Card def = fp.defend;
            fp.defend = null;
            if (!CanDefend(fp, def))
            {
                Debug.Log("ERR CANT DEF");
                return;
            }
            else
            {
                Debug.Log("VALID DEF");
                Defend(myTurn, def, fp);
                //fp.defend = def;
            }
        }

        if (isServer)
        {
            Server.server.CommitDefend();
        }
        else
        {
            Client.client.CommitDefend();
        }
        /*
        foreach(Card c in selected)
        {
            for(int i = 0; i < myCardsContainer.transform.childCount; i++)
            {
                CardUI cui = myCardsContainer.transform.GetChild(i).GetChild(0).GetComponent<CardUI>();
                if (cui.card.Equals(c) && cui.inHand)
                {
                    cui.selected = false;
                    cui.transform.position -= new Vector3(0, -20f, 0);
                    break;
                }
            }
        }
        selected.Clear();
        foreach(FieldPair fp in defendCards)
        {

        }*/
        defendCards.Clear();
        UpdateActionButtons();
        UpdateGameUI();
    }

    public void CommitAttack()
    {
        //Debug.Log("Commit attack");
        if (ValidAttack(selected))
        {
          //  Debug.Log("CAN ATTK");
            Attack(myTurn, selected);
            if (isServer)
            {
                Server.server.CommitAttack();
            }
            else
            {
                Client.client.CommitAttack();
            }
            selected.Clear();
            UpdateActionButtons();
            UpdateGameUI();
        }
        else
        {
           // Debug.Log("NOT VALID");

        }
    }

    public void LoadClientScene()
    {
        isServer = false;

    }

    public void LoadServerScene()
    {
        isServer = true;
    }

    public void LoadMultiplayerScene()
    {
        multiplayersCanvas = GameObject.Find("MultiplayersCanvas");
        multiplayersCanvas.transform.Find("HostBtn").GetComponent<Button>().onClick.AddListener(GoToServerScene);
        multiplayersCanvas.transform.Find("JoinBtn").GetComponent<Button>().onClick.AddListener(GoToClientScene);
        multiplayersCanvas.transform.Find("BackBtn").GetComponent<Button>().onClick.AddListener(GoToMainScene);
    }

    public void LoadMainScene()
    {
        mainCanvas = GameObject.Find("MainCanvas");
        mainCanvas.transform.Find("MultiplayerBtn").GetComponent<Button>().onClick.AddListener(GoToMultiplayerScene);
    }

    public void GoToMultiplayerScene()
    {
        if (mainCanvas != null)
        {
            playerName = mainCanvas.transform.Find("NameInputField").Find("Text").GetComponent<Text>().text;
            int i = playerName.IndexOf(' ');
            if (i >= 0)
            {
                if (playerName[0] == ' ')
                {
                    int j = 0;
                    while (playerName[j] == ' ')
                    {
                        j++;
                    }
                    if (playerName[j] == '\0')
                        playerName = "";
                    else
                        playerName = playerName.Substring(j, playerName.Length - j);
                }
                else
                    playerName = playerName.Substring(0, i);
            }

            if (playerName == "")
                playerName = mainCanvas.transform.Find("NameInputField").Find("Placeholder").GetComponent<Text>().text;
            if (playerName.Length > 10)
                playerName = playerName.Substring(0, 10);
        }
        SceneManager.LoadScene("Multiplayer");
    }

    public void GoToServerScene()
    {
        SceneManager.LoadScene("Server");
    }

    public void GoToClientScene()
    {
        SceneManager.LoadScene("Client");
    }

    public void GoToMainScene()
    {
        SceneManager.LoadScene("Main");
    }

    public void GoToGameScene()
    {
        SceneManager.LoadScene("Game");
    }

    public void LeaveGame()
    {
        SceneManager.LoadScene("Client");
    }
}
